using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runs the whole pipeline in order: terrain -> painting -> trees -> grass patches -> NavMesh bake
/// -> start the simulation clock. Water and sightings download after the mesh exists so animals
/// can be created while those requests are still in flight.
/// Stages honor <see cref="HimalayaBuildProfile"/> when assigned.
/// Two ways to start it: tick Build On Start, or let TerrainConfig -> AutoBakeFloor.Bake call BuildFromCounts.
/// </summary>
public class HimalayaWorldBuilder : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    [Tooltip("Mountain vs flat-grass cut. Leave empty to run the full mountain pipeline.")]
    [SerializeField] HimalayaBuildProfile profile;

    [SerializeField] HimalayaTerrainBuilder terrainBuilder;
    [SerializeField] HimalayaSplatPainter splatPainter;
    [SerializeField] HimalayaTreePlacer treePlacer;
    [SerializeField] HimalayaWaterLoader waterLoader;
    [SerializeField] HimalayaResourcePlacer resourcePlacer;
    [SerializeField] HimalayaSightings sightings;

    [Tooltip("NavMeshSurface on the terrain, baked at the end. Created at runtime if empty.")]
    [SerializeField] NavMeshSurface navSurface;

    [Tooltip("Enabled once the world is ready.")]
    [SerializeField] SimulationClock clockToEnable;
    readonly List<SimulationClock> heldClocks = new();   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

    [Tooltip("Leave OFF when TerrainConfig / AutoBakeFloor starts the build, or it would run twice.")]
    [SerializeField] bool buildOnStart = false;

    [Tooltip("Show the in-game Edit Terrain panel (sculpt hills, paint grass) once the world is built.")]
    [SerializeField] bool enableTerrainEditor = true;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

    [Tooltip("Editor only: load the saved NavMesh like a device does, instead of baking fresh and saving it.")]
    [SerializeField] bool usePrebakedInEditor = false;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

    public HimalayaTerrainBuilder TerrainBuilder => terrainBuilder;
    public HimalayaSightings Sightings => sightings;
    public bool IsReady { get; private set; }
    /// <summary>0-1, how far the world build has got (shown as % on the loading card).</summary>
    public float Progress { get; private set; }

    /// <summary>Raised when the terrain, grass and NavMesh are ready (online water/sightings may still be loading).</summary>
    public event Action Built;

    int treeCap = -1;    // -1 = use the config's maxTrees
    int riverCap = -1;   // -1 = use every river found
    bool building;

    bool PlaceTrees => profile == null || profile.placeTrees;
    bool PlaceWater => profile == null || profile.placeWater;
    bool PlaceGrassPatches => profile == null || profile.placeGrassPatches;

    void OnDisable()
    {
        if (!Application.isPlaying)
            ReleaseNavMesh();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        building = false;
        ReleaseNavMesh();
    }

    void ReleaseNavMesh()
    {
        var surface = navSurface != null
            ? navSurface
            : terrainBuilder != null ? terrainBuilder.NavSurface : null;
        if (surface != null)
            surface.RemoveData();
    }

    IEnumerator Start()
    {
        // Don't tick before the world exists. Every clock on the same simulator is held, not just this one:
        // otherwise another species' clock takes over ticking the simulator while the world is still building.
        if (clockToEnable != null)
            foreach (var c in FindObjectsByType<SimulationClock>())
                if (c == clockToEnable || (c.simulator != null && c.simulator == clockToEnable.simulator && c.enabled))
                {
                    c.enabled = false;
                    heldClocks.Add(c);
                }
        if (buildOnStart) yield return Build();
    }

    /// <summary>
    /// Starts a build using the counts from TerrainConfig, on the box set in HimalayaConfig.
    /// Trees and rivers set how much is placed; hills and mud are accepted but not used yet.
    /// </summary>
    public void BuildFromCounts(int trees, int rivers, int hills, int mud)
    {
        if (building) { Debug.LogWarning("[Himalaya] Already building; ignoring the new request."); return; }
        config.ClearOverride();
        StartBuild(trees, rivers, hills, mud);
    }

    /// <summary>Same, but builds the world around a point (lat/lon) instead of the configured box.</summary>
    public void BuildFromCounts(int trees, int rivers, int hills, int mud, double centerLat, double centerLon)
    {
        if (building) { Debug.LogWarning("[Himalaya] Already building; ignoring the new request."); return; }
        config.SetCenter(centerLat, centerLon);
        StartBuild(trees, rivers, hills, mud);
    }

    void StartBuild(int trees, int rivers, int hills, int mud)
    {
        treeCap = trees > 0 ? trees * Mathf.Max(1, config.treesPerCount) : -1;
        riverCap = rivers > 0 ? rivers : -1;
        Debug.Log($"[Himalaya] Counts received: trees {trees} (-> up to {treeCap}), rivers {rivers}, hills {hills} and mud {mud} (unused).");
        StartCoroutine(Build());
    }

    [ContextMenu("Rebuild")]
    void RebuildFromMenu()
    {
        if (!building) StartCoroutine(Build());
    }

    public IEnumerator Build()
    {
        building = true;
        IsReady = false;
        Progress = 0.05f;
        // Load timing per step (Console: "[Himalaya] Load ms"), to find what makes the tablet wait.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        long tTerrain = 0, tPaint = 0, tGrass = 0, tTrees = 0;
        try
        {
            yield return terrainBuilder.Build();
            tTerrain = sw.ElapsedMilliseconds;
            Progress = 0.45f;
            if (!terrainBuilder.IsBuilt) yield break;

            if (navSurface == null)
                navSurface = terrainBuilder.NavSurface;

            HimalayaLandCover landCover = null;
            if (config.landCover != null && !config.HasOverride) landCover = new HimalayaLandCover(config.landCover);
            else if (config.landCover != null) Debug.Log("[Himalaya] Location came from another script, but the land-cover image only covers the configured box. Using elevation and slope rules instead.");
            else Debug.Log("[Himalaya] No land-cover image set; using elevation and slope rules only.");

            if (splatPainter != null) splatPainter.Paint(terrainBuilder, landCover);
            tPaint = sw.ElapsedMilliseconds;
            Progress = 0.55f;
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): maxTrees 0 in the config = no trees at all (not wanted for M2)

            Progress = 0.65f;
            if (resourcePlacer != null)
            {
                resourcePlacer.Clear();
                resourcePlacer.PlaceRiver(terrainBuilder);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): before the bake, so it blocks walking
                if (PlaceGrassPatches)
                    resourcePlacer.PlaceGrass(terrainBuilder, landCover);
            }
            tGrass = sw.ElapsedMilliseconds;
            // Trees after the river exists, so none are placed on the water.
            if (PlaceTrees && treePlacer != null && config.maxTrees > 0) treePlacer.Place(terrainBuilder, landCover, treeCap);

            // (Yande) Bake before water and sightings, and check the NavMesh really has triangles before
            // animals are spawned; an empty bake used to leave them unable to move.
            tTrees = sw.ElapsedMilliseconds;
            Progress = 0.8f;
            yield return null;                       // let the TerrainCollider pick up the new heights
            yield return null;
            var vertCount = BakeWalkSurface();
            if (vertCount == 0)
            {
                yield return null;
                vertCount = CountNavVerts();
            }
            Debug.Log($"[Himalaya] Load ms: terrain {tTerrain}, paint {tPaint - tTerrain}, river+grass {tGrass - tPaint}, trees {tTrees - tGrass}, navmesh {sw.ElapsedMilliseconds - tTrees}, total {sw.ElapsedMilliseconds}");
            if (vertCount > 0)
                Debug.Log($"[Himalaya] NavMesh baked: {vertCount} verts.", navSurface);
            else
                Debug.LogWarning("[Himalaya] NavMesh bake produced 0 verts — animals cannot path.", navSurface);

            if (vertCount > 0)
            {
                foreach (var held in heldClocks) if (held != null) held.enabled = true;
                heldClocks.Clear();
                Progress = 1f;
                IsReady = true;
                if (GetComponent<SeasonWeather>() == null) gameObject.AddComponent<SeasonWeather>();   // [AI-GENERATED] Claude Code (Claude Opus 5.5): seasons start with the world
                if (enableTerrainEditor)   // [AI-GENERATED] Claude Code (Claude Opus 5.5): in-game terrain/grass editor
                {
                    var editor = GetComponent<TerrainEditTool>();
                    if (editor == null) editor = gameObject.AddComponent<TerrainEditTool>();
                    editor.Init(terrainBuilder, resourcePlacer, navSurface, splatPainter);
                }
                Built?.Invoke();
            }

            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Online extras load after the world is playable, so a slow or missing
            // connection never holds up the animals.
            if (resourcePlacer != null && PlaceWater && waterLoader != null)
            {
                List<WaterFeature> water = null;
                yield return waterLoader.Fetch(r => water = r);
                resourcePlacer.PlaceWater(terrainBuilder, water, riverCap);
            }

            if (sightings != null) yield return sightings.FetchAll();
            // [AI-GENERATED END]
        }
        finally
        {
            building = false;
        }
    }

    int BakeWalkSurface()
    {
        if (navSurface == null && terrainBuilder != null)
            navSurface = terrainBuilder.NavSurface;
        if (navSurface == null)
        {
            Debug.LogError("[Himalaya] No NavMeshSurface — animals cannot path.", this);
            return 0;
        }

        var terrain = terrainBuilder != null ? terrainBuilder.Terrain : navSurface.GetComponent<Terrain>();
        Physics.SyncTransforms();
        navSurface.minRegionArea = 0.1f;

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
        // Same world as one baked in the editor: load that NavMesh instead of baking (instant on tablets).
        var key = WorldKey();
        var prebaked = PrebakedNavMesh.Load();
        var saved = prebaked != null ? prebaked.Find(key) : null;
#if UNITY_EDITOR
        // The editor always bakes fresh and refreshes the saved copy, so it follows code changes.
        // Tick "Use Pre-baked In Editor" to test the device path here.
        if (!usePrebakedInEditor) saved = null;
#endif
        if (saved != null)
        {
            navSurface.RemoveData();
            navSurface.navMeshData = saved;   // terrain edits later update it in memory only; the asset on disk is never written at runtime
            navSurface.AddData();
            int loadedVerts = CountNavVerts();
            Debug.Log($"[Himalaya] NavMesh loaded pre-baked ({key}), {loadedVerts} verts.");
            if (loadedVerts > 0) return loadedVerts;
            navSurface.RemoveData();
        }
        else Debug.Log(prebaked != null && prebaked.Find(key) != null
            ? $"[Himalaya] Pre-baked NavMesh {key} exists; editor bakes fresh anyway (tick Use Pre-baked In Editor to load it)."
            : $"[Himalaya] No pre-baked NavMesh for {key}; baking now.");
        // [AI-GENERATED END]

        var verts = BakeFromTerrain(navSurface, terrain);
        if (verts == 0)
        {
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.layerMask = ~(1 << 2);   // skip "Ignore Raycast" (decor trees)
            navSurface.defaultArea = 0;
            Coarse(navSurface);
            navSurface.BuildNavMesh();
            verts = CountNavVerts();
        }

#if UNITY_EDITOR
        if (verts > 0) PrebakedNavMesh.SaveInEditor(key, navSurface.navMeshData);
#endif
        return verts;
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
    /// <summary>
    /// Key for a pre-baked NavMesh, made only from the map's inputs (config, profile, tree/river counts,
    /// location) so it is identical on PC and tablet. Every placement step is seeded, so the same inputs
    /// always give the same world. Bump <see cref="NavBakeVersion"/> when placement code changes; the
    /// editor also re-bakes and refreshes the saved copy on every Play, so it follows code changes.
    /// </summary>
    const int NavBakeVersion = 1;

    string WorldKey()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(NavBakeVersion).Append('|').Append(config != null ? JsonUtility.ToJson(config) : "-");
        sb.Append('|').Append(profile != null ? JsonUtility.ToJson(profile) : "-");
        var bnd = config != null ? config.Bounds : default;
        sb.Append('|').Append(bnd.West.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
          .Append(',').Append(bnd.South.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
          .Append(',').Append(bnd.East.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
          .Append(',').Append(bnd.North.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append('|').Append(treeCap).Append('|').Append(riverCap);
        // Guided and Free Exploration build slightly different worlds (174k vs 172k NavMesh verts).
        sb.Append('|').Append(ScreenFlow.GuidedMode).Append(CameraModeController.MainSpecies);
        sb.Append('|').Append(HimalayaTreePlacer.MobileBuild);   // mobile places half the trees (trunks are no-walk spots)
        sb.Append('|').Append(PlaceTrees).Append(PlaceGrassPatches).Append(config != null && config.landCover != null);
        // Object references serialize as instance IDs, which differ per run and per device.
        var text = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\{""instanceID"":-?\d+\}", "ref");
        unchecked
        {
            ulong h = 1469598103934665603UL;
            foreach (char c in text) { h ^= c; h *= 1099511628211UL; }
            return h.ToString("x16");
        }
    }
    // [AI-GENERATED END]


    // The default NavMesh detail (voxel = agent radius / 3) gave 205k verts and 1.7 s of the 2.1 s load on PC
    // (more on a tablet). Animals don't need that: coarser voxels and bigger tiles bake several times faster.
    static void Coarse(NavMeshSurface surface)
    {
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.25f;   // 2 voxels per agent radius (0.5): coarse but still accurate around fords and trunks
        surface.overrideTileSize = true;
        surface.tileSize = 128;
    }

    static int BakeFromTerrain(NavMeshSurface surface, Terrain terrain)
    {
        if (surface == null || terrain == null || terrain.terrainData == null)
            return 0;

        // Collect all applicable scene geometry and NavMesh modifiers,
        // including the river's NavMeshModifierVolume components.
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = ~(1 << 2);   // skip "Ignore Raycast" (decor trees)
        surface.defaultArea = 0;
        Coarse(surface);

        Physics.SyncTransforms();

        // Build the NavMesh using the NavMeshSurface collection system.
        surface.BuildNavMesh();

        return CountNavVerts();
    }


    static int CountNavVerts()
    {
        var verts = NavMesh.CalculateTriangulation().vertices;
        return verts != null ? verts.Length : 0;
    }
}
