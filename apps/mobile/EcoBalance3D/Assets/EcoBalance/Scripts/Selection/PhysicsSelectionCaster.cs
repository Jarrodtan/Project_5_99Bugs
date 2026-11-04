using UnityEngine;

/// <summary>
/// Default 3D physics caster. Uses <see cref="SelectionConfig"/> for mask, distance, and triggers,
/// then resolves <see cref="ISelectable"/> via GetComponentInParent.
/// </summary>
public sealed class PhysicsSelectionCaster : ISelectionCaster
{
    /// <inheritdoc />
    public bool TryGetSelectable(
        Camera camera,
        Vector2 screenPosition,
        SelectionConfig config,
        out ISelectable selectable)
    {
        selectable = null;

        if (camera == null || config == null)
            return false;

        var ray = camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(
                ray,
                out var hit,
                config.maxDistance,
                config.selectableLayers,
                config.triggerInteraction))
        {
            return false;
        }

        selectable = hit.collider.GetComponentInParent<ISelectable>();
        return selectable != null;
    }
}
