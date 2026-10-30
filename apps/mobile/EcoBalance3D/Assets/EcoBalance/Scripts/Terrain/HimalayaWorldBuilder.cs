using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Runs the whole pipeline in order: terrain -> painting -> trees -> water + grass patches -> sightings
/// -> NavMesh bake -> start the simulation clock.
/// Two ways to start it: tick Build On Start, or let TerrainConfig -> AutoBakeFloor.Bake call BuildFromCounts.
/// </summary>
public class HimalayaWorldBuilder : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;
    [SerializeField] HimalayaTerrainBuilder terrainBuilder;
    [SerializeField] HimalayaSplatPainter splatPainter;
    [SerializeField] HimalayaTreePlacer treePlacer;
    [SerializeField] HimalayaWaterLoader waterLoader;
    [SerializeField] HimalayaResourcePlacer resourcePlacer;
    [SerializeField] HimalayaSightings sightings;

    [Tooltip("NavMeshSurface on the terrain, baked at the end.")]
    [SerializeField] NavMeshSurface navSurface;

    [Tooltip("Enabled once the world is ready.")]
    [SerializeField] SimulationClock clockToEnable;

    [Tooltip("Leave OFF when TerrainConfig / AutoBakeFloor starts the build, or it would run twice.")]
    [SerializeField] bool buildOnStart = false;

    public HimalayaTerrainBuilder TerrainBuilder => terrainBuilder;
    public HimalayaSightings Sightings => sightings;
    public bool IsReady { get; private set; }

    /// <summary>Raised when the terrain, grass and NavMesh are ready (online water/sightings may still be loading).</summary>
    public event Action Built;

    int treeCap = -1;    // -1 = use the config's maxTrees
    int riverCap = -1;   // -1 = use every river found
    bool building;

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

            HimalayaLandCover landCover = null;
            if (config.landCover != null && !config.HasOverride) landCover = new HimalayaLandCover(config.landCover);
            else if (config.landCover != null) Debug.Log("[Himalaya] Location came from another script, but the land-cover image only covers the configured box. Using elevation and slope rules instead.");
            else Debug.Log("[Himalaya] No land-cover image set; using elevation and slope rules only.");

            if (splatPainter != null) splatPainter.Paint(terrainBuilder, landCover);
            if (treePlacer != null) treePlacer.Place(terrainBuilder, landCover, treeCap);

            if (resourcePlacer != null)
            {
                resourcePlacer.Clear();
                resourcePlacer.PlaceRiver(terrainBuilder);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): before the bake, so it blocks walking
                resourcePlacer.PlaceGrass(terrainBuilder, landCover);
            }

            yield return null;                       // let the TerrainCollider pick up the new heights
            if (navSurface != null) navSurface.BuildNavMesh();
            if (clockToEnable != null) clockToEnable.enabled = true;

            IsReady = true;
            if (GetComponent<SeasonWeather>() == null) gameObject.AddComponent<SeasonWeather>();   // [AI-GENERATED] Claude Code (Claude Opus 5.5): seasons start with the world
            Built?.Invoke();

            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Online extras load after the world is playable, so a slow or missing
            // connection never holds up the animals.
            if (resourcePlacer != null && waterLoader != null)
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
}
