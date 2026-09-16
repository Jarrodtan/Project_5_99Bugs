using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Type-agnostic inspector content view. Binds to <see cref="SelectionContext"/> and paints
/// <see cref="IInspectable"/> title + <see cref="StatEntry"/> rows using <see cref="InspectionUiConfig"/>.
/// Does not implement world-follow positioning (Task 4).
/// </summary>
public class InspectorPanel : MonoBehaviour
{
    [Tooltip("Shared selection session to listen to.")]
    [SerializeField]
    SelectionContext selectionContext;

    [Tooltip("All visual/layout/behaviour knobs for this panel.")]
    [SerializeField]
    InspectionUiConfig uiConfig;

    Canvas canvas;
    RectTransform panelRoot;
    Image panelBackground;
    RectTransform titleRect;
    TMP_Text titleText;
    RectTransform rowsParent;
    VerticalLayoutGroup rowsLayout;
    readonly List<StatRowView> rowPool = new();
    bool built;

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

        ApplyContent(inspectable);
        SetPanelVisible(true);
    }

    /// <summary>
    /// Hides the panel without destroying the UI hierarchy.
    /// </summary>
    public void Hide()
    {
        if (!EnsureReady())
            return;

        SetPanelVisible(false);
    }

    void Awake()
    {
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

    void OnSelectionChanged(SelectionChange change)
    {
        if (change.Current.IsValid)
            Show(change.Current);
        else
            Hide();
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

        var canvasGo = new GameObject("InspectorCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = uiConfig.canvasSortOrder;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        panelRoot = panelGo.AddComponent<RectTransform>();
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

        ApplyLayoutFromConfig();
        built = true;

        if (uiConfig.startHidden)
            SetPanelVisible(false);
        else
            SetPanelVisible(true);
    }

    void ApplyLayoutFromConfig()
    {
        canvas.sortingOrder = uiConfig.canvasSortOrder;

        panelRoot.anchorMin = new Vector2(0f, 1f);
        panelRoot.anchorMax = new Vector2(0f, 1f);
        panelRoot.pivot = new Vector2(0f, 1f);
        panelRoot.sizeDelta = uiConfig.panelSize;
        panelRoot.anchoredPosition = uiConfig.panelAnchoredPosition;

        panelBackground.color = uiConfig.backgroundColor;

        titleText.fontSize = uiConfig.titleFontSize;
        titleText.color = uiConfig.titleColor;
        if (uiConfig.font != null)
            titleText.font = uiConfig.font;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        rowsLayout.spacing = uiConfig.rowSpacing;
        rowsLayout.padding = new RectOffset(0, 0, 0, 0);

        LayoutTitleAndRows();

        for (var i = 0; i < rowPool.Count; i++)
            rowPool[i].ApplyStyle(uiConfig);
    }

    void LayoutTitleAndRows()
    {
        if (titleRect == null || rowsParent == null)
            return;

        var pad = uiConfig.panelPadding;
        var titleHeight = uiConfig.titleFontSize + uiConfig.rowSpacing;

        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(pad, -pad - titleHeight);
        titleRect.offsetMax = new Vector2(-pad, -pad);

        rowsParent.anchorMin = new Vector2(0f, 0f);
        rowsParent.anchorMax = new Vector2(1f, 1f);
        rowsParent.offsetMin = new Vector2(pad, pad);
        rowsParent.offsetMax = new Vector2(-pad, -pad - titleHeight - uiConfig.rowSpacing);
    }

    void ApplyContent(IInspectable inspectable)
    {
        ApplyLayoutFromConfig();

        titleText.text = inspectable.GetDisplayName() ?? string.Empty;

        var stats = inspectable.GetStats();
        var count = stats != null ? stats.Count : 0;
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

    void SetPanelVisible(bool show)
    {
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(show);
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
