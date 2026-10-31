using UnityEngine;

/// <summary>
/// Body drawn inside the shared tooltip frame. Text is the first implementer; image and other bodies can follow.
/// </summary>
public interface ITooltipContent
{
    /// <summary>
    /// Fills <paramref name="body"/> and shows only this content's children, hiding any siblings left by another body type.
    /// </summary>
    /// <param name="body">Content root under the tooltip frame.</param>
    void Apply(RectTransform body);
}
