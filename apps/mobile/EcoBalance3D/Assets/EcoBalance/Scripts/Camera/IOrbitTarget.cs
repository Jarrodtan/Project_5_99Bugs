using System;
using UnityEngine;

/// <summary>
/// Pivot provider for <see cref="OrbitCameraController"/>. Keeps the camera free of
/// concrete Island Platform types — any MonoBehaviour can implement this.
/// </summary>
public interface IOrbitTarget
{
    /// <summary>World-space point the camera orbits around.</summary>
    Vector3 GetPivotPoint();

    /// <summary>
    /// Raised when the orbit pivot should be treated as changed (e.g. after an island spawn/swap).
    /// </summary>
    event Action OrbitPivotChanged;
}
