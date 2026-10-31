using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Opens the shared tooltip on mouse hover (after a delay) or touch long-press.
/// </summary>
public class TooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerMoveHandler
{
    [Tooltip("Text shown in the tooltip. Empty text never opens.")]
    [SerializeField]
    string text = string.Empty;

    [Tooltip("Look and timing. When set, this trigger can Ensure the host itself.")]
    [SerializeField]
    TooltipStyle style;

    RectTransform rect;
    bool hoverArmed;
    bool hoverShown;
    bool externalHover;
    float hoverArmTime;
    bool pressArmed;
    bool pressShown;
    bool suppressClick;
    int pressPointerId = int.MinValue;
    Vector2 pressStart;
    float pressArmTime;

    static readonly List<RaycastResult> RaycastHits = new();

    /// <summary>Caption used for the text body.</summary>
    public string Text
    {
        get => text;
        set => text = value ?? string.Empty;
    }

    /// <summary>Style used to open the host when it is not already created.</summary>
    public TooltipStyle Style
    {
        get => style;
        set => style = value;
    }

    /// <summary>
    /// Sets caption and style together (used by the action menu).
    /// </summary>
    /// <param name="tooltipText">Caption. Empty disables the tooltip.</param>
    /// <param name="tooltipStyle">Shared look and timing.</param>
    public void Configure(string tooltipText, TooltipStyle tooltipStyle)
    {
        text = tooltipText ?? string.Empty;
        style = tooltipStyle;
        if (style != null)
            TooltipHost.Ensure(style);
    }

    /// <summary>
    /// Arms hover from another handler (e.g. an option-row scroll edge covering this bubble).
    /// </summary>
    public void ArmHoverExternal()
    {
        if (string.IsNullOrEmpty(text))
            return;

        EnsureReady();
        externalHover = true;
        hoverArmed = true;
        hoverShown = false;
        hoverArmTime = Time.unscaledTime;
    }

    /// <summary>
    /// Clears hover armed by <see cref="ArmHoverExternal"/>.
    /// </summary>
    public void DisarmHoverExternal()
    {
        externalHover = false;
        hoverArmed = false;
        hoverShown = false;
        TooltipHost.Instance?.Hide(this);
    }

    void Awake()
    {
        rect = transform as RectTransform;
    }

    void OnDisable()
    {
        CancelAll();
        TooltipHost.Instance?.Hide(this);
    }

    void Update()
    {
        if (string.IsNullOrEmpty(text))
            return;

        EnsureReady();
        var host = TooltipHost.Instance;
        if (host == null || host.Style == null)
            return;

        var activeStyle = host.Style;
        if (hoverArmed && !hoverShown && Time.unscaledTime - hoverArmTime >= activeStyle.hoverDelaySeconds)
        {
            hoverShown = true;
            ShowNow(host, activeStyle);
        }

        if (pressArmed && !pressShown && Time.unscaledTime - pressArmTime >= activeStyle.longPressSeconds)
        {
            pressShown = true;
            suppressClick = true;
            ShowNow(host, activeStyle);
        }
    }

    /// <inheritdoc />
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData == null || IsTouchPointer(eventData) || string.IsNullOrEmpty(text))
            return;

        // Options sit under categories. Input System also sends enter to parents, so without
        // this check the category tooltip arms while hovering an option.
        if (!externalHover && !IsDeepestTriggerUnderPointer(eventData))
            return;

        EnsureReady();
        hoverArmed = true;
        hoverShown = false;
        hoverArmTime = Time.unscaledTime;
    }

    /// <inheritdoc />
    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData == null || IsTouchPointer(eventData))
            return;

        if (externalHover)
            return;

        hoverArmed = false;
        hoverShown = false;
        TooltipHost.Instance?.Hide(this);
    }

    /// <inheritdoc />
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || !IsTouchPointer(eventData) || string.IsNullOrEmpty(text))
            return;

        if (!IsDeepestTriggerUnderPointer(eventData))
            return;

        EnsureReady();
        pressArmed = true;
        pressShown = false;
        suppressClick = false;
        pressPointerId = eventData.pointerId;
        pressStart = eventData.position;
        pressArmTime = Time.unscaledTime;
    }

    /// <inheritdoc />
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null || !IsTouchPointer(eventData) || eventData.pointerId != pressPointerId)
            return;

        if (suppressClick)
            eventData.eligibleForClick = false;

        pressArmed = false;
        pressShown = false;
        pressPointerId = int.MinValue;
        TooltipHost.Instance?.Hide(this);
        suppressClick = false;
    }

    /// <inheritdoc />
    public void OnPointerMove(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (hoverArmed && !externalHover && !IsDeepestTriggerUnderPointer(eventData))
        {
            hoverArmed = false;
            hoverShown = false;
            TooltipHost.Instance?.Hide(this);
        }

        if (!pressArmed || eventData.pointerId != pressPointerId)
            return;

        var host = TooltipHost.Instance;
        if (host == null || host.Style == null)
            return;

        if ((eventData.position - pressStart).sqrMagnitude >= host.Style.longPressMoveSlop * host.Style.longPressMoveSlop)
        {
            pressArmed = false;
            pressShown = false;
            pressPointerId = int.MinValue;
            host.Hide(this);
        }
    }

    void ShowNow(TooltipHost host, TooltipStyle activeStyle)
    {
        if (rect == null)
            rect = transform as RectTransform;
        host.Show(rect, new TextTooltipContent(text, activeStyle), this);
    }

    void EnsureReady()
    {
        if (style != null)
            TooltipHost.Ensure(style);
    }

    void CancelAll()
    {
        hoverArmed = false;
        hoverShown = false;
        externalHover = false;
        pressArmed = false;
        pressShown = false;
        suppressClick = false;
        pressPointerId = int.MinValue;
    }

    /// <summary>
    /// True when this trigger is the deepest one under the pointer, including hits behind scroll edges.
    /// </summary>
    bool IsDeepestTriggerUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return true;

        RaycastHits.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastHits);

        TooltipTrigger deepest = null;
        var deepestRank = -1;
        for (var i = 0; i < RaycastHits.Count; i++)
        {
            var trigger = RaycastHits[i].gameObject.GetComponentInParent<TooltipTrigger>();
            if (trigger == null || string.IsNullOrEmpty(trigger.text))
                continue;

            var rank = HierarchyDepth(trigger.transform);
            if (rank > deepestRank)
            {
                deepestRank = rank;
                deepest = trigger;
            }
        }

        return deepest == this;
    }

    static int HierarchyDepth(Transform t)
    {
        var depth = 0;
        while (t != null)
        {
            depth++;
            t = t.parent;
        }

        return depth;
    }

    /// <summary>
    /// Input System mouse/pen use a device id as <see cref="PointerEventData.pointerId"/> (often ≥ 0),
    /// so pointerId &lt; 0 is not a reliable mouse check.
    /// </summary>
    static bool IsTouchPointer(PointerEventData eventData)
    {
        if (eventData is ExtendedPointerEventData extended)
            return extended.pointerType == UIPointerType.Touch;

        return eventData.pointerId >= 0;
    }
}
