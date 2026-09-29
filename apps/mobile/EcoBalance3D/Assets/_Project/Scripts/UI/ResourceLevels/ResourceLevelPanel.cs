using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display 2: live per-resource levels. Polls an <see cref="ISimulationDataProvider"/>, shows one row per
/// resource with a numeric readout and level bar, and flags critically low resources with a color change
/// and pulsing warning icon. Builds its own Screen Space Overlay canvas unless hosted by
/// <see cref="TabbedStatsPanel"/>; all styling comes from <see cref="ResourceLevelUiConfig"/>.
/// </summary>
public class ResourceLevelPanel : MonoBehaviour, ITabbedPanelContent
{
    [Tooltip("Component implementing ISimulationDataProvider (e.g. MockSimulationDataProvider).")]
    [SerializeField]
    MonoBehaviour dataProviderSource;

    [Tooltip("All visual/layout/text/limit/timing knobs for this display.")]
    [SerializeField]
    ResourceLevelUiConfig uiConfig;

    readonly ResourceLevelReader reader = new();
    readonly List<ResourceLevelRowView> rowPool = new();
    readonly TabContentFrame frame = new();
    ISimulationDataProvider provider;
    RectTransform panelRoot;
    Image panelBackground;
    VerticalLayoutGroup panelLayout;
    TMP_Text titleText;
    LayoutElement titleLayout;
    TMP_Text noDataText;
    LayoutElement noDataLayout;
    float nextPollTime;
    bool built;

    /// <inheritdoc />
    public string TabLabel => uiConfig != null && !string.IsNullOrEmpty(uiConfig.titleText) ? uiConfig.titleText : name;

    /// <inheritdoc />
    public bool IsTabAvailable => true;

    /// <inheritdoc />
    public event System.Action TabAvailabilityChanged { add { } remove { } }

    /// <inheritdoc />
    public event System.Action TabFocusRequested { add { } remove { } }

    /// <inheritdoc />
    public void EmbedIn(RectTransform host)
    {
        if (frame.EmbedIn(host))
            ApplyLayoutFromConfig();
    }

    /// <inheritdoc />
    public void SetShown(bool isShown) => frame.SetShown(isShown);

    /// <summary>
    /// Overrides the inspector-assigned provider (e.g. to swap mock for live at runtime).
    /// </summary>
    /// <param name="dataProvider">Data access layer to read from; null empties the display.</param>
    public void SetProvider(ISimulationDataProvider dataProvider)
    {
        provider = dataProvider;
        nextPollTime = 0f;
    }

    /// <summary>
    /// Reads the provider immediately instead of waiting for the next poll.
    /// </summary>
    public void Refresh()
    {
        if (!EnsureReady())
            return;

        reader.Refresh(provider, uiConfig.GetLimits);
        var states = reader.States;
        noDataText.gameObject.SetActive(states.Count == 0);
        EnsureRowCount(states.Count);

        for (var i = 0; i < rowPool.Count; i++)
        {
            if (i < states.Count)
            {
                rowPool[i].SetVisible(true);
                rowPool[i].Bind(states[i]);
            }
            else
            {
                rowPool[i].SetVisible(false);
            }
        }
    }

    void Awake()
    {
        if (provider == null)
            provider = dataProviderSource as ISimulationDataProvider;

        if (dataProviderSource != null && provider == null)
            Debug.LogError($"{nameof(ResourceLevelPanel)}: {dataProviderSource.GetType().Name} does not implement {nameof(ISimulationDataProvider)}.", this);
        else if (provider == null)
            Debug.LogError($"{nameof(ResourceLevelPanel)} requires an {nameof(ISimulationDataProvider)}.", this);
    }

    void OnEnable()
    {
        nextPollTime = 0f;
    }

    void Update()
    {
        if (Time.unscaledTime < nextPollTime)
            return;

        Refresh();
        nextPollTime = uiConfig != null && uiConfig.pollIntervalSeconds > 0f
            ? Time.unscaledTime + uiConfig.pollIntervalSeconds
            : 0f;
    }

    void OnValidate()
    {
        if (dataProviderSource != null && dataProviderSource is not ISimulationDataProvider)
        {
            Debug.LogWarning($"{nameof(ResourceLevelPanel)}: {dataProviderSource.GetType().Name} does not implement {nameof(ISimulationDataProvider)}; clearing.", this);
            dataProviderSource = null;
        }

        if (built)
            ApplyLayoutFromConfig();
    }

    bool EnsureReady()
    {
        if (uiConfig == null)
        {
            Debug.LogError($"{nameof(ResourceLevelPanel)} requires a {nameof(ResourceLevelUiConfig)}.", this);
            enabled = false;
            return false;
        }

        if (!built)
            BuildUi();

        return built;
    }

    void BuildUi()
    {
        var parent = frame.ResolveParent(transform, "ResourceLevelCanvas");

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(parent, false);
        panelRoot = panelGo.AddComponent<RectTransform>();
        frame.AttachRoot(panelRoot);
        panelBackground = panelGo.AddComponent<Image>();
        panelBackground.raycastTarget = false;
        panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
        panelLayout.childAlignment = TextAnchor.UpperLeft;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText(panelGo.transform, "Title", out titleLayout);
        noDataText = CreateText(panelGo.transform, "NoData", out noDataLayout);

        built = true;
        ApplyLayoutFromConfig();
    }

    void ApplyLayoutFromConfig()
    {
        if (uiConfig == null)
            return;

        frame.SetSortOrder(uiConfig.canvasSortOrder);

        frame.ApplyPlacement(uiConfig.panelAnchor, uiConfig.panelAnchoredPosition, uiConfig.panelWidth);

        if (panelBackground != null)
            panelBackground.color = uiConfig.backgroundColor;

        if (panelLayout != null)
        {
            var pad = uiConfig.panelPadding;
            panelLayout.padding = new RectOffset(pad, pad, pad, pad);
            panelLayout.spacing = uiConfig.rowSpacing;
        }

        if (titleText != null)
        {
            // The tab already names the panel when embedded.
            titleText.gameObject.SetActive(!frame.IsEmbedded && !string.IsNullOrEmpty(uiConfig.titleText));
            titleText.text = uiConfig.titleText ?? string.Empty;
            ApplyTextStyle(titleText, uiConfig.titleFontSize, uiConfig.titleColor);
            titleLayout.preferredHeight = uiConfig.titleFontSize + uiConfig.rowSpacing;
        }

        if (noDataText != null)
        {
            noDataText.text = uiConfig.noDataText ?? string.Empty;
            ApplyTextStyle(noDataText, uiConfig.rowFontSize, uiConfig.unavailableColor);
            noDataLayout.preferredHeight = uiConfig.textLineHeight;
        }

        for (var i = 0; i < rowPool.Count; i++)
            rowPool[i].ApplyStyle(uiConfig);
    }

    void ApplyTextStyle(TMP_Text text, float fontSize, Color color)
    {
        text.fontSize = fontSize;
        text.color = color;
        if (uiConfig.font != null)
            text.font = uiConfig.font;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    void EnsureRowCount(int needed)
    {
        while (rowPool.Count < needed)
            rowPool.Add(CreateRow());
    }

    ResourceLevelRowView CreateRow()
    {
        var rowGo = new GameObject("ResourceRow");
        rowGo.transform.SetParent(panelRoot, false);
        rowGo.AddComponent<RectTransform>();
        var layout = rowGo.AddComponent<LayoutElement>();

        var nameLabel = CreateText(rowGo.transform, "Name", out _);
        var value = CreateText(rowGo.transform, "Value", out _);
        var warning = CreateText(rowGo.transform, "Warning", out _);

        var barBackGo = new GameObject("Bar");
        barBackGo.transform.SetParent(rowGo.transform, false);
        barBackGo.AddComponent<RectTransform>();
        var barBack = barBackGo.AddComponent<Image>();
        barBack.raycastTarget = false;

        var barFillGo = new GameObject("Fill");
        barFillGo.transform.SetParent(barBackGo.transform, false);
        barFillGo.AddComponent<RectTransform>();
        var barFill = barFillGo.AddComponent<Image>();
        barFill.raycastTarget = false;

        var view = rowGo.AddComponent<ResourceLevelRowView>();
        view.Initialize(layout, nameLabel, value, warning, barBack, barFill);
        view.ApplyStyle(uiConfig);
        return view;
    }

    static TMP_Text CreateText(Transform parent, string name, out LayoutElement layout)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        layout = go.AddComponent<LayoutElement>();
        var text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }
}
