using UnityEngine;

/// <summary>
/// Builds an invisible ring of box colliders from the active island's boundary data
/// and wall knobs on <see cref="IslandPlatformConfig"/>.
/// </summary>
public class IslandBoundaryWall : MonoBehaviour, IIslandBoundaryBuilder, IIslandSwapListener
{
    Transform _wallRoot;

    /// <inheritdoc />
    public void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig config)
    {
        Rebuild(definition, islandRoot, config);
    }

    /// <inheritdoc />
    public void OnIslandDespawning(IslandDefinition definition, Transform islandRoot)
    {
        Clear();
    }

    /// <inheritdoc />
    public void Rebuild(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig config)
    {
        Clear();

        if (definition == null || islandRoot == null || config == null)
            return;

        var boundary = definition.boundary;
        if (boundary.radius <= 0f)
            return;

        var segments = Mathf.Max(3, config.wallSegmentCount);
        var height = config.wallHeight;
        var thickness = config.wallThickness;
        var radius = boundary.radius;

        var root = new GameObject(nameof(IslandBoundaryWall));
        root.transform.SetParent(islandRoot, false);
        root.transform.localPosition = boundary.localCenter;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        _wallRoot = root.transform;

        var angleStep = 360f / segments;
        var chord = 2f * radius * Mathf.Sin(Mathf.PI / segments);

        for (var i = 0; i < segments; i++)
        {
            var angleDeg = angleStep * i;
            var angleRad = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            var segment = new GameObject($"Wall_{i}");
            segment.transform.SetParent(_wallRoot, false);
            segment.transform.localPosition = dir * radius;
            segment.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            segment.transform.localScale = Vector3.one;

            var box = segment.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = new Vector3(0f, height * 0.5f, 0f);
            box.size = new Vector3(chord, height, thickness);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_wallRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(_wallRoot.gameObject);
        else
            DestroyImmediate(_wallRoot.gameObject);

        _wallRoot = null;
    }
}
