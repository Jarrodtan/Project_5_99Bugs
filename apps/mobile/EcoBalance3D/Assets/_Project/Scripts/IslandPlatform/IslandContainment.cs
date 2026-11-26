using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logical bounds for the active island. Exposes <see cref="IIslandBoundsQuery"/> and
/// clamps registered <see cref="IIslandBoundable"/> targets when the hub enables enforcement.
/// </summary>
public class IslandContainment : MonoBehaviour, IIslandBoundsQuery, IIslandSwapListener
{
    [Tooltip("Platform hub for enforceLogicalBounds and world placement context.")]
    [SerializeField]
    IslandPlatformConfig config;

    [Tooltip("Boundable adapters to clamp each LateUpdate when enforcement is on.")]
    [SerializeField]
    List<MonoBehaviour> boundableBehaviours = new();

    IslandDefinition _definition;
    Transform _islandRoot;
    readonly List<IIslandBoundable> _boundables = new();

    /// <inheritdoc />
    public bool HasActiveBounds =>
        _definition != null && _islandRoot != null && _definition.boundary.radius > 0f;

    void Awake()
    {
        CacheBoundables();
    }

    /// <inheritdoc />
    public void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig platformConfig)
    {
        _definition = definition;
        _islandRoot = islandRoot;
        if (platformConfig != null)
            config = platformConfig;
    }

    /// <inheritdoc />
    public void OnIslandDespawning(IslandDefinition definition, Transform islandRoot)
    {
        _definition = null;
        _islandRoot = null;
    }

    /// <inheritdoc />
    public bool ContainsXZ(Vector3 worldPoint)
    {
        if (!HasActiveBounds)
            return true;

        return _definition.boundary.ContainsXZ(
            worldPoint,
            _islandRoot.position,
            _islandRoot.rotation);
    }

    /// <inheritdoc />
    public Vector3 ClampXZ(Vector3 worldPoint)
    {
        if (!HasActiveBounds)
            return worldPoint;

        return _definition.boundary.ClampXZ(
            worldPoint,
            _islandRoot.position,
            _islandRoot.rotation);
    }

    void LateUpdate()
    {
        if (config == null || !config.enforceLogicalBounds || !HasActiveBounds)
            return;

        for (var i = 0; i < _boundables.Count; i++)
        {
            var boundable = _boundables[i];
            if (boundable == null || boundable.BoundTransform == null)
                continue;

            var t = boundable.BoundTransform;
            t.position = ClampXZ(t.position);
        }
    }

    void CacheBoundables()
    {
        _boundables.Clear();
        if (boundableBehaviours == null)
            return;

        for (var i = 0; i < boundableBehaviours.Count; i++)
        {
            if (boundableBehaviours[i] is IIslandBoundable boundable)
                _boundables.Add(boundable);
        }
    }
}
