using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Scrolls an option row while the pointer stays over this edge. Speed and direction come from the hub and this component.
/// </summary>
public class ActionMenuEdgeScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
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

    static readonly List<RaycastResult> RaycastHits = new();

    /// <summary>Starts scrolling while the pointer is over this edge.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
    }

    /// <summary>Stops scrolling when the pointer leaves this edge.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
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
        if (EventSystem.current == null)
            return;

        RaycastHits.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastHits);
        for (var i = 0; i < RaycastHits.Count; i++)
        {
            if (RaycastHits[i].gameObject == gameObject)
                continue;

            var bubble = RaycastHits[i].gameObject.GetComponentInParent<ActionMenuBubble>();
            if (bubble == null || bubble.Role != ActionMenuBubbleRole.Option || bubble.Button == null)
                continue;

            bubble.Button.onClick.Invoke();
            return;
        }
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
}
