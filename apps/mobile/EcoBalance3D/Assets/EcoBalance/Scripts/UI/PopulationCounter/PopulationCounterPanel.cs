using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display 1: live per-species population counters. Polls an <see cref="ISimulationDataProvider"/>,
/// shows one row per species with a trend arrow and change flash, and marks zero counts as extinct.
/// Builds its own Screen Space Overlay canvas unless hosted by <see cref="TabbedStatsPanel"/>;
/// list styling comes from the shared <see cref="StatListStyle"/>, population states from <see cref="PopulationCounterUiConfig"/>.
/// </summary>
public class PopulationCounterPanel : MonoBehaviour, ITabbedPanelContent
{
    [Tooltip("Component implementing ISimulationDataProvider (e.g. MockSimulationDataProvider).")]
    [SerializeField]
    MonoBehaviour dataProviderSource;

    [Tooltip("All visual/layout/text/timing knobs for this display.")]
    [SerializeField]
    PopulationCounterUiConfig uiConfig;

    readonly SpeciesPopulationTracker tracker = new();
    readonly List<SpeciesCounterRowView> rowPool = new();
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
    /// Clears change history so the swap doesn't flash every row.
    /// </summary>
    /// <param name="dataProvider">Data access layer to read from; null empties the display.</param>
    public void SetProvider(ISimulationDataProvider dataProvider)
    {
        provider = dataProvider;
        tracker.Reset();
        nextPollTime = 0f;
    }

    /// <summary>
    /// Reads the provider immediately instead of waiting for the next poll.
    /// </summary>
    public void Refresh()
    {
        if (!EnsureReady())
            return;

        tracker.Refresh(provider);
        var states = tracker.States;
        noDataText.gameObject.SetActive(states.Count == 0);
        EnsureRowCount(states.Count);

        for (var i = 0; i < rowPool.Count; i++)
        {
            if (i < states.Count)
            {
                rowPool[i].Bind(states[i]);
                rowPool[i].SetVisible(true);
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
            Debug.LogError($"{nameof(PopulationCounterPanel)}: {dataProviderSource.GetType().Name} does not implement {nameof(ISimulationDataProvider)}.", this);
        else if (provider == null)
            Debug.LogError($"{nameof(PopulationCounterPanel)} requires an {nameof(ISimulationDataProvider)}.", this);
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
            Debug.LogWarning($"{nameof(PopulationCounterPanel)}: {dataProviderSource.GetType().Name} does not implement {nameof(ISimulationDataProvider)}; clearing.", this);
            dataProviderSource = null;
        }

        if (built)
            ApplyLayoutFromConfig();
    }

    bool EnsureReady()
    {
        if (uiConfig == null)
        {
            Debug.LogError($"{nameof(PopulationCounterPanel)} requires a {nameof(PopulationCounterUiConfig)}.", this);
            enabled = false;
            return false;
        }

        if (uiConfig.listStyle == null)
        {
            Debug.LogError($"{nameof(PopulationCounterPanel)}: assign a {nameof(StatListStyle)} on {uiConfig.name}.", this);
            enabled = false;
            return false;
        }

        if (!built)
            BuildUi();

        return built;
    }

    void BuildUi()
    {
        var parent = frame.ResolveParent(transform, "PopulationCounterCanvas");

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

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        titleGo.AddComponent<RectTransform>();
        titleLayout = titleGo.AddComponent<LayoutElement>();
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.raycastTarget = false;

        var noDataGo = new GameObject("NoData");
        noDataGo.transform.SetParent(panelGo.transform, false);
        noDataGo.AddComponent<RectTransform>();
        noDataLayout = noDataGo.AddComponent<LayoutElement>();
        noDataText = noDataGo.AddComponent<TextMeshProUGUI>();
        noDataText.raycastTarget = false;

        built = true;
        ApplyLayoutFromConfig();
    }

    void ApplyLayoutFromConfig()
    {
        if (uiConfig == null || uiConfig.listStyle == null)
            return;

        var style = uiConfig.listStyle;

        frame.SetSortOrder(uiConfig.canvasSortOrder);

        frame.ApplyPlacement(new Vector2(0f, 1f), uiConfig.panelAnchoredPosition, uiConfig.panelWidth);

        StatListStyling.ApplyPanel(style, panelBackground, panelLayout);

        if (titleText != null)
        {
            // The tab already names the panel when embedded.
            var hasTitle = !frame.IsEmbedded && !string.IsNullOrEmpty(uiConfig.titleText);
            titleText.gameObject.SetActive(hasTitle);
            titleText.text = uiConfig.titleText ?? string.Empty;
            StatListStyling.ApplyTitle(style, titleText, titleLayout);
        }

        if (noDataText != null)
        {
            noDataText.text = uiConfig.noDataText ?? string.Empty;
            StatListStyling.ApplyText(style, noDataText, style.rowFontSize, uiConfig.unavailableTextColor, TextAlignmentOptions.MidlineLeft);
            noDataLayout.preferredHeight = style.rowHeight;
        }

        for (var i = 0; i < rowPool.Count; i++)
            rowPool[i].ApplyStyle(uiConfig);
    }

    void EnsureRowCount(int needed)
    {
        while (rowPool.Count < needed)
            rowPool.Add(CreateRow());
    }

    SpeciesCounterRowView CreateRow()
    {
        var rowGo = new GameObject("SpeciesRow");
        rowGo.transform.SetParent(panelRoot, false);
        rowGo.AddComponent<RectTransform>();
        var layout = rowGo.AddComponent<LayoutElement>();
        var background = rowGo.AddComponent<Image>();
        background.raycastTarget = false;

        var countGo = new GameObject("Count");
        countGo.transform.SetParent(rowGo.transform, false);
        countGo.AddComponent<RectTransform>();
        var countText = countGo.AddComponent<TextMeshProUGUI>();

        var arrowGo = new GameObject("Trend");
        arrowGo.transform.SetParent(rowGo.transform, false);
        arrowGo.AddComponent<RectTransform>();
        var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();

        var view = rowGo.AddComponent<SpeciesCounterRowView>();
        view.Initialize(layout, background, countText, arrowText);
        view.ApplyStyle(uiConfig);
        return view;
    }
}
