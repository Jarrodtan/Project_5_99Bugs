using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display 3: rolling feed of recent ecosystem events. Polls an <see cref="ISimulationEventProvider"/> and
/// shows the newest events first, capped to <see cref="EventFeedUiConfig.maxVisibleEntries"/>; new entries
/// highlight briefly and older ones fade. Builds its own Screen Space Overlay canvas unless hosted by
/// <see cref="TabbedStatsPanel"/>; all styling comes from <see cref="EventFeedUiConfig"/>.
/// </summary>
public class EventFeedPanel : MonoBehaviour, ITabbedPanelContent
{
    [Tooltip("Component implementing ISimulationEventProvider (e.g. MockSimulationEventProvider).")]
    [SerializeField]
    MonoBehaviour eventProviderSource;

    [Tooltip("All visual/layout/text/timing knobs for this display.")]
    [SerializeField]
    EventFeedUiConfig uiConfig;

    readonly EventFeedReader reader = new();
    readonly List<EventFeedRowView> rowPool = new();
    readonly TabContentFrame frame = new();
    ISimulationEventProvider provider;
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
    /// Clears feed history so the swap doesn't highlight every entry.
    /// </summary>
    /// <param name="eventProvider">Event source to read from; null empties the feed.</param>
    public void SetProvider(ISimulationEventProvider eventProvider)
    {
        provider = eventProvider;
        reader.Reset();
        nextPollTime = 0f;
    }

    /// <summary>
    /// Reads the provider immediately instead of waiting for the next poll.
    /// </summary>
    public void Refresh()
    {
        if (!EnsureReady())
            return;

        reader.Refresh(provider, uiConfig.maxVisibleEntries, Time.unscaledTime);
        var entries = reader.Entries;
        noDataText.gameObject.SetActive(entries.Count == 0);
        EnsureRowCount(entries.Count);

        for (var i = 0; i < rowPool.Count; i++)
        {
            if (i < entries.Count)
            {
                rowPool[i].SetVisible(true);
                rowPool[i].Bind(entries[i], AgeAlpha(i, entries.Count));
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
            provider = eventProviderSource as ISimulationEventProvider;

        if (eventProviderSource != null && provider == null)
            Debug.LogError($"{nameof(EventFeedPanel)}: {eventProviderSource.GetType().Name} does not implement {nameof(ISimulationEventProvider)}.", this);
        else if (provider == null)
            Debug.LogError($"{nameof(EventFeedPanel)} requires an {nameof(ISimulationEventProvider)}.", this);
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
        if (eventProviderSource != null && eventProviderSource is not ISimulationEventProvider)
        {
            Debug.LogWarning($"{nameof(EventFeedPanel)}: {eventProviderSource.GetType().Name} does not implement {nameof(ISimulationEventProvider)}; clearing.", this);
            eventProviderSource = null;
        }

        if (built)
            ApplyLayoutFromConfig();
    }

    float AgeAlpha(int index, int count)
    {
        if (count <= 1)
            return 1f;

        return Mathf.Lerp(1f, uiConfig.oldestEntryAlpha, index / (float)(count - 1));
    }

    bool EnsureReady()
    {
        if (uiConfig == null)
        {
            Debug.LogError($"{nameof(EventFeedPanel)} requires an {nameof(EventFeedUiConfig)}.", this);
            enabled = false;
            return false;
        }

        if (!built)
            BuildUi();

        return built;
    }

    void BuildUi()
    {
        var parent = frame.ResolveParent(transform, "EventFeedCanvas");

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
            ApplyTextStyle(noDataText, uiConfig.rowFontSize, uiConfig.unavailableTextColor);
            noDataLayout.preferredHeight = uiConfig.rowMinHeight;
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

    EventFeedRowView CreateRow()
    {
        var rowGo = new GameObject("EventRow");
        rowGo.transform.SetParent(panelRoot, false);
        rowGo.AddComponent<RectTransform>();
        var layout = rowGo.AddComponent<LayoutElement>();
        var background = rowGo.AddComponent<Image>();
        background.raycastTarget = false;

        // Row layout lets wrapped text drive the row height.
        var rowLayout = rowGo.AddComponent<VerticalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        var text = CreateText(rowGo.transform, "Text", out _);

        var view = rowGo.AddComponent<EventFeedRowView>();
        view.Initialize(layout, rowLayout, background, text);
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
