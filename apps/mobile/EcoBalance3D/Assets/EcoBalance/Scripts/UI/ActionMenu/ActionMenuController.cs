using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the placed FAB, bubbles, and modal to <see cref="ActionMenuConfig"/> and the category handlers.
/// Does not create menu objects. Prototype UI — not approved Feature Plan work and not a T04 deliverable.
/// </summary>
public class ActionMenuController : MonoBehaviour, ISimulationPauseGate
{
    [Tooltip("Action menu hub.")]
    [SerializeField]
    ActionMenuConfig config;

    [Tooltip("Shared tooltip look and timing. Required for bubble and FAB tooltips.")]
    [SerializeField]
    TooltipStyle tooltipStyle;

    [Tooltip("FAB button already in the hierarchy.")]
    [SerializeField]
    Button fabButton;

    [Tooltip("Rect rotated while the menu is open.")]
    [SerializeField]
    RectTransform fabRect;

    [Tooltip("Label on the FAB.")]
    [SerializeField]
    Text fabLabel;

    [Tooltip("Category stack. Inactive while the menu is closed.")]
    [SerializeField]
    GameObject categoryStack;

    [Tooltip("Settings panel. Inactive until a category bubble opens it.")]
    [SerializeField]
    GameObject modalRoot;

    [Tooltip("Modal title label.")]
    [SerializeField]
    Text modalTitle;

    [Tooltip("Button that closes the modal.")]
    [SerializeField]
    Button modalCloseButton;

    [Tooltip("Caption on the modal close button.")]
    [SerializeField]
    Text modalCloseLabel;

    [Tooltip("Slider rows already placed in the modal, in display order.")]
    [SerializeField]
    ActionMenuFieldView[] sliderViews = new ActionMenuFieldView[0];

    [Tooltip("Toggle rows already placed in the modal, in display order.")]
    [SerializeField]
    ActionMenuFieldView[] toggleViews = new ActionMenuFieldView[0];

    [Tooltip("Category handlers. Matched to hub entries by category id.")]
    [SerializeField]
    List<MonoBehaviour> handlerBehaviours = new();

    readonly List<IActionMenuCategoryHandler> _handlers = new();
    readonly ActionMenuSettings _settings = new();
    TooltipStyle _resolvedTooltipStyle;
    bool _menuOpen;
    bool _modalOpen;
    bool _seeded;

    /// <inheritdoc />
    public bool ShouldPause =>
        config != null && config.pauseSimulationWhileOpen && (_menuOpen || _modalOpen);

    void Awake()
    {
        CacheHandlers();
    }

    void OnEnable()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(ActionMenuController)} requires an {nameof(ActionMenuConfig)}.", this);
            return;
        }

        SeedSettings();
        _resolvedTooltipStyle = ResolveTooltipStyle();
        TooltipHost.Ensure(_resolvedTooltipStyle);
        ApplyLayout();
        WireFab();
        WireModalClose();
        BringAboveHud();
        SetMenuOpen(false);
        SetModalOpen(false);
    }

    /// <summary>
    /// The ecosystem overview is on a canvas loaded after this one, so it covers the button and
    /// steals the click. Draw the button, the category stack and the settings above that HUD,
    /// and under the journal and leave dialog.
    /// </summary>
    void BringAboveHud()
    {
        Raise(fabRect);
        if (categoryStack != null)
            Raise(categoryStack.transform);
        if (modalRoot != null)
            Raise(modalRoot.transform);
    }

    static void Raise(Transform target)
    {
        if (target == null)
            return;

        var canvas = target.GetComponent<Canvas>();
        if (canvas == null)
            canvas = target.gameObject.AddComponent<Canvas>();

        var parent = target.parent != null ? target.parent.GetComponentInParent<Canvas>() : null;
        if (parent != null)
            canvas.additionalShaderChannels = parent.additionalShaderChannels;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 40;

        if (target.GetComponent<GraphicRaycaster>() == null)
            target.gameObject.AddComponent<GraphicRaycaster>();
    }

    void OnDisable()
    {
        if (fabButton != null)
            fabButton.onClick.RemoveListener(ToggleMenu);

        if (modalCloseButton != null)
            modalCloseButton.onClick.RemoveListener(CloseModal);
    }

    void CacheHandlers()
    {
        _handlers.Clear();
        if (handlerBehaviours == null)
            return;

        for (var i = 0; i < handlerBehaviours.Count; i++)
        {
            if (handlerBehaviours[i] is IActionMenuCategoryHandler handler)
                _handlers.Add(handler);
        }
    }

    void SeedSettings()
    {
        if (_seeded || config.categories == null)
            return;

        for (var i = 0; i < config.categories.Count; i++)
        {
            var category = config.categories[i];
            if (category?.fields == null)
                continue;

            for (var f = 0; f < category.fields.Count; f++)
            {
                var field = category.fields[f];
                if (field == null || string.IsNullOrEmpty(field.id))
                    continue;

                if (field.kind == ActionMenuFieldKind.Slider)
                    _settings.SetSlider(field.id, field.sliderDefault);
                else
                    _settings.SetToggle(field.id, field.toggleDefault);
            }
        }

        _seeded = true;
    }

    void ApplyLayout()
    {
        if (fabRect != null)
        {
            fabRect.anchorMin = Vector2.right;
            fabRect.anchorMax = Vector2.right;
            fabRect.pivot = new Vector2(0.5f, 0.5f);
            fabRect.sizeDelta = new Vector2(config.bubbleDiameter, config.bubbleDiameter);
            fabRect.anchoredPosition = new Vector2(-config.fabMargin.x, config.fabMargin.y);
        }

        if (fabLabel != null)
            fabLabel.text = config.fabLabel ?? string.Empty;

        ApplyFabTooltip();

        if (modalRoot != null)
        {
            var modalRect = modalRoot.GetComponent<RectTransform>();
            if (modalRect != null)
            {
                modalRect.anchorMin = new Vector2(0.5f, 0.5f);
                modalRect.anchorMax = new Vector2(0.5f, 0.5f);
                modalRect.pivot = new Vector2(0.5f, 0.5f);
                modalRect.sizeDelta = config.modalSize;
                modalRect.anchoredPosition = Vector2.zero;
            }
        }

        if (modalCloseLabel != null)
            modalCloseLabel.text = config.modalCloseLabel ?? string.Empty;

        if (config.categories == null)
            return;

        var step = config.bubbleDiameter + config.stackSpacing;
        var visibleIndex = 0;
        for (var i = 0; i < config.categories.Count; i++)
        {
            var category = config.categories[i];
            if (category == null)
                continue;

            var bubble = FindCategoryBubble(category.id);
            if (bubble == null)
                continue;

            if (!category.enabled)
            {
                bubble.gameObject.SetActive(false);
                continue;
            }

            bubble.gameObject.SetActive(true);
            bubble.ApplyLabel(category.label);
            bubble.ApplyIcon(category.icon);
            bubble.ApplyDot(config.prototypeDotColor, config.prototypeDotSize);
            bubble.ApplyDiameter(config.bubbleDiameter);
            bubble.SetTooltip(category.label, _resolvedTooltipStyle);

            var bubbleRect = (RectTransform)bubble.transform;
            bubbleRect.anchorMin = Vector2.right;
            bubbleRect.anchorMax = Vector2.right;
            bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            bubbleRect.anchoredPosition = new Vector2(-config.fabMargin.x, config.fabMargin.y + (visibleIndex + 1) * step);
            visibleIndex++;

            var handler = FindHandler(category.id);
            var options = handler != null ? handler.GetOptions() : new List<ActionMenuOption>();
            BindOptions(bubble, options);
            WireCategoryButton(bubble, category);
        }
    }

    void BindOptions(ActionMenuBubble categoryBubble, IReadOnlyList<ActionMenuOption> options)
    {
        var row = categoryBubble.GetComponentInChildren<ActionMenuOptionRow>(true);
        if (row == null || row.Content == null || row.Viewport == null)
            return;

        var step = config.bubbleDiameter + config.stackSpacing;
        var visible = config.maxVisibleOptions > 0 ? config.maxVisibleOptions : options.Count;
        var visibleCount = Mathf.Min(visible, options.Count);
        var viewportWidth = visibleCount <= 0
            ? 0f
            : visibleCount * config.bubbleDiameter + Mathf.Max(0, visibleCount - 1) * config.stackSpacing;
        var contentWidth = options.Count <= 0
            ? 0f
            : options.Count * config.bubbleDiameter + Mathf.Max(0, options.Count - 1) * config.stackSpacing;

        var viewport = row.Viewport;
        viewport.anchorMin = new Vector2(0f, 0.5f);
        viewport.anchorMax = new Vector2(0f, 0.5f);
        viewport.pivot = new Vector2(1f, 0.5f);
        viewport.sizeDelta = new Vector2(viewportWidth, config.bubbleDiameter);
        viewport.anchoredPosition = new Vector2(-config.stackSpacing, 0f);

        var content = row.Content;
        content.anchorMin = new Vector2(1f, 0.5f);
        content.anchorMax = new Vector2(1f, 0.5f);
        content.pivot = new Vector2(1f, 0.5f);
        content.sizeDelta = new Vector2(contentWidth, config.bubbleDiameter);
        content.anchoredPosition = Vector2.zero;

        PlaceEdge(row.OutwardEdge, true);
        PlaceEdge(row.InwardEdge, false);

        var optionBubbles = row.Content.GetComponentsInChildren<ActionMenuBubble>(true);
        for (var i = 0; i < optionBubbles.Length; i++)
        {
            var bubble = optionBubbles[i];
            if (bubble.Role != ActionMenuBubbleRole.Option)
                continue;

            var match = FindOption(options, bubble.BindingId);
            bubble.gameObject.SetActive(match != null);
            if (match == null)
                continue;

            var icon = match.icon != null ? match.icon : ActionMenuPreview.Capture(match.preview);
            bubble.ApplyLabel(icon != null ? string.Empty : match.label);
            bubble.ApplyIcon(icon);
            bubble.ApplyDot(config.prototypeDotColor, config.prototypeDotSize);
            bubble.ApplyDiameter(config.bubbleDiameter);
            bubble.SetTooltip(match.label, _resolvedTooltipStyle);

            var optionIndex = IndexOfOption(options, match.id);
            var rect = (RectTransform)bubble.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-(optionIndex * step + config.bubbleDiameter * 0.5f), 0f);
            WireOptionButton(bubble);
        }
    }

    void PlaceEdge(ActionMenuEdgeScroll edge, bool outward)
    {
        if (edge == null)
            return;

        var rect = (RectTransform)edge.transform;
        if (outward)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
        }
        else
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
        }

        rect.sizeDelta = new Vector2(config.scrollEdgeWidth, 0f);
        rect.anchoredPosition = Vector2.zero;
    }

    void WireFab()
    {
        if (fabButton == null)
            return;

        fabButton.onClick.RemoveListener(ToggleMenu);
        fabButton.onClick.AddListener(ToggleMenu);
    }

    void ApplyFabTooltip()
    {
        if (fabButton == null)
            return;

        var trigger = fabButton.GetComponent<TooltipTrigger>();
        var tooltipText = config != null ? config.fabTooltip : null;
        if (string.IsNullOrEmpty(tooltipText) || _resolvedTooltipStyle == null)
        {
            if (trigger != null)
                trigger.Text = string.Empty;
            return;
        }

        if (trigger == null)
            trigger = fabButton.gameObject.AddComponent<TooltipTrigger>();
        trigger.Configure(tooltipText, _resolvedTooltipStyle);
    }

    TooltipStyle ResolveTooltipStyle()
    {
        if (tooltipStyle != null)
            return tooltipStyle;

        // Enter Play Mode without Domain Reload leaves newly added SerializeFields null
        // even when the scene YAML references an asset. Fall back so tooltips still work.
        return ScriptableObject.CreateInstance<TooltipStyle>();
    }

    void WireModalClose()
    {
        if (modalCloseButton == null)
            return;

        modalCloseButton.onClick.RemoveListener(CloseModal);
        modalCloseButton.onClick.AddListener(CloseModal);
    }

    void WireCategoryButton(ActionMenuBubble bubble, ActionMenuCategory category)
    {
        if (bubble.Button == null)
            return;

        bubble.Button.onClick.RemoveAllListeners();
        bubble.Button.onClick.AddListener(() => OpenModal(category));
    }

    void WireOptionButton(ActionMenuBubble bubble)
    {
        if (bubble.Button == null)
            return;

        bubble.Button.onClick.RemoveAllListeners();
        bubble.Button.onClick.AddListener(() => OnOptionClicked(bubble));
    }

    void ToggleMenu()
    {
        if (_modalOpen)
            SetModalOpen(false);

        SetMenuOpen(!_menuOpen);
    }

    void CloseMenu()
    {
        SetMenuOpen(false);
    }

    void CloseModal()
    {
        SetModalOpen(false);
    }

    void SetMenuOpen(bool open)
    {
        _menuOpen = open;
        if (categoryStack != null)
            categoryStack.SetActive(open);

        if (fabRect != null && config != null)
            fabRect.localEulerAngles = new Vector3(0f, 0f, open ? config.openRotationDegrees : 0f);
    }

    void SetModalOpen(bool open)
    {
        _modalOpen = open;
        if (modalRoot != null)
            modalRoot.SetActive(open);
    }

    void OpenModal(ActionMenuCategory category)
    {
        CloseMenu();
        SetModalOpen(true);
        if (modalTitle != null)
            modalTitle.text = category != null ? category.label : string.Empty;

        BindModalFields(category);
    }

    void BindModalFields(ActionMenuCategory category)
    {
        var sliders = new List<ActionMenuField>();
        var toggles = new List<ActionMenuField>();
        if (category?.fields != null)
        {
            for (var i = 0; i < category.fields.Count; i++)
            {
                var field = category.fields[i];
                if (field == null)
                    continue;

                if (field.kind == ActionMenuFieldKind.Slider)
                    sliders.Add(field);
                else
                    toggles.Add(field);
            }
        }

        BindViews(sliderViews, sliders, true);
        BindViews(toggleViews, toggles, false);
    }

    void BindViews(ActionMenuFieldView[] views, List<ActionMenuField> fields, bool sliders)
    {
        if (views == null)
            return;

        for (var i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view == null)
                continue;

            if (i >= fields.Count)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            var field = fields[i];
            view.gameObject.SetActive(true);
            view.SetCaption(field.label);
            if (sliders && view.Slider != null)
            {
                view.BindSlider(field.minimum, field.maximum, field.wholeNumbers, _settings.GetSlider(field.id), field.locked);
                var fieldId = field.id;
                view.Slider.onValueChanged.RemoveAllListeners();
                view.Slider.onValueChanged.AddListener(value => _settings.SetSlider(fieldId, value));
            }
            else if (!sliders && view.Toggle != null)
            {
                view.BindToggle(_settings.GetToggle(field.id), field.locked);
                var fieldId = field.id;
                view.Toggle.onValueChanged.RemoveAllListeners();
                view.Toggle.onValueChanged.AddListener(value => _settings.SetToggle(fieldId, value));
            }
        }
    }

    void OnOptionClicked(ActionMenuBubble bubble)
    {
        var row = bubble.GetComponentInParent<ActionMenuOptionRow>();
        if (row == null)
            return;

        var handler = FindHandler(row.CategoryId);
        handler?.OnOptionChosen(bubble.BindingId, _settings);
    }

    ActionMenuBubble FindCategoryBubble(string categoryId)
    {
        if (categoryStack == null)
            return null;

        var bubbles = categoryStack.GetComponentsInChildren<ActionMenuBubble>(true);
        for (var i = 0; i < bubbles.Length; i++)
        {
            if (bubbles[i].Role == ActionMenuBubbleRole.Category && bubbles[i].BindingId == categoryId)
                return bubbles[i];
        }

        return null;
    }

    IActionMenuCategoryHandler FindHandler(string categoryId)
    {
        for (var i = 0; i < _handlers.Count; i++)
        {
            if (_handlers[i] != null && _handlers[i].CategoryId == categoryId)
                return _handlers[i];
        }

        return null;
    }

    static ActionMenuOption FindOption(IReadOnlyList<ActionMenuOption> options, string id)
    {
        if (options == null)
            return null;

        for (var i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].id == id)
                return options[i];
        }

        return null;
    }

    static int IndexOfOption(IReadOnlyList<ActionMenuOption> options, string id)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].id == id)
                return i;
        }

        return 0;
    }
}
