using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared EventSystem pointer-over-UI check used by Selection and Camera.
/// </summary>
public static class PointerOverUI
{
    /// <summary>
    /// Returns true when the current EventSystem reports the pointer over a UI element.
    /// </summary>
    public static bool IsOver()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
