using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared EventSystem pointer-over-UI check used by Selection and Camera.
/// </summary>
public static class PointerOverUI
{
    /// <summary>
    /// Returns true when the current EventSystem reports the default pointer over a UI element.
    /// Prefer <see cref="IsOver(int)"/> for touch finger IDs.
    /// </summary>
    public static bool IsOver()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Returns true when the given pointer/touch id is over a UI element.
    /// </summary>
    public static bool IsOver(int pointerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
    }
}
