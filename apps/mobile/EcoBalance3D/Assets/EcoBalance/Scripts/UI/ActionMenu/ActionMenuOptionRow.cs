using UnityEngine;

/// <summary>
/// Option row already placed under a category bubble. Holds the viewport, content, and scroll edges.
/// </summary>
public class ActionMenuOptionRow : MonoBehaviour
{
    [Tooltip("Same id as the parent category bubble and the hub entry.")]
    [SerializeField]
    string categoryId = string.Empty;

    [Tooltip("Visible window. Width is set from the hub's max visible count.")]
    [SerializeField]
    RectTransform viewport;

    [Tooltip("Parent of the option bubbles. Moved by the scroll edges.")]
    [SerializeField]
    RectTransform content;

    [Tooltip("Hover target on the side that reveals further options.")]
    [SerializeField]
    ActionMenuEdgeScroll outwardEdge;

    [Tooltip("Hover target on the side that returns toward the category.")]
    [SerializeField]
    ActionMenuEdgeScroll inwardEdge;

    /// <summary>Category id this row belongs to.</summary>
    public string CategoryId => categoryId;

    /// <summary>Visible window.</summary>
    public RectTransform Viewport => viewport;

    /// <summary>Option bubble parent.</summary>
    public RectTransform Content => content;

    /// <summary>Edge that scrolls toward options farther from the category.</summary>
    public ActionMenuEdgeScroll OutwardEdge => outwardEdge;

    /// <summary>Edge that scrolls back toward the category.</summary>
    public ActionMenuEdgeScroll InwardEdge => inwardEdge;
}
