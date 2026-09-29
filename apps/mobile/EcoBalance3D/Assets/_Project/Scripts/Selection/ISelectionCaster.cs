using UnityEngine;

/// <summary>
/// Resolves a screen-space press into an <see cref="ISelectable"/> using config-driven pick settings.
/// </summary>
public interface ISelectionCaster
{
    /// <summary>
    /// Attempts to find a selectable under the given screen position.
    /// </summary>
    /// <param name="camera">Camera used to build the pick ray.</param>
    /// <param name="screenPosition">Screen-space pointer position.</param>
    /// <param name="config">Raycast tunables (mask, distance, triggers).</param>
    /// <param name="selectable">Hit selectable, or null.</param>
    /// <returns>True when an <see cref="ISelectable"/> was found on the hit or a parent.</returns>
    bool TryGetSelectable(
        Camera camera,
        Vector2 screenPosition,
        SelectionConfig config,
        out ISelectable selectable);
}
