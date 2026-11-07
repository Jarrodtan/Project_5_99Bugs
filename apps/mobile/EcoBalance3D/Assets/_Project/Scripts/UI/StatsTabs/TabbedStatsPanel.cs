using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Hosts several HUD panels (Displays 1–3 and the contextual Inspect tab) in one top-left panel, with a
/// tab bar to switch between them. Clicking a tab shows its content and hides the others; hiding is
/// display-only, so every tab keeps reading its data. Contextual tabs appear in the bar only while
/// <see cref="ITabbedPanelContent.IsTabAvailable"/> is true. Selection rules live in
/// <see cref="TabSelectionModel"/>; styling comes from <see cref="TabbedStatsUiConfig"/>.
/// </summary>
public class TabbedStatsPanel : MonoBehaviour
{
    [Tooltip("Components implementing ITabbedPanelContent (e.g. PopulationCounterPanel, InspectorPanel), in tab order.")]
    [SerializeField]
    List<MonoBehaviour> tabSources = new();

    [Tooltip("Tab bar layout and colors.")]
    [SerializeField]
    TabbedStatsUiConfig uiConfig;

    readonly List<ITabbedPanelContent> tabs = new();
    readonly List<Action> availabilityHandlers = new();
    readonly List<Action> focusHandlers = new();
    readonly List<Button> tabButtons = new();
    readonly List<TMP_Text> tabLabels = new();
    TabSelectionModel model;
    Canvas canvas;
    RectTransform root;
    RectTransform tabBar;
    HorizontalLayoutGroup tabBarLayout;
    RectTransform content;
    bool built;

    /// <summary>
    /// Index of the visible tab, or -1 before the UI is built or when no tab is available.
    /// </summary>
    public int SelectedIndex => model?.SelectedIndex ?? -1;

    /// <summary>
    /// Shows the tab at <paramref name="index"/> and hides the others. Ignored for hidden or out-of-range tabs.
    /// </summary>
    /// <param name="index">0-based tab index.</param>
    public void Select(int index)
    {
        if (built && model.Select(index))
            ApplySelection();
    }

    // Start rather than Awake so a scene-placed EventSystem has registered itself as current.
    void Start()
    {
        if (uiConfig == null)
        {
            Debug.LogError($"{nameof(TabbedStatsPanel)} requires a {nameof(TabbedStatsUiConfig)}.", this);
            enabled = false;
            return;
        }

        foreach (var source in tabSources)
        {
            if (source is ITabbedPanelContent tab)
                tabs.Add(tab);
            else if (source != null)
                Debug.LogError($"{nameof(TabbedStatsPanel)}: {source.GetType().Name} does not implement {nameof(ITabbedPanelContent)}.", this);
        }

        if (tabs.Count == 0)
        {
            Debug.LogError($"{nameof(TabbedStatsPanel)} has no tabs to host.", this);
            enabled = false;
            return;
        }

        model = new TabSelectionModel(tabs.Count, uiConfig.focusTabWhenRevealed);
        BuildUi();

        for (var i = 0; i < tabs.Count; i++)
        {
            model.SetAvailable(i, tabs[i].IsTabAvailable, allowFocus: false);
            var index = i;
            Action availabilityHandler = () => OnTabAvailabilityChanged(index);
            Action focusHandler = () => OnTabFocusRequested(index);
            availabilityHandlers.Add(availabilityHandler);
            focusHandlers.Add(focusHandler);
            tabs[i].TabAvailabilityChanged += availabilityHandler;
            tabs[i].TabFocusRequested += focusHandler;
        }

        model.SelectDefault(Mathf.Clamp(uiConfig.defaultTabIndex, 0, tabs.Count - 1));
        ApplySelection();
    }

    void OnDestroy()
    {
        for (var i = 0; i < availabilityHandlers.Count; i++)
        {
            tabs[i].TabAvailabilityChanged -= availabilityHandlers[i];
            tabs[i].TabFocusRequested -= focusHandlers[i];
        }

        availabilityHandlers.Clear();
        focusHandlers.Clear();
    }

    void OnValidate()
    {
        for (var i = tabSources.Count - 1; i >= 0; i--)
        {
            if (tabSources[i] != null && tabSources[i] is not ITabbedPanelContent)
            {
                Debug.LogWarning($"{nameof(TabbedStatsPanel)}: {tabSources[i].GetType().Name} does not implement {nameof(ITabbedPanelContent)}; clearing.", this);
                tabSources[i] = null;
            }
        }

        if (built)
            ApplyLayoutFromConfig();
    }

    void OnTabAvailabilityChanged(int index)
    {
        if (built && model.SetAvailable(index, tabs[index].IsTabAvailable))
            ApplySelection();
    }

    void OnTabFocusRequested(int index)
    {
        if (built && model.RequestFocus(index))
            ApplySelection();
    }

    void ApplySelection()
    {
        for (var i = 0; i < tabs.Count; i++)
        {
            tabButtons[i].gameObject.SetActive(model.IsAvailable(i));
            tabs[i].SetShown(i == model.SelectedIndex);
        }

        ApplyTabColors();
    }

    void BuildUi()
    {
        EnsureEventSystem();

        var canvasGo = new GameObject("TabbedStatsCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        root = CreateRect("StatsTabs", canvasGo.transform);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);

        tabBar = CreateRect("TabBar", root);
        SetTopStretch(tabBar);
        tabBarLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabBarLayout.childControlWidth = true;
        tabBarLayout.childControlHeight = true;
        tabBarLayout.childForceExpandWidth = true;
        tabBarLayout.childForceExpandHeight = true;

        content = CreateRect("Content", root);
        SetTopStretch(content);

        for (var i = 0; i < tabs.Count; i++)
            CreateTabButton(i);

        built = true;
        ApplyLayoutFromConfig();

        foreach (var tab in tabs)
            tab.EmbedIn(content);
    }

    void CreateTabButton(int index)
    {
        var buttonRect = CreateRect("Tab", tabBar);
        var background = buttonRect.gameObject.AddComponent<Image>();
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Select(index));

        var labelRect = CreateRect("Label", buttonRect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        label.text = tabs[index].TabLabel;

        tabButtons.Add(button);
        tabLabels.Add(label);
    }

    void ApplyLayoutFromConfig()
    {
        if (uiConfig == null)
            return;

        canvas.sortingOrder = uiConfig.canvasSortOrder;
        root.anchoredPosition = uiConfig.panelAnchoredPosition;
        root.sizeDelta = new Vector2(uiConfig.panelWidth, uiConfig.tabHeight);
        tabBar.sizeDelta = new Vector2(0f, uiConfig.tabHeight);
        tabBarLayout.spacing = uiConfig.tabSpacing;
        content.anchoredPosition = new Vector2(0f, -uiConfig.tabHeight);

        foreach (var label in tabLabels)
        {
            var pad = uiConfig.tabLabelPadding;
            label.rectTransform.offsetMin = new Vector2(pad, 0f);
            label.rectTransform.offsetMax = new Vector2(-pad, 0f);
            // Auto-size so labels still fit when a contextual tab narrows the others.
            label.enableAutoSizing = true;
            label.fontSizeMax = uiConfig.tabFontSize;
            label.fontSizeMin = Mathf.Min(uiConfig.tabMinFontSize, uiConfig.tabFontSize);
            if (uiConfig.font != null)
                label.font = uiConfig.font;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        ApplyTabColors();
    }

    void ApplyTabColors()
    {
        var selected = model?.SelectedIndex ?? -1;
        for (var i = 0; i < tabButtons.Count; i++)
        {
            var active = i == selected;
            var restColor = active ? uiConfig.activeTabColor : uiConfig.inactiveTabColor;

            // Image stays white and the ColorBlock carries the real colors, so hover fades work per state.
            tabButtons[i].targetGraphic.color = Color.white;
            var colors = tabButtons[i].colors;
            colors.normalColor = restColor;
            colors.highlightedColor = active ? restColor : uiConfig.hoverTabColor;
            colors.pressedColor = uiConfig.activeTabColor;
            colors.selectedColor = restColor;
            colors.disabledColor = restColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = uiConfig.tabColorFadeSeconds;
            tabButtons[i].colors = colors;

            tabLabels[i].color = active ? uiConfig.activeTextColor : uiConfig.inactiveTextColor;
        }
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void SetTopStretch(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
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
