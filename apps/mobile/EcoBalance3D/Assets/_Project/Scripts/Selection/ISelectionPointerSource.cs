using UnityEngine;

/// <summary>
/// Provides screen-space press events for selection. Implementations must read bindings from
/// <see cref="SelectionConfig"/> — never hardcode input devices.
/// </summary>
public interface ISelectionPointerSource
{
    /// <summary>
    /// Enables any input actions required for selection.
    /// </summary>
    /// <param name="config">Selection config that holds the actions asset and action paths.</param>
    void Enable(SelectionConfig config);

    /// <summary>
    /// Disables input actions previously enabled.
    /// </summary>
    void Disable();

    /// <summary>
    /// Returns true on the frame a selection press begins, with the pointer's screen position.
    /// </summary>
    /// <param name="screenPosition">Screen-space position of the press.</param>
    /// <returns>True when a press occurred this frame.</returns>
    bool TryGetPress(out Vector2 screenPosition);
}
