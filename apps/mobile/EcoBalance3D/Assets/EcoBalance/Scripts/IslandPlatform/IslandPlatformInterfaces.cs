using System.Collections;
using UnityEngine;

/// <summary>
/// Fades an island visual in or out during a swap. Duration comes from <see cref="IslandPlatformConfig"/>.
/// </summary>
public interface IIslandTransition
{
    /// <summary>
    /// Plays a fade on <paramref name="islandRoot"/> toward the given visible state.
    /// </summary>
    /// <param name="islandRoot">Root transform of the current island instance.</param>
    /// <param name="visible">True to fade in, false to fade out.</param>
    /// <param name="config">Platform hub supplying transition duration.</param>
    /// <returns>Coroutine enumerator for the fade.</returns>
    IEnumerator Transition(Transform islandRoot, bool visible, IslandPlatformConfig config);
}

// IEcosystemReset moved to the EcoSim package (packages/ecosim/Runtime/IEcosystemReset.cs)
// so the simulation core does not depend on app code. Island code uses it unchanged.

/// <summary>
/// Notified after an island is spawned or about to be destroyed during a swap.
/// </summary>
public interface IIslandSwapListener
{
    /// <summary>
    /// Called after a new island root is spawned and before fade-in.
    /// </summary>
    /// <param name="definition">Active island definition.</param>
    /// <param name="islandRoot">New island root transform.</param>
    /// <param name="config">Platform hub.</param>
    void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig config);

    /// <summary>
    /// Called before the current island root is destroyed.
    /// </summary>
    /// <param name="definition">Island being removed, or null.</param>
    /// <param name="islandRoot">Root about to be destroyed, or null.</param>
    void OnIslandDespawning(IslandDefinition definition, Transform islandRoot);
}

/// <summary>
/// Builds or clears the physical containment geometry for the active island.
/// </summary>
public interface IIslandBoundaryBuilder
{
    /// <summary>
    /// Rebuilds physical walls for <paramref name="definition"/> under <paramref name="islandRoot"/>.
    /// </summary>
    void Rebuild(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig config);

    /// <summary>Removes any previously built wall geometry.</summary>
    void Clear();
}

/// <summary>
/// Logical bounds query for the active island.
/// </summary>
public interface IIslandBoundsQuery
{
    /// <summary>True when logical containment is active for the current island.</summary>
    bool HasActiveBounds { get; }

    /// <summary>
    /// Returns true when <paramref name="worldPoint"/> is inside the active island bounds.
    /// </summary>
    bool ContainsXZ(Vector3 worldPoint);

    /// <summary>
    /// Clamps <paramref name="worldPoint"/> onto the active island bounds.
    /// </summary>
    Vector3 ClampXZ(Vector3 worldPoint);
}

/// <summary>
/// A transform that IslandContainment may clamp into island bounds.
/// </summary>
public interface IIslandBoundable
{
    /// <summary>World transform to read and write when clamping.</summary>
    Transform BoundTransform { get; }
}
