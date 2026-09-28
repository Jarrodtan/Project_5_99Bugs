using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Entity category: options are the species list on the matching hub entry. Clicks spawn that species' visual prefab.
/// Navigation on the copy stays off when the scene has no NavMesh. Prototype UI — not approved Feature Plan work.
/// </summary>
public class EntitySpawnHandler : MonoBehaviour, IActionMenuCategoryHandler, IIslandSwapListener
{
    [Tooltip("Must match the entity category id on the action menu hub.")]
    [SerializeField]
    string categoryId = string.Empty;

    [Tooltip("Hub that holds the species list and scatter radius.")]
    [SerializeField]
    ActionMenuConfig config;

    [Tooltip("Modal field id that stores how many visuals to spawn.")]
    [SerializeField]
    string countFieldId = string.Empty;

    [Tooltip("Modal field id that stores whether spawn positions are scattered.")]
    [SerializeField]
    string randomPositionFieldId = string.Empty;

    [Tooltip("Pivot used when placing visuals. When empty, the spawn root position is used.")]
    [SerializeField]
    IslandManager islandManager;

    [Tooltip("Parent for spawned visuals.")]
    [SerializeField]
    Transform spawnRoot;

    readonly List<TrackedSpawn> _spawned = new();
    Transform _staging;

    /// <inheritdoc />
    public string CategoryId => categoryId;

    /// <inheritdoc />
    public IReadOnlyList<ActionMenuOption> GetOptions()
    {
        var results = new List<ActionMenuOption>();
        if (config == null || !config.TryGetCategory(categoryId, out var category) || category.species == null)
            return results;

        for (var i = 0; i < category.species.Count; i++)
        {
            var species = category.species[i];
            var id = SpeciesOptionId(species);
            if (string.IsNullOrEmpty(id))
                continue;

            results.Add(new ActionMenuOption
            {
                id = id,
                label = id
            });
        }

        return results;
    }

    /// <inheritdoc />
    public void OnOptionChosen(string optionId, ActionMenuSettings settings)
    {
        if (config == null || settings == null || !config.TryGetCategory(categoryId, out var category))
            return;

        var species = FindSpecies(category, optionId);
        if (species == null || species.visualPrefab == null)
            return;

        var count = Mathf.Max(0, Mathf.RoundToInt(settings.GetSlider(countFieldId)));
        var scatter = settings.GetToggle(randomPositionFieldId);
        var pivot = islandManager != null
            ? islandManager.GetPivotPoint()
            : spawnRoot != null ? spawnRoot.position : Vector3.zero;
        var hasFootprint = TryGetIslandFootprint(out var footprint);
        var navMeshReady = HasNavMesh();

        for (var i = 0; i < count; i++)
        {
            var position = ChoosePosition(pivot, scatter, hasFootprint, footprint);
            var instance = SpawnVisual(species.visualPrefab, position, navMeshReady);
            instance.name = species.visualPrefab.name;
            Track(instance, position, hasFootprint, footprint);
        }
    }

    GameObject SpawnVisual(GameObject prefab, Vector3 position, bool navMeshReady)
    {
        var instance = Instantiate(prefab, Staging());
        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        if (!navMeshReady)
            DisableNavigation(instance);

        instance.transform.SetParent(spawnRoot, true);
        return instance;
    }

    Transform Staging()
    {
        if (_staging != null)
            return _staging;

        var holder = new GameObject("Entity Spawn Staging");
        holder.SetActive(false);
        if (spawnRoot != null)
            holder.transform.SetParent(spawnRoot, false);

        _staging = holder.transform;
        return _staging;
    }

    static bool HasNavMesh()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        return triangulation.vertices != null && triangulation.vertices.Length > 0;
    }

    static void DisableNavigation(GameObject instance)
    {
        var agents = instance.GetComponentsInChildren<NavMeshAgent>(true);
        for (var i = 0; i < agents.Length; i++)
            agents[i].enabled = false;

        var brains = instance.GetComponentsInChildren<AnimalBrain>(true);
        for (var i = 0; i < brains.Length; i++)
            brains[i].enabled = false;
    }

    /// <inheritdoc />
    public void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig platformConfig)
    {
        if (!TryGetFootprint(islandRoot, out var footprint))
            return;

        var lift = config != null ? config.spawnSurfaceOffset : 0f;
        for (var i = _spawned.Count - 1; i >= 0; i--)
        {
            var tracked = _spawned[i];
            if (tracked.instance == null)
            {
                _spawned.RemoveAt(i);
                continue;
            }

            var position = tracked.instance.transform.position;
            position.x = AxisOnFootprint(footprint.min.x, footprint.size.x, footprint.center.x, tracked.normalizedX);
            position.z = AxisOnFootprint(footprint.min.z, footprint.size.z, footprint.center.z, tracked.normalizedZ);
            position.y = footprint.max.y + lift;
            tracked.instance.transform.position = position;
        }
    }

    /// <inheritdoc />
    public void OnIslandDespawning(IslandDefinition definition, Transform islandRoot)
    {
        if (!TryGetFootprint(islandRoot, out var footprint))
            return;

        for (var i = _spawned.Count - 1; i >= 0; i--)
        {
            var tracked = _spawned[i];
            if (tracked.instance == null)
            {
                _spawned.RemoveAt(i);
                continue;
            }

            RememberNormalized(tracked, tracked.instance.transform.position, footprint);
        }
    }

    Vector3 ChoosePosition(Vector3 pivot, bool scatter, bool hasFootprint, Bounds footprint)
    {
        if (hasFootprint)
        {
            var position = pivot;
            position.y = footprint.max.y + config.spawnSurfaceOffset;
            if (!scatter)
                return position;

            var radius = config.entityScatterRadius;
            var minX = Mathf.Max(footprint.min.x, pivot.x - radius);
            var maxX = Mathf.Min(footprint.max.x, pivot.x + radius);
            var minZ = Mathf.Max(footprint.min.z, pivot.z - radius);
            var maxZ = Mathf.Min(footprint.max.z, pivot.z + radius);
            if (minX > maxX)
                minX = maxX = footprint.center.x;
            if (minZ > maxZ)
                minZ = maxZ = footprint.center.z;

            position.x = Random.Range(minX, maxX);
            position.z = Random.Range(minZ, maxZ);
            return position;
        }

        var fallback = pivot;
        fallback.y += config.spawnSurfaceOffset;
        if (scatter)
        {
            var radius = config.entityScatterRadius;
            fallback.x += Random.Range(-radius, radius);
            fallback.z += Random.Range(-radius, radius);
        }

        var root = islandManager != null ? islandManager.IslandRoot : null;
        var definition = islandManager != null ? islandManager.ActiveDefinition : null;
        if (root != null && definition != null)
            fallback = definition.boundary.ClampXZ(fallback, root.position, root.rotation);

        return fallback;
    }

    void Track(GameObject instance, Vector3 position, bool hasFootprint, Bounds footprint)
    {
        var tracked = new TrackedSpawn { instance = instance };
        if (hasFootprint)
            RememberNormalized(tracked, position, footprint);
        _spawned.Add(tracked);
    }

    static void RememberNormalized(TrackedSpawn tracked, Vector3 position, Bounds footprint)
    {
        tracked.normalizedX = AxisNormalized(position.x, footprint.min.x, footprint.size.x);
        tracked.normalizedZ = AxisNormalized(position.z, footprint.min.z, footprint.size.z);
    }

    static float AxisNormalized(float value, float min, float size)
    {
        if (size <= 0f)
            return 0.5f;

        return Mathf.Clamp01((value - min) / size);
    }

    static float AxisOnFootprint(float min, float size, float center, float normalized)
    {
        if (size <= 0f)
            return center;

        return min + Mathf.Clamp01(normalized) * size;
    }

    bool TryGetIslandFootprint(out Bounds footprint)
    {
        var root = islandManager != null ? islandManager.IslandRoot : null;
        return TryGetFootprint(root, out footprint);
    }

    static bool TryGetFootprint(Transform root, out Bounds footprint)
    {
        footprint = default;
        if (root == null)
            return false;

        var renderers = root.GetComponentsInChildren<Renderer>();
        var found = false;
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!found)
            {
                footprint = renderer.bounds;
                found = true;
            }
            else
            {
                footprint.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static SpeciesDefinition FindSpecies(ActionMenuCategory category, string optionId)
    {
        if (category.species == null)
            return null;

        for (var i = 0; i < category.species.Count; i++)
        {
            var species = category.species[i];
            if (SpeciesOptionId(species) == optionId)
                return species;
        }

        return null;
    }

    static string SpeciesOptionId(SpeciesDefinition species)
    {
        if (species == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(species.speciesName))
            return species.speciesName;

        return species.name;
    }

    class TrackedSpawn
    {
        public GameObject instance;
        public float normalizedX = 0.5f;
        public float normalizedZ = 0.5f;
    }
}
