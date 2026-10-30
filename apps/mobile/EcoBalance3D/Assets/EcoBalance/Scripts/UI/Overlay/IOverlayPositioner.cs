using UnityEngine;

/// <summary>
/// Placement module for the inspector overlay. Implementations read knobs only from
/// <see cref="InspectionUiConfig"/> via <see cref="OverlayPositionContext"/>.
/// </summary>
public interface IOverlayPositioner
{
    /// <summary>Mode this module implements.</summary>
    OverlayMode Mode { get; }

    /// <summary>
    /// Wires canvases, panel root, config, and camera resolver. Called when this module becomes active.
    /// </summary>
    /// <param name="context">Shared wiring bag.</param>
    void Bind(OverlayPositionContext context);

    /// <summary>
    /// Starts following a world-space transform (type-agnostic).
    /// </summary>
    /// <param name="worldAnchor">Selected object's transform.</param>
    void SetTarget(Transform worldAnchor);

    /// <summary>
    /// Stops following. Does not clear panel content.
    /// </summary>
    void ClearTarget();

    /// <summary>
    /// Per-frame placement tick. Returns false when the panel should be position-hidden
    /// (e.g. target behind camera) without clearing selection or content.
    /// </summary>
    bool UpdatePosition();
}
