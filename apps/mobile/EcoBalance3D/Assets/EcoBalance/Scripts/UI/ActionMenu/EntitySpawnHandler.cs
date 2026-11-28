using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Entity category: options come from the species list, or from prefab options when that list is empty.
/// Clicks spawn the matching visual. Navigation on the copy stays off when the scene has no NavMesh.
/// Prototype UI — not approved Feature Plan work.
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
    Coroutine _settle;

    static bool _rebuiltOnLoad;

    void OnDisable()
    {
        StopAllCoroutines();
        _settle = null;
    }

    void Start()
    {
        if (_rebuiltOnLoad)
            return;

        _rebuiltOnLoad = true;
        StartCoroutine(RebuildAfterLoad());
    }

    IEnumerator RebuildAfterLoad()
    {
        yield return null;
        yield return WaitForIslandSurface();
        if (!HasNavMesh())
            RebuildWalkSurface();
        if (TryGetIslandFootprint(out var footprint))
            PlaceAnimalsOnFootprint(footprint);
    }

    IEnumerator WaitForIslandSurface()
    {
        var builders = CollectWorldBuilders();
        if (builders.Count == 0)
            yield break;

        var wait = 0f;
        const float timeout = 120f;
        while (wait < timeout)
        {
            var ready = true;
            for (var i = 0; i < builders.Count; i++)
            {
                if (builders[i] != null && builders[i].isActiveAndEnabled && !builders[i].IsReady)
                {
                    ready = false;
                    break;
                }
            }

            if (ready)
                yield break;

            wait += Time.deltaTime;
            yield return null;
        }
    }

    List<HimalayaWorldBuilder> CollectWorldBuilders()
    {
        var builders = new List<HimalayaWorldBuilder>();
        var root = islandManager != null ? islandManager.IslandRoot : null;
        if (root != null)
            builders.AddRange(root.GetComponentsInChildren<HimalayaWorldBuilder>(true));

        if (builders.Count > 0)
            return builders;

        builders.AddRange(FindObjectsByType<HimalayaWorldBuilder>(FindObjectsInactive.Exclude));
        return builders;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLoadBake()
    {
        _rebuiltOnLoad = false;
    }

    /// <inheritdoc />
    public string CategoryId => categoryId;

    /// <inheritdoc />
    public IReadOnlyList<ActionMenuOption> GetOptions()
    {
        var results = new List<ActionMenuOption>();
        if (config == null || !config.TryGetCategory(categoryId, out var category))
            return results;

        if (category.species != null && category.species.Count > 0)
        {
            for (var i = 0; i < category.species.Count; i++)
            {
                var species = category.species[i];
                var id = SpeciesOptionId(species);
                if (string.IsNullOrEmpty(id))
                    continue;

                results.Add(new ActionMenuOption
                {
                    id = id,
                    label = id,
                    preview = species.visualPrefab
                });
            }

            return results;
        }

        if (category.prefabOptions == null)
            return results;

        for (var i = 0; i < category.prefabOptions.Count; i++)
        {
            var option = category.prefabOptions[i];
            if (option == null || option.prefab == null || string.IsNullOrEmpty(option.id))
                continue;

            results.Add(new ActionMenuOption
            {
                id = option.id,
                label = string.IsNullOrEmpty(option.label) ? option.id : option.label,
                preview = option.prefab
            });
        }

        return results;
    }

    /// <inheritdoc />
    public void OnOptionChosen(string optionId, ActionMenuSettings settings)
    {
        if (config == null || settings == null || !config.TryGetCategory(categoryId, out var category))
            return;

        var prefab = FindVisual(category, optionId);
        if (prefab == null)
        {
            Debug.Log($"[EntitySpawn] No visual for '{optionId}'. Nothing spawned.");
            return;
        }

        var count = Mathf.Max(0, Mathf.RoundToInt(settings.GetSlider(countFieldId)));
        if (count == 0)
        {
            Debug.Log($"[EntitySpawn] Spawn count is 0 for '{optionId}'. Nothing spawned.");
            return;
        }
        var scatter = settings.GetToggle(randomPositionFieldId);
        var pivot = islandManager != null
            ? islandManager.GetPivotPoint()
            : spawnRoot != null ? spawnRoot.position : Vector3.zero;
        var hasFootprint = TryGetIslandFootprint(out var footprint);
        var lift = FindSpecies(category, optionId) != null
            ? config.spawnSurfaceOffset
            : PivotAboveBottom(prefab);

        var fallback = hasFootprint
            ? FootprintFallback(footprint)
            : pivot + Vector3.up * lift;

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
        // A simulated species: each placed animal counts (+1 on the HUD, the graph and the cases) and joins its
        // spawner. Before, they were never counted but their deaths were, so placing animals made the count drift down.
        var species = FindSpecies(category, optionId);
        var spawner = CreatureSpawner.For(species);
        var simulator = spawner != null ? spawner.simulator : FindAnyObjectByType<EcosystemSimulator>();
        bool counted = species != null && simulator != null && simulator.populations.ContainsKey(species);
        // [AI-GENERATED END]

        for (var i = 0; i < count; i++)
        {
            if (counted && simulator.AddAnimals(species, 1) == 0)
            {
                Debug.Log($"[EntitySpawn] {optionId} is at its limit of {simulator.maxPopulationPerSpecies:0}. Spawned {i} of {count}.");
                break;
            }
            var position = ChoosePosition(pivot, scatter, hasFootprint, footprint, lift);
            position = SnapToSurface(position, fallback, hasFootprint, footprint);
            var instance = SpawnVisual(prefab, position);
            instance.name = prefab.name;
            if (counted && spawner != null) spawner.Adopt(instance);
            ActivateAiOnSurface(instance, position, fallback);
            AttachSpawnRay(instance);
            FlashSpawnLight(instance);
            Track(instance, instance.transform.position, hasFootprint, footprint, lift);
            Debug.Log($"[EntitySpawn] Spawned {instance.name} at {instance.transform.position}.", instance);
        }
    }

    GameObject SpawnVisual(GameObject prefab, Vector3 position)
    {
        var instance = Instantiate(prefab, Staging());
        // Keep the agent off until a mesh exists. Enabling it here is what logs
        // "Failed to create agent because there is no valid NavMesh".
        DisableAnimalBrain(instance);
        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.transform.SetParent(spawnRoot, true);
        return instance;
    }

    void ActivateAiOnSurface(GameObject instance, Vector3 position, Vector3 fallback)
    {
        if (instance == null)
            return;

        // Always finish on a coroutine so NavMesh bake + Warp happen after the island is ready.
        StartCoroutine(ActivateAiWhenWalkSurfaceReady(instance, position, fallback));
    }

    IEnumerator ActivateAiWhenWalkSurfaceReady(GameObject instance, Vector3 position, Vector3 fallback)
    {
        yield return WaitForIslandSurface();
        if (instance == null)
            yield break;

        // One bake only if the world builder did not leave a mesh. Rebuilding in a loop
        // removes the surface the builder just added and the agent then fails to create.
        if (!HasNavMesh())
        {
            RebuildWalkSurface();
            yield return null;
        }

        if (!HasNavMesh())
        {
            if (instance != null)
                Debug.LogWarning($"[EntitySpawn] {instance.name} could not join NavMesh — bake is empty. AI stays idle.", instance);
            yield break;
        }

        var lift = config != null ? config.spawnSurfaceOffset : 0f;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (instance == null)
                yield break;

            var probe = position;
            if (attempt > 0)
            {
                var radius = 5f * attempt;
                probe.x += Random.Range(-radius, radius);
                probe.z += Random.Range(-radius, radius);
            }

            if (TrySampleTerrainHeight(probe, out var terrainY))
                probe.y = terrainY + lift;

            PlaceOnWalkSurface(instance, probe, fallback);
            if (IsAiReady(instance))
            {
                EnableAnimalBrain(instance);
                yield break;
            }

            yield return null;
        }

        if (instance != null)
            Debug.LogWarning($"[EntitySpawn] {instance.name} could not join NavMesh — AI stays idle. Check that Himalaya finished baking NavMesh.", instance);
    }

    static bool IsAiReady(GameObject instance)
    {
        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null)
            return true;

        return agent.enabled && agent.isOnNavMesh;
    }

    void AttachSpawnRay(GameObject instance)
    {
        if (config == null || config.spawnRayLength <= 0f || instance == null)
            return;

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Spawn Ray";
        var collider = marker.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        marker.transform.SetParent(instance.transform, false);
        marker.transform.localRotation = Quaternion.identity;
        var lossy = instance.transform.lossyScale;
        var invX = 1f / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f);
        var invY = 1f / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f);
        var invZ = 1f / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f);
        var length = config.spawnRayLength;
        var width = config.spawnRayWidth;
        marker.transform.localScale = new Vector3(width * invX, length * 0.5f * invY, width * invZ);
        marker.transform.localPosition = new Vector3(0f, length * 0.5f * invY, 0f);

        var renderer = marker.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", config.spawnRayColor);
        block.SetColor("_Color", config.spawnRayColor);
        renderer.SetPropertyBlock(block);
    }

    void FlashSpawnLight(GameObject instance)
    {
        if (config == null || config.spawnLightSeconds <= 0f || instance == null)
            return;

        var marker = new GameObject("Spawn Light");
        marker.transform.SetParent(instance.transform, false);
        var lossy = instance.transform.lossyScale;
        var largest = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
        var inv = 1f / Mathf.Max(largest, 0.0001f);
        marker.transform.localScale = Vector3.one * inv;
        marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localPosition = new Vector3(0f, config.spawnLightRange * inv, 0f);

        var light = marker.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = config.spawnLightColor;
        light.intensity = config.spawnLightIntensity;
        light.range = config.spawnLightRange;
        light.spotAngle = config.spawnLightAngle;
        light.innerSpotAngle = config.spawnLightAngle * 0.55f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        StartCoroutine(TurnOffSpawnLight(marker, config.spawnLightSeconds));
    }

    static IEnumerator TurnOffSpawnLight(GameObject marker, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (marker != null)
            Destroy(marker);
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

    void RebuildWalkSurface()
    {
        var surface = FindWalkSurface();
        if (surface == null)
        {
            Debug.LogWarning("[EntitySpawn] No NavMeshSurface found on the active island.", this);
            return;
        }

        var agents = FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include);
        var wasEnabled = new bool[agents.Length];
        for (var i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            if (agent == null)
                continue;

            wasEnabled[i] = agent.enabled;
            agent.enabled = false;
            var modifier = agent.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = agent.gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }

        ConfigureWalkSurface(surface);
        Physics.SyncTransforms();
        surface.BuildNavMesh();

        var verts = NavMesh.CalculateTriangulation().vertices;
        var vertCount = verts != null ? verts.Length : 0;
        Debug.Log($"[EntitySpawn] NavMesh rebuilt on '{surface.name}' with {vertCount} verts.", surface);
        if (vertCount == 0)
        {
            Debug.LogWarning(
                "[EntitySpawn] NavMesh bake produced 0 verts — agents cannot activate. " +
                "Check that Himalaya finished building Terrain + TerrainCollider.",
                surface);
            return;
        }

        // Re-enable agents that were already walking; callers place new spawns afterward.
        for (var i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            if (agent == null || !wasEnabled[i])
                continue;

            if (!PlaceAgentOnMesh(agent, agent.transform.position, agent.transform.position))
                agent.enabled = false;
        }
    }

    static void ConfigureWalkSurface(NavMeshSurface surface)
    {
        if (surface == null)
            return;

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << 2);   // skip "Ignore Raycast" (decor trees)
        surface.defaultArea = 0;
    }

    NavMeshSurface FindWalkSurface()
    {
        var builders = CollectWorldBuilders();
        for (var i = 0; i < builders.Count; i++)
        {
            var terrainBuilder = builders[i] != null ? builders[i].TerrainBuilder : null;
            var onTerrain = terrainBuilder != null ? terrainBuilder.NavSurface : null;
            if (onTerrain != null && onTerrain.enabled)
                return onTerrain;
        }

        // Prefer the surface baked with the active island terrain — not a stale scene bake on IslandManager.
        if (islandManager != null && islandManager.IslandRoot != null)
        {
            var onIsland = islandManager.IslandRoot.GetComponentInChildren<NavMeshSurface>(true);
            if (onIsland != null && onIsland.enabled)
                return onIsland;
        }

        if (islandManager != null)
        {
            var onManager = islandManager.GetComponent<NavMeshSurface>();
            if (onManager != null && onManager.enabled)
                return onManager;
        }

        return FindAnyObjectByType<NavMeshSurface>();
    }

    static void PlaceOnWalkSurface(GameObject instance, Vector3 position, Vector3 fallback)
    {
        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            instance.transform.position = position;
            EnableAnimalBrain(instance);
            return;
        }

        if (PlaceAgentOnMesh(agent, position, fallback))
            EnableAnimalBrain(instance);
        else
            DisableAnimalBrain(instance);
    }

    static bool PlaceAgentOnMesh(NavMeshAgent agent, Vector3 position, Vector3 fallback)
    {
        if (agent == null)
            return false;

        agent.enabled = false;
        agent.transform.position = position;

        if (!HasNavMesh())
            return false;

        if (!TrySampleNavMeshNear(position, out var hit)
            && !TrySampleNavMeshNear(fallback, out hit))
            return false;

        agent.transform.position = hit.position;
        agent.enabled = true;
        agent.Warp(hit.position);
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            return false;
        }

        agent.ResetPath();
        agent.isStopped = false;
        return true;
    }

    static bool TrySampleNavMeshNear(Vector3 position, out NavMeshHit hit)
    {
        // Himalaya slopes often leave walkable patches farther than a small probe radius.
        for (var radius = 8f; radius <= 120f; radius *= 2f)
        {
            if (NavMesh.SamplePosition(position, out hit, radius, NavMesh.AllAreas))
                return true;

            var highProbe = position + Vector3.up * Mathf.Min(radius, 40f);
            if (NavMesh.SamplePosition(highProbe, out hit, radius, NavMesh.AllAreas))
                return true;
        }

        hit = default;
        return false;
    }

    Vector3 SnapToSurface(Vector3 position, Vector3 fallback, bool hasFootprint, Bounds footprint)
    {
        var lift = config != null ? config.spawnSurfaceOffset : 0f;

        // Terrain height is authoritative for Himalaya islands; old scene NavMeshes must not win.
        if (TrySampleTerrainHeight(position, out var terrainY))
        {
            position.y = terrainY + lift;
            if (TrySampleNavMeshNear(position, out var nearHit))
                return nearHit.position;
            return position;
        }

        if (TrySampleNavMeshNear(position, out var navHit))
            return navHit.position;

        if (TrySampleNavMeshNear(fallback, out navHit))
            return navHit.position;

        if (hasFootprint)
        {
            position.x = footprint.center.x;
            position.z = footprint.center.z;
            if (TrySampleTerrainHeight(position, out terrainY))
                position.y = terrainY + lift;
            else
                position.y = footprint.center.y + lift;
        }

        return position;
    }

    bool TrySampleTerrainHeight(Vector3 position, out float height)
    {
        height = 0f;
        var root = islandManager != null ? islandManager.IslandRoot : null;
        if (root == null)
            return false;

        var terrains = root.GetComponentsInChildren<Terrain>(true);
        for (var i = 0; i < terrains.Length; i++)
        {
            var terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
                continue;

            var local = position - terrain.transform.position;
            var size = terrain.terrainData.size;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
                continue;

            height = terrain.SampleHeight(position) + terrain.transform.position.y;
            return true;
        }

        return false;
    }

    static bool HasNavMesh()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        return triangulation.vertices != null && triangulation.vertices.Length > 0;
    }

    static void DisableAnimalBrain(GameObject instance)
    {
        var agents = instance.GetComponentsInChildren<NavMeshAgent>(true);
        for (var i = 0; i < agents.Length; i++)
            agents[i].enabled = false;

        // Only AnimalBrain. A second brain on the same object sets its own destinations.
        var brains = instance.GetComponentsInChildren<AnimalBrain>(true);
        for (var i = 0; i < brains.Length; i++)
            brains[i].enabled = false;

        var goalBrains = instance.GetComponentsInChildren<GoalOrientedAnimalBrain>(true);
        for (var i = 0; i < goalBrains.Length; i++)
            goalBrains[i].enabled = false;
    }

    static void EnableAnimalBrain(GameObject instance)
    {
        if (!HasNavMesh())
        {
            DisableAnimalBrain(instance);
            return;
        }

        var agents = instance.GetComponentsInChildren<NavMeshAgent>(true);
        for (var i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            if (agent == null)
                continue;

            if (!agent.enabled)
                agent.enabled = true;

            if (!agent.isOnNavMesh)
            {
                agent.enabled = false;
                continue;
            }

            agent.isStopped = false;
            agent.ResetPath();
        }

        var brains = instance.GetComponentsInChildren<AnimalBrain>(true);
        for (var i = 0; i < brains.Length; i++)
            brains[i].enabled = true;

        var goalBrains = instance.GetComponentsInChildren<GoalOrientedAnimalBrain>(true);
        for (var i = 0; i < goalBrains.Length; i++)
            goalBrains[i].enabled = false;
    }

    /// <inheritdoc />
    public void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig platformConfig)
    {
        if (!TryGetFootprint(islandRoot, out var footprint))
            return;

        var placements = new List<TrackedSpawn>(_spawned.Count);
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
            position.y = footprint.max.y + tracked.surfaceLift;
            tracked.pendingPosition = position;
            tracked.instance.transform.position = position;
            placements.Add(tracked);
        }

        if (_settle != null)
            StopCoroutine(_settle);
        _settle = StartCoroutine(SettleOnNewIsland(islandRoot, placements));
    }

    IEnumerator SettleOnNewIsland(Transform islandRoot, List<TrackedSpawn> placements)
    {
        yield return null;
        var wait = 0f;
        while (islandRoot != null && islandRoot.localScale.x < 0.99f && wait < 2f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        yield return WaitForIslandSurface();
        RebuildWalkSurface();
        if (!TryGetFootprint(islandRoot, out var footprint))
            footprint = new Bounds(islandRoot != null ? islandRoot.position : Vector3.zero, Vector3.one);

        var fallback = FootprintFallback(footprint);
        for (var i = 0; i < placements.Count; i++)
        {
            var tracked = placements[i];
            if (tracked.instance == null)
                continue;

            var pending = tracked.pendingPosition;
            pending = SnapToSurface(pending, fallback, true, footprint);
            tracked.instance.transform.position = pending;
            PlaceOnWalkSurface(tracked.instance, pending, fallback);
            if (IsAiReady(tracked.instance))
                EnableAnimalBrain(tracked.instance);
            else
                RestoreAnimal(tracked);
        }

        _settle = null;
    }

    /// <inheritdoc />
    public void OnIslandDespawning(IslandDefinition definition, Transform islandRoot)
    {
        if (!TryGetFootprint(islandRoot, out var footprint))
            return;

        AdoptAnimals(footprint);

        for (var i = _spawned.Count - 1; i >= 0; i--)
        {
            var tracked = _spawned[i];
            if (tracked.instance == null)
            {
                _spawned.RemoveAt(i);
                continue;
            }

            if (footprint.size.x <= 0.001f || footprint.size.z <= 0.001f)
                continue;

            RememberNormalized(tracked, tracked.instance.transform.position, footprint);
            FreezeAnimal(tracked);
        }
    }

    void AdoptAnimals(Bounds footprint)
    {
        if (config == null || !config.TryGetCategory(categoryId, out var category))
            return;

        if (category.species == null || category.species.Count == 0)
            return;

        var agents = FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude);
        for (var i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            if (agent == null || IsTracked(agent.gameObject))
                continue;

            if (agent.GetComponent<AnimalBrain>() == null && agent.GetComponent<GoalOrientedAnimalBrain>() == null)
                continue;

            var tracked = new TrackedSpawn
            {
                instance = agent.gameObject,
                surfaceLift = config != null ? config.spawnSurfaceOffset : 0f
            };
            RememberNormalized(tracked, agent.transform.position, footprint);
            _spawned.Add(tracked);
        }
    }

    bool IsTracked(GameObject instance)
    {
        for (var i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i].instance == instance)
                return true;
        }

        return false;
    }

    static void FreezeAnimal(TrackedSpawn tracked)
    {
        var agent = tracked.instance.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            tracked.agentWasEnabled = agent.enabled;
            agent.enabled = false;
        }

        var brain = tracked.instance.GetComponent<AnimalBrain>();
        if (brain != null)
        {
            tracked.animalBrainWasEnabled = brain.enabled;
            brain.enabled = false;
        }

        var goalBrain = tracked.instance.GetComponent<GoalOrientedAnimalBrain>();
        if (goalBrain != null)
        {
            tracked.goalBrainWasEnabled = goalBrain.enabled;
            goalBrain.enabled = false;
        }
    }

    static void RestoreAnimal(TrackedSpawn tracked)
    {
        var agent = tracked.instance.GetComponent<NavMeshAgent>();
        var onMesh = agent != null && agent.enabled && agent.isOnNavMesh;

        var brain = tracked.instance.GetComponent<AnimalBrain>();
        if (brain != null)
            brain.enabled = onMesh && tracked.animalBrainWasEnabled;

        var goalBrain = tracked.instance.GetComponent<GoalOrientedAnimalBrain>();
        if (goalBrain != null)
            goalBrain.enabled = onMesh && tracked.goalBrainWasEnabled;
    }

    void PlaceAnimalsOnFootprint(Bounds footprint)
    {
        var fallback = FootprintFallback(footprint);
        var brains = FindObjectsByType<AnimalBrain>(FindObjectsInactive.Exclude);
        for (var i = 0; i < brains.Length; i++)
        {
            var brain = brains[i];
            if (brain == null)
                continue;

            var agent = brain.GetComponent<NavMeshAgent>();
            if (agent == null)
                continue;

            PlaceAgentOnMesh(agent, agent.transform.position, fallback);
        }
    }

    Vector3 FootprintFallback(Bounds footprint)
    {
        var lift = config != null ? config.spawnSurfaceOffset : 0f;
        var point = new Vector3(footprint.center.x, footprint.center.y + lift, footprint.center.z);
        if (TrySampleTerrainHeight(point, out var terrainY))
            point.y = terrainY + lift;
        return point;
    }

    Vector3 ChoosePosition(Vector3 pivot, bool scatter, bool hasFootprint, Bounds footprint, float lift)
    {
        Vector3 position;
        if (hasFootprint && scatter)
            position = RandomOnFootprint(footprint, SpawnCoverage());
        else
        {
            position = pivot;
            if (!hasFootprint && scatter)
                position = ScatterAroundPivot(position);
        }

        if (TrySampleTerrainHeight(position, out var terrainY))
            position.y = terrainY + lift;
        else
            position.y = pivot.y + lift;

        return position;
    }

    float SpawnCoverage()
    {
        if (config == null)
            return 1f;

        var coverage = config.entitySpawnCoverage;
        if (coverage > 1f)
            return 1f;

        return Mathf.Clamp01(coverage);
    }

    static Vector3 RandomOnFootprint(Bounds footprint, float coverage)
    {
        var halfX = Mathf.Max(0f, footprint.extents.x * coverage);
        var halfZ = Mathf.Max(0f, footprint.extents.z * coverage);
        var minX = footprint.center.x - halfX;
        var maxX = footprint.center.x + halfX;
        var minZ = footprint.center.z - halfZ;
        var maxZ = footprint.center.z + halfZ;
        var insetX = (maxX - minX) * 0.04f;
        var insetZ = (maxZ - minZ) * 0.04f;
        if (minX + insetX < maxX - insetX)
        {
            minX += insetX;
            maxX -= insetX;
        }

        if (minZ + insetZ < maxZ - insetZ)
        {
            minZ += insetZ;
            maxZ -= insetZ;
        }

        return new Vector3(Random.Range(minX, maxX), footprint.center.y, Random.Range(minZ, maxZ));
    }

    Vector3 ScatterAroundPivot(Vector3 position)
    {
        var root = islandManager != null ? islandManager.IslandRoot : null;
        var definition = islandManager != null ? islandManager.ActiveDefinition : null;
        if (root != null && definition != null && definition.boundary.radius > 0f)
        {
            var offset = Random.insideUnitCircle * (definition.boundary.radius * SpawnCoverage());
            position.x += offset.x;
            position.z += offset.y;
            return definition.boundary.ClampXZ(position, root.position, root.rotation);
        }

        return position;
    }

    void Track(GameObject instance, Vector3 position, bool hasFootprint, Bounds footprint, float lift)
    {
        var tracked = new TrackedSpawn { instance = instance, surfaceLift = lift };
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

        if (TryGetTerrainFootprint(root, out footprint))
            return true;

        var found = false;
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (var i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            var world = FootprintBounds(root, filter);
            if (!found)
            {
                footprint = world;
                found = true;
            }
            else
            {
                footprint.Encapsulate(world);
            }
        }

        return found;
    }

    static bool TryGetTerrainFootprint(Transform root, out Bounds footprint)
    {
        footprint = default;
        var terrains = root.GetComponentsInChildren<Terrain>(true);
        var found = false;
        for (var i = 0; i < terrains.Length; i++)
        {
            var terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
                continue;

            var size = terrain.terrainData.size;
            if (size.x <= 0.01f || size.z <= 0.01f)
                continue;

            var corners = new[]
            {
                Vector3.zero,
                new Vector3(size.x, 0f, 0f),
                new Vector3(0f, 0f, size.z),
                new Vector3(size.x, 0f, size.z),
                new Vector3(size.x * 0.5f, size.y, size.z * 0.5f)
            };
            var world = new Bounds(terrain.transform.TransformPoint(corners[0]), Vector3.zero);
            for (var c = 1; c < corners.Length; c++)
                world.Encapsulate(terrain.transform.TransformPoint(corners[c]));

            if (!found)
            {
                footprint = world;
                found = true;
            }
            else
            {
                footprint.Encapsulate(world);
            }
        }

        return found;
    }

    static Bounds FootprintBounds(Transform root, MeshFilter filter)
    {
        var mesh = filter.sharedMesh.bounds;
        var localCenter = ToRootLocal(root, filter.transform, mesh.center, includePosition: true);
        var extentX = ToRootLocal(root, filter.transform, new Vector3(mesh.extents.x, 0f, 0f), includePosition: false);
        var extentY = ToRootLocal(root, filter.transform, new Vector3(0f, mesh.extents.y, 0f), includePosition: false);
        var extentZ = ToRootLocal(root, filter.transform, new Vector3(0f, 0f, mesh.extents.z), includePosition: false);
        var extents = new Vector3(
            Mathf.Abs(extentX.x) + Mathf.Abs(extentY.x) + Mathf.Abs(extentZ.x),
            Mathf.Abs(extentX.y) + Mathf.Abs(extentY.y) + Mathf.Abs(extentZ.y),
            Mathf.Abs(extentX.z) + Mathf.Abs(extentY.z) + Mathf.Abs(extentZ.z));
        var worldCenter = root.position + root.rotation * localCenter;
        var axisX = root.rotation * new Vector3(extents.x, 0f, 0f);
        var axisY = root.rotation * new Vector3(0f, extents.y, 0f);
        var axisZ = root.rotation * new Vector3(0f, 0f, extents.z);
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
        return new Bounds(worldCenter, extents * 2f);
    }

    static Vector3 ToRootLocal(Transform root, Transform meshTransform, Vector3 point, bool includePosition)
    {
        var current = point;
        var transform = meshTransform;
        while (transform != null && transform != root)
        {
            current = transform.localRotation * Vector3.Scale(transform.localScale, current);
            if (includePosition)
                current += transform.localPosition;
            transform = transform.parent;
        }

        return current;
    }

    static GameObject FindVisual(ActionMenuCategory category, string optionId)
    {
        var species = FindSpecies(category, optionId);
        if (species != null)
            return species.visualPrefab;

        if (category.prefabOptions == null)
            return null;

        for (var i = 0; i < category.prefabOptions.Count; i++)
        {
            var option = category.prefabOptions[i];
            if (option != null && option.prefab != null && option.id == optionId)
                return option.prefab;
        }

        return null;
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
        public Vector3 pendingPosition;
        public float surfaceLift;
        public bool agentWasEnabled = true;
        public bool animalBrainWasEnabled = true;
        public bool goalBrainWasEnabled;
    }

    static float PivotAboveBottom(GameObject prefab)
    {
        if (prefab == null)
            return 0f;

        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        var lowest = 0f;
        var found = false;
        for (var i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            var mesh = filter.sharedMesh;
            if (mesh == null)
                continue;

            var bottom = mesh.bounds.center;
            bottom.y -= mesh.bounds.extents.y;
            var inRoot = MeshPointInRoot(prefab.transform, filter.transform, bottom);
            if (!found || inRoot.y < lowest)
            {
                lowest = inRoot.y;
                found = true;
            }
        }

        return found ? -lowest : 0f;
    }

    static Vector3 MeshPointInRoot(Transform root, Transform meshTransform, Vector3 point)
    {
        var current = point;
        var transform = meshTransform;
        while (transform != null)
        {
            current = transform.localRotation * Vector3.Scale(transform.localScale, current);
            if (transform == root)
                break;

            current += transform.localPosition;
            transform = transform.parent;
        }

        return current;
    }
}
