using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Type-agnostic inspector content view. Binds to <see cref="SelectionContext"/> and paints
/// <see cref="IInspectable"/> stats. Placement is delegated to <see cref="IOverlayPositioner"/> modules,
/// unless the panel is hosted as the contextual Inspect tab of <see cref="TabbedStatsPanel"/>; then it sits in
/// the tab content area and its tab is available only while a valid inspectable is selected.
/// </summary>
public class InspectorPanel : MonoBehaviour, ITabbedPanelContent
{
    [Tooltip("Shared selection session to listen to.")]
    [SerializeField]
    SelectionContext selectionContext;

    [Tooltip("All visual/layout/overlay knobs for this panel.")]
    [SerializeField]
    InspectionUiConfig uiConfig;

    [Tooltip("Optional camera override. When empty and uiConfig.useMainCamera is true, Camera.main is used.")]
    [SerializeField]
    Camera cameraOverride;

    Canvas screenCanvas;
    Canvas worldCanvas;
    RectTransform panelRoot;
    Image panelBackground;
    RectTransform titleRect;
    TMP_Text titleText;
    RectTransform rowsParent;
    VerticalLayoutGroup rowsLayout;
    readonly List<StatRowView> rowPool = new();
    readonly List<IOverlayPositioner> positioners = new();
    OverlayPositionContext overlayContext;
    IOverlayPositioner activePositioner;
    OverlayMode appliedMode;
    Transform currentTarget;
    bool built;
    bool contentVisible;
    bool positionVisible = true;
    readonly TabContentFrame frame = new();
    readonly List<LabeledStatRowView> listRowPool = new();
    CanvasGroup rowsGroup;
    bool isStale;
    SelectionHandle currentHandle;
    int visibleRowCount;
    float nextLiveRefreshTime;
    bool reportedTabAvailable;

    /// <inheritdoc />
    public string TabLabel => uiConfig != null && !string.IsNullOrEmpty(uiConfig.tabLabel) ? uiConfig.tabLabel : name;

    /// <inheritdoc />
    public bool IsTabAvailable => contentVisible;

    /// <inheritdoc />
    public event System.Action TabAvailabilityChanged;

    /// <inheritdoc />
    public event System.Action TabFocusRequested;

    // Hosted as the Inspect tab with a shared list style: rows render like the Population tab.
    StatListStyle ListStyle => frame.IsEmbedded && uiConfig != null ? uiConfig.tabListStyle : null;

    /// <inheritdoc />
    public void EmbedIn(RectTransform host)
    {
        if (!frame.EmbedIn(host))
            return;

        // Hosted: the tab bar owns placement, so the overlay canvases and positioners sit idle.
        activePositioner?.ClearTarget();
        screenCanvas.gameObject.SetActive(false);
        worldCanvas.gameObject.SetActive(false);
        ApplyLayoutFromConfig();
        RefreshVisibility();
    }

    /// <inheritdoc />
    public void SetShown(bool isShown) => frame.SetShown(isShown);

    /// <summary>
    /// Replaces default positioner modules. Call before the first Show / LateUpdate if customizing.
    /// </summary>
    /// <param name="modules">Positioner implementations to use.</param>
    public void SetPositioners(params IOverlayPositioner[] modules)
    {
        positioners.Clear();
        if (modules == null)
            return;

        for (var i = 0; i < modules.Length; i++)
        {
            if (modules[i] != null)
                positioners.Add(modules[i]);
        }

        if (built && uiConfig != null)
            ApplyOverlayMode(uiConfig.overlayMode, force: true);
    }

    /// <summary>
    /// Shows stats for the given handle, or hides when the handle is invalid / not inspectable.
    /// </summary>
    /// <param name="handle">Current selection handle.</param>
    public void Show(SelectionHandle handle)
    {
        if (!EnsureReady())
            return;

        if (!handle.IsValid)
        {
            Hide();
            return;
        }

        if (!TryResolveInspectable(handle, out var inspectable))
        {
            Hide();
            return;
        }

        currentHandle = handle;
        nextLiveRefreshTime = Time.unscaledTime + uiConfig.liveRefreshIntervalSeconds;
        SetStale(false);
        ApplyContent(inspectable);
        currentTarget = ResolveTargetTransform(handle);
        if (!frame.IsEmbedded)
            activePositioner?.SetTarget(currentTarget);
        contentVisible = true;
        positionVisible = true;
        RefreshVisibility();
    }

    /// <summary>
    /// Hides the panel without destroying the UI hierarchy.
    /// </summary>
    public void Hide()
    {
        if (!EnsureReady())
            return;

        activePositioner?.ClearTarget();
        currentTarget = null;
        currentHandle = SelectionHandle.None;
        SetStale(false);
        contentVisible = false;
        positionVisible = false;
        RefreshVisibility();
    }

    void Awake()
    {
        if (positioners.Count == 0)
            SetPositioners(new ScreenSpacePositioner(), new WorldSpacePositioner());

        EnsureReady();
    }

    void OnEnable()
    {
        if (selectionContext == null)
        {
            Debug.LogError($"{nameof(InspectorPanel)} requires a {nameof(SelectionContext)}.", this);
            return;
        }

        selectionContext.Changed += OnSelectionChanged;

        if (selectionContext.Current.IsValid)
            Show(selectionContext.Current);
        else if (uiConfig != null && uiConfig.startHidden)
            Hide();
    }

    void OnDisable()
    {
        if (selectionContext != null)
            selectionContext.Changed -= OnSelectionChanged;
    }

    void LateUpdate()
    {
        if (!built || uiConfig == null)
            return;

        TickLiveRefresh();

        if (frame.IsEmbedded || activePositioner == null)
            return;

        if (appliedMode != uiConfig.overlayMode)
            ApplyOverlayMode(uiConfig.overlayMode, force: false);

        if (!contentVisible || currentTarget == null)
        {
            positionVisible = false;
            RefreshVisibility();
            return;
        }

        positionVisible = activePositioner.UpdatePosition();
        RefreshVisibility();
    }

    void OnSelectionChanged(SelectionChange change)
    {
        if (change.Current.IsValid)
            Show(change.Current);
        else
            HideOrKeepStale();

        // Every new inspectable selection pulls the tab host to Inspect, not just the first reveal.
        if (change.Current.IsValid && contentVisible)
            TabFocusRequested?.Invoke();
    }

    bool EnsureReady()
    {
        if (uiConfig == null)
        {
            Debug.LogError($"{nameof(InspectorPanel)} requires an {nameof(InspectionUiConfig)}.", this);
            return false;
        }

        if (!built)
            BuildUi();

        return built;
    }

    void BuildUi()
    {
        EnsureEventSystem();

        var screenGo = new GameObject("InspectorScreenCanvas");
        screenGo.transform.SetParent(transform, false);
        screenCanvas = screenGo.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = uiConfig.canvasSortOrder;
        screenGo.AddComponent<CanvasScaler>();
        screenGo.AddComponent<GraphicRaycaster>();

        var worldGo = new GameObject("InspectorWorldCanvas");
        worldGo.transform.SetParent(transform, false);
        worldCanvas = worldGo.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = uiConfig.worldSpaceSortingOrder;
        worldGo.AddComponent<CanvasScaler>();
        worldGo.AddComponent<GraphicRaycaster>();
        worldGo.SetActive(false);

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(screenGo.transform, false);
        panelRoot = panelGo.AddComponent<RectTransform>();
        frame.AttachRoot(panelRoot);
        panelBackground = panelGo.AddComponent<Image>();
        panelBackground.raycastTarget = true;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        titleRect = titleGo.AddComponent<RectTransform>();
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.raycastTarget = false;

        var rowsGo = new GameObject("Rows");
        rowsGo.transform.SetParent(panelGo.transform, false);
        rowsParent = rowsGo.AddComponent<RectTransform>();
        rowsLayout = rowsGo.AddComponent<VerticalLayoutGroup>();
        rowsLayout.childAlignment = TextAnchor.UpperLeft;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        overlayContext = new OverlayPositionContext(
            uiConfig,
            panelRoot,
            screenCanvas,
            worldCanvas,
            ResolveCamera);

        ApplyLayoutFromConfig();
        built = true;
        ApplyOverlayMode(uiConfig.overlayMode, force: true);

        if (uiConfig.startHidden)
            Hide();
        else
        {
            contentVisible = true;
            positionVisible = true;
            RefreshVisibility();
        }
    }

    void ApplyOverlayMode(OverlayMode mode, bool force)
    {
        if (frame.IsEmbedded)
            return;

        if (!force && activePositioner != null && appliedMode == mode)
            return;

        IOverlayPositioner next = null;
        for (var i = 0; i < positioners.Count; i++)
        {
            if (positioners[i].Mode == mode)
            {
                next = positioners[i];
                break;
            }
        }

        if (next == null)
        {
            Debug.LogError($"{nameof(InspectorPanel)} has no {nameof(IOverlayPositioner)} for mode {mode}.", this);
            return;
        }

        activePositioner = next;
        appliedMode = mode;
        activePositioner.Bind(overlayContext);
        ApplyLayoutFromConfig();

        if (currentTarget != null)
            activePositioner.SetTarget(currentTarget);
        else
            activePositioner.ClearTarget();
    }

    Camera ResolveCamera()
    {
        if (cameraOverride != null)
            return cameraOverride;

        if (uiConfig != null && uiConfig.useMainCamera)
            return Camera.main;

        return null;
    }

    void ApplyLayoutFromConfig()
    {
        if (screenCanvas != null)
            screenCanvas.sortingOrder = uiConfig.canvasSortOrder;

        if (panelRoot != null)
        {
            if (frame.IsEmbedded)
            {
                frame.ApplyPlacement(Vector2.zero, Vector2.zero, 0f);
                panelRoot.sizeDelta = new Vector2(0f, EmbeddedHeight());
            }
            else
            {
                panelRoot.sizeDelta = uiConfig.panelSize;
            }
        }

        if (panelBackground != null)
            panelBackground.color = uiConfig.backgroundColor;

        if (titleText != null)
        {
            titleText.fontSize = uiConfig.titleFontSize;
            titleText.color = uiConfig.titleColor;
            if (uiConfig.font != null)
                titleText.font = uiConfig.font;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (rowsLayout != null)
        {
            rowsLayout.spacing = uiConfig.rowSpacing;
            rowsLayout.padding = new RectOffset(0, 0, 0, 0);
        }

        LayoutTitleAndRows();

        for (var i = 0; i < rowPool.Count; i++)
            rowPool[i].ApplyStyle(uiConfig);

        var listStyle = ListStyle;
        if (listStyle != null)
        {
            StatListStyling.ApplyPanel(listStyle, panelBackground, null);
            StatListStyling.ApplyTitle(listStyle, titleText);
            if (rowsLayout != null)
                rowsLayout.spacing = listStyle.rowSpacing;
            for (var i = 0; i < listRowPool.Count; i++)
                listRowPool[i].ApplyStyle(listStyle);
        }
    }

    void LayoutTitleAndRows()
    {
        if (titleRect == null || rowsParent == null)
            return;

        var listStyle = ListStyle;
        var pad = listStyle != null ? listStyle.panelPadding : uiConfig.panelPadding;
        var rowSpacing = listStyle != null ? listStyle.rowSpacing : uiConfig.rowSpacing;
        var titleHeight = listStyle != null ? StatListStyling.TitleHeight(listStyle) : uiConfig.titleFontSize + uiConfig.rowSpacing;

        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(pad, -pad - titleHeight);
        titleRect.offsetMax = new Vector2(-pad, -pad);

        rowsParent.anchorMin = new Vector2(0f, 0f);
        rowsParent.anchorMax = new Vector2(1f, 1f);
        rowsParent.offsetMin = new Vector2(pad, pad);
        rowsParent.offsetMax = new Vector2(-pad, -pad - titleHeight - rowSpacing);
    }

    void ApplyContent(IInspectable inspectable)
    {
        var stats = inspectable.GetStats();
        var count = stats != null ? stats.Count : 0;
        visibleRowCount = count;

        ApplyLayoutFromConfig();

        titleText.text = inspectable.GetDisplayName() ?? string.Empty;

        if (ListStyle != null)
        {
            BindListRows(stats, count);
            return;
        }

        EnsureRowCount(count);

        for (var i = 0; i < rowPool.Count; i++)
        {
            if (i < count)
            {
                rowPool[i].ApplyStyle(uiConfig);
                rowPool[i].Bind(stats[i]);
                rowPool[i].SetVisible(true);
            }
            else
            {
                rowPool[i].SetVisible(false);
            }
        }
    }

    void EnsureRowCount(int needed)
    {
        while (rowPool.Count < needed)
            rowPool.Add(CreateRow());
    }

    StatRowView CreateRow()
    {
        var rowGo = new GameObject("StatRow");
        rowGo.transform.SetParent(rowsParent, false);
        var rowRect = rowGo.AddComponent<RectTransform>();
        var layout = rowGo.AddComponent<LayoutElement>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        var labelText = labelGo.AddComponent<TextMeshProUGUI>();

        var valueGo = new GameObject("Value");
        valueGo.transform.SetParent(rowGo.transform, false);
        var valueRect = valueGo.AddComponent<RectTransform>();
        var valueText = valueGo.AddComponent<TextMeshProUGUI>();

        var view = rowGo.AddComponent<StatRowView>();
        view.Initialize(rowRect, layout, labelText, valueText, labelRect, valueRect);
        view.ApplyStyle(uiConfig);
        return view;
    }

    void RefreshVisibility()
    {
        if (panelRoot == null)
            return;

        // Hosted: no positioner, so only content decides; the tab host handles show/hide.
        panelRoot.gameObject.SetActive(contentVisible && (frame.IsEmbedded || positionVisible));

        if (reportedTabAvailable != contentVisible)
        {
            reportedTabAvailable = contentVisible;
            TabAvailabilityChanged?.Invoke();
        }
    }

    void TickLiveRefresh()
    {
        if (!contentVisible || isStale || uiConfig.liveRefreshIntervalSeconds <= 0f || Time.unscaledTime < nextLiveRefreshTime)
            return;

        nextLiveRefreshTime = Time.unscaledTime + uiConfig.liveRefreshIntervalSeconds;
        if (TryResolveInspectable(currentHandle, out var inspectable))
            ApplyContent(inspectable);
        else
            HideOrKeepStale();
    }

    // Deselect policy for the Inspect tab (InspectionUiConfig.tabDeselectBehaviour). The floating overlay always hides.
    void HideOrKeepStale()
    {
        var keep = frame.IsEmbedded
                   && contentVisible
                   && uiConfig.tabDeselectBehaviour == InspectTabDeselectBehaviour.KeepLastSelection;
        if (!keep)
        {
            Hide();
            return;
        }

        // Tab stays available; content freezes and is visibly marked as the last-seen object.
        var lastName = isStale ? null : titleText.text;
        currentHandle = SelectionHandle.None;
        SetStale(true);
        if (lastName != null)
            titleText.text = string.Format(uiConfig.staleTitleFormat ?? "{0}", lastName);
    }

    void SetStale(bool stale)
    {
        isStale = stale;
        if (rowsParent == null)
            return;

        if (rowsGroup == null)
        {
            if (!stale)
                return;
            rowsGroup = rowsParent.gameObject.AddComponent<CanvasGroup>();
        }

        rowsGroup.alpha = stale ? uiConfig.staleRowsAlpha : 1f;
    }

    void BindListRows(IReadOnlyList<StatEntry> stats, int count)
    {
        for (var i = 0; i < rowPool.Count; i++)
            rowPool[i].SetVisible(false);

        while (listRowPool.Count < count)
            listRowPool.Add(LabeledStatRowView.Create(rowsParent, ListStyle));

        for (var i = 0; i < listRowPool.Count; i++)
        {
            if (i < count)
            {
                listRowPool[i].Bind(stats[i]);
                listRowPool[i].SetVisible(true);
            }
            else
            {
                listRowPool[i].SetVisible(false);
            }
        }
    }

    float EmbeddedHeight()
    {
        if (ListStyle != null)
            return StatListStyling.PanelHeight(ListStyle, visibleRowCount);

        var pad = uiConfig.panelPadding;
        var titleHeight = uiConfig.titleFontSize + uiConfig.rowSpacing;
        var rows = visibleRowCount * uiConfig.rowHeight + Mathf.Max(0, visibleRowCount - 1) * uiConfig.rowSpacing;
        return pad * 2f + titleHeight + uiConfig.rowSpacing + rows;
    }

    static Transform ResolveTargetTransform(SelectionHandle handle)
    {
        if (!SelectableRegistry.TryGet(handle, out var selectable))
            return null;

        var behaviour = selectable as Component;
        return behaviour != null ? behaviour.transform : null;
    }

    static bool TryResolveInspectable(SelectionHandle handle, out IInspectable inspectable)
    {
        inspectable = null;

        if (!SelectableRegistry.TryGet(handle, out var selectable))
            return false;

        var behaviour = selectable as Component;
        if (behaviour == null)
            return false;

        inspectable = behaviour.GetComponentInParent<IInspectable>();
        return inspectable != null;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }
}
