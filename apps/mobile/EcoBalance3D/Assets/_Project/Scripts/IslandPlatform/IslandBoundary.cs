using System;
using UnityEngine;

/// <summary>
/// Containment data for one island, authored on <see cref="IslandDefinition"/>.
/// XZ circle relative to the island root. Size and center live on the asset — not in code.
/// </summary>
[Serializable]
public struct IslandBoundary
{
    [Tooltip("Local-space center of the containment circle relative to the island root.")]
    public Vector3 localCenter;

    [Tooltip("Containment radius on the XZ plane. Leave at zero until authored on the asset.")]
    public float radius;

    /// <summary>
    /// Returns true when <paramref name="worldPoint"/> lies inside the authored XZ circle.
    /// When <see cref="radius"/> is at or below zero, containment is inactive and this returns true.
    /// </summary>
    /// <param name="worldPoint">World-space point to test.</param>
    /// <param name="islandRootWorld">World position of the island root.</param>
    /// <param name="islandRootRot">World rotation of the island root.</param>
    /// <returns>True when inside bounds, or when bounds are inactive.</returns>
    public bool ContainsXZ(Vector3 worldPoint, Vector3 islandRootWorld, Quaternion islandRootRot)
    {
        if (radius <= 0f)
            return true;

        var local = Quaternion.Inverse(islandRootRot) * (worldPoint - islandRootWorld) - localCenter;
        var dx = local.x;
        var dz = local.z;
        return dx * dx + dz * dz <= radius * radius;
    }

    /// <summary>
    /// Clamps <paramref name="worldPoint"/> onto the XZ circle when outside.
    /// When <see cref="radius"/> is at or below zero, returns the point unchanged.
    /// </summary>
    /// <param name="worldPoint">World-space point to clamp.</param>
    /// <param name="islandRootWorld">World position of the island root.</param>
    /// <param name="islandRootRot">World rotation of the island root.</param>
    /// <returns>Clamped world position (Y unchanged relative to input via local rebuild).</returns>
    public Vector3 ClampXZ(Vector3 worldPoint, Vector3 islandRootWorld, Quaternion islandRootRot)
    {
        if (radius <= 0f)
            return worldPoint;

        var local = Quaternion.Inverse(islandRootRot) * (worldPoint - islandRootWorld) - localCenter;
        var flat = new Vector2(local.x, local.z);
        if (flat.sqrMagnitude <= radius * radius)
            return worldPoint;

        flat = flat.normalized * radius;
        var clampedLocal = localCenter + new Vector3(flat.x, local.y, flat.y);
        return islandRootWorld + islandRootRot * clampedLocal;
    }
}
