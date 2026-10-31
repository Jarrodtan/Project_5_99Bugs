using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Scrolls an option row while the pointer stays over this edge. Speed and direction come from the hub and this component.
/// Also forwards hover to the option bubble underneath so tooltips still work.
/// </summary>
public class ActionMenuEdgeScroll : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerClickHandler,
    IPointerMoveHandler
{
    [Tooltip("Action menu hub. Scroll speed is read from here.")]
    [SerializeField]
    ActionMenuConfig config;

    [Tooltip("Content rect to move.")]
    [SerializeField]
    RectTransform content;

    [Tooltip("Viewport used to clamp the scroll range.")]
    [SerializeField]
    RectTransform viewport;

    [Tooltip("Sign of content movement while hovered. Set on the placed edge object.")]
    [SerializeField]
    float direction = 1f;

    bool _hover;
    TooltipTrigger _forwardedTooltip;

    static readonly List<RaycastResult> RaycastHits = new();

    /// <summary>Starts scrolling while the pointer is over this edge.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
        ForwardTooltip(eventData);
    }

    /// <summary>Stops scrolling when the pointer leaves this edge.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
        ClearForwardedTooltip();
    }

    /// <inheritdoc />
    public void OnPointerMove(PointerEventData eventData)
    {
        if (_hover)
            ForwardTooltip(eventData);
    }

    /// <summary>
    /// Keeps this click on the edge. The edge is a child of the category bubble, so an unhandled press opens that bubble's settings.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
    }

    /// <summary>Runs the option under this edge when the press landed on an option bubble.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        var bubble = FindOptionBubble(eventData);
        if (bubble?.Button != null)
            bubble.Button.onClick.Invoke();
    }

    void Update()
    {
        if (!_hover || config == null || content == null || viewport == null)
            return;

        var position = content.anchoredPosition;
        position.x += direction * config.edgeScrollSpeed * Time.unscaledDeltaTime;
        var limit = Mathf.Max(0f, content.sizeDelta.x - viewport.sizeDelta.x);
        position.x = Mathf.Clamp(position.x, 0f, limit);
        content.anchoredPosition = position;
    }

    void OnDisable()
    {
        _hover = false;
        ClearForwardedTooltip();
    }

    void ForwardTooltip(PointerEventData eventData)
    {
        var bubble = FindOptionBubble(eventData);
        var trigger = bubble != null ? bubble.GetComponent<TooltipTrigger>() : null;
        if (trigger == _forwardedTooltip)
            return;

        ClearForwardedTooltip();
        _forwardedTooltip = trigger;
        _forwardedTooltip?.ArmHoverExternal();
    }

    void ClearForwardedTooltip()
    {
        if (_forwardedTooltip != null)
        {
            _forwardedTooltip.DisarmHoverExternal();
            _forwardedTooltip = null;
        }
    }

    static ActionMenuBubble FindOptionBubble(PointerEventData eventData)
    {
        if (EventSystem.current == null || eventData == null)
            return null;

        RaycastHits.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastHits);
        for (var i = 0; i < RaycastHits.Count; i++)
        {
            var bubble = RaycastHits[i].gameObject.GetComponentInParent<ActionMenuBubble>();
            if (bubble != null && bubble.Role == ActionMenuBubbleRole.Option)
                return bubble;
        }

        return null;
    }
}
