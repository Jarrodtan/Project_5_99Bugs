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

    [Tooltip("Leave OFF when TerrainConfig / AutoBakeFloor starts the build, or it would run twice.")]
    [SerializeField] bool buildOnStart = false;

    [Tooltip("Show the in-game Edit Terrain panel (sculpt hills, paint grass) once the world is built.")]
    [SerializeField] bool enableTerrainEditor = true;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

    public HimalayaTerrainBuilder TerrainBuilder => terrainBuilder;
    public HimalayaSightings Sightings => sightings;
    public bool IsReady { get; private set; }

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
        if (clockToEnable != null) clockToEnable.enabled = false;   // don't tick before the world exists
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
        try
        {
            yield return terrainBuilder.Build();
            if (!terrainBuilder.IsBuilt) yield break;

            if (navSurface == null)
                navSurface = terrainBuilder.NavSurface;

            HimalayaLandCover landCover = null;
            if (config.landCover != null && !config.HasOverride) landCover = new HimalayaLandCover(config.landCover);
            else if (config.landCover != null) Debug.Log("[Himalaya] Location came from another script, but the land-cover image only covers the configured box. Using elevation and slope rules instead.");
            else Debug.Log("[Himalaya] No land-cover image set; using elevation and slope rules only.");

            if (splatPainter != null) splatPainter.Paint(terrainBuilder, landCover);
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): maxTrees 0 in the config = no trees at all (not wanted for M2)
            if (PlaceTrees && treePlacer != null && config.maxTrees > 0) treePlacer.Place(terrainBuilder, landCover, treeCap);

            if (resourcePlacer != null)
            {
                resourcePlacer.Clear();
                resourcePlacer.PlaceRiver(terrainBuilder);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): before the bake, so it blocks walking
                if (PlaceGrassPatches)
                    resourcePlacer.PlaceGrass(terrainBuilder, landCover);
            }

            // (Yande) Bake before water and sightings, and check the NavMesh really has triangles before
            // animals are spawned; an empty bake used to leave them unable to move.
            yield return null;                       // let the TerrainCollider pick up the new heights
            yield return null;
            var vertCount = BakeWalkSurface();
            if (vertCount == 0)
            {
                yield return null;
                vertCount = CountNavVerts();
            }
            if (vertCount > 0)
                Debug.Log($"[Himalaya] NavMesh baked: {vertCount} verts.", navSurface);
            else
                Debug.LogWarning("[Himalaya] NavMesh bake produced 0 verts — animals cannot path.", navSurface);

            if (vertCount > 0)
            {
                if (clockToEnable != null) clockToEnable.enabled = true;
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

        var verts = BakeFromTerrain(navSurface, terrain);
        if (verts == 0)
        {
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.layerMask = ~0;
            navSurface.defaultArea = 0;
            navSurface.BuildNavMesh();
            verts = CountNavVerts();
        }

        return verts;
    }

    static int BakeFromTerrain(NavMeshSurface surface, Terrain terrain)
    {
        if (surface == null || terrain == null || terrain.terrainData == null)
            return 0;

        var data = terrain.terrainData;
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Terrain,
                sourceObject = data,
                transform = Matrix4x4.TRS(terrain.transform.position, terrain.transform.rotation, Vector3.one),
                area = 0
            }
        };

        var settings = surface.GetBuildSettings();
        // Cliffs on the Himalaya cut were walkable at 60°, so animals climbed and slid.
        settings.agentSlope = Mathf.Min(settings.agentSlope, 40f);
        settings.minRegionArea = 0.1f;

        var worldCenter = terrain.transform.TransformPoint(data.size * 0.5f);
        var localCenter = surface.transform.InverseTransformPoint(worldCenter);
        var localSize = surface.transform.InverseTransformVector(data.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        var localBounds = new Bounds(localCenter, localSize);
        localBounds.Expand(2f);

        var mesh = NavMeshBuilder.BuildNavMeshData(
            settings, sources, localBounds, surface.transform.position, surface.transform.rotation);
        if (mesh == null)
            return 0;

        mesh.name = surface.gameObject.name;
        surface.RemoveData();
        surface.navMeshData = mesh;
        if (surface.isActiveAndEnabled)
            surface.AddData();
        return CountNavVerts();
    }

    static int CountNavVerts()
    {
        var verts = NavMesh.CalculateTriangulation().vertices;
        return verts != null ? verts.Length : 0;
    }
}
