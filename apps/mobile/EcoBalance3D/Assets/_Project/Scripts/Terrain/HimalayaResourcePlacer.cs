using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates ResourcePatch objects from the data: grass on grassland pixels, water along streams,
/// lake shores and springs. Same visuals as ResourcePatchSpawner, so animals use them unchanged.
/// Patches must sit on or within 2 units of the NavMesh, so bake the NavMesh after this runs.
/// </summary>
public class HimalayaResourcePlacer : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;
    [SerializeField] ResourceDefinition grass;
    [SerializeField] ResourceDefinition water;

    [SerializeField] Vector3 grassSize = new Vector3(1.2f, 0.25f, 1.2f);
    [SerializeField] Vector3 waterSize = new Vector3(1.5f, 0.2f, 1.5f);
    [SerializeField] Color grassColor = new Color(0.3f, 0.75f, 0.3f, 1f);
    [SerializeField] Color waterColor = new Color(0.25f, 0.55f, 0.95f, 1f);
    [Tooltip("Patches are skipped on slopes steeper than this (animals can't reach them).")]
    [SerializeField] float maxPatchSlope = 30f;

    readonly List<GameObject> spawned = new List<GameObject>();

    public void Clear()
    {
        foreach (var go in spawned) if (go != null) Destroy(go);
        spawned.Clear();
    }

    public void PlaceGrass(HimalayaTerrainBuilder tb, HimalayaLandCover landCover)
    {
        if (grass == null) return;
        var rng = new System.Random(config.seed + 1);
        var material = MakeMaterial(grassColor);
        int placed = 0, tries = 0, maxTries = Mathf.Max(1, config.grassPatches) * 40;

        while (placed < config.grassPatches && tries++ < maxTries)
        {
            float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
            if (landCover != null && !IsGrassClass(landCover.ClassAt(u, v))) continue;
            if (tb.ElevationMeters(u, v) > config.snowLineMeters) continue;
            if (tb.SlopeDegrees(u, v) > maxPatchSlope) continue;

            Spawn(grass, tb.WorldPosition(u, v), grassSize, material, $"Grass Patch {placed + 1}");
            placed++;
        }
        Debug.Log($"[Himalaya] Grass patches: {placed}");
    }

    public void PlaceWater(HimalayaTerrainBuilder tb, List<WaterFeature> features, int maxRivers = -1)
    {
        if (water == null) return;
        if (features == null || features.Count == 0)
        {
            PlaceValleyWater(tb);
            return;
        }
        if (maxRivers > 0) features = LimitRivers(features, maxRivers);
        var material = MakeMaterial(waterColor);
        int placed = 0;

        foreach (var f in features)
        {
            if (placed >= config.maxWaterPatches) break;

            if (f.IsSpring)
            {
                var uv = f.UV[0];
                if (Inside(uv) && TryPlace(tb, uv, material, ref placed)) { }
                continue;
            }

            // Walk the line and drop a patch every waterPatchSpacing world units.
            bool havePrev = false;
            Vector3 prev = default;
            float carried = config.waterPatchSpacing;       // places one at the start
            foreach (var uv in f.UV)
            {
                if (!Inside(uv)) { havePrev = false; continue; }
                var pos = tb.WorldPosition(uv.x, uv.y);
                if (havePrev) carried += Vector3.Distance(prev, pos);
                prev = pos;
                havePrev = true;

                if (carried >= config.waterPatchSpacing)
                {
                    carried = 0f;
                    if (placed >= config.maxWaterPatches) break;
                    TryPlace(tb, uv, material, ref placed);
                }
            }
        }
        Debug.Log($"[Himalaya] Water patches: {placed}");
    }

    // Used when the map service cannot be reached. Drops patches on the lowest gentle ground.
    void PlaceValleyWater(HimalayaTerrainBuilder tb)
    {
        var rng = new System.Random(config.seed + 2);
        var material = MakeMaterial(waterColor);
        int placed = 0, tries = 0, maxTries = Mathf.Max(1, config.maxWaterPatches) * 40;
        float span = Mathf.Max(1f, tb.MaxElevation - tb.MinElevation);

        while (placed < config.maxWaterPatches && tries++ < maxTries)
        {
            float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
            if (tb.SlopeDegrees(u, v) > maxPatchSlope) continue;
            float height = (tb.ElevationMeters(u, v) - tb.MinElevation) / span;
            if (height > 0.35f) continue;
            TryPlace(tb, new Vector2(u, v), material, ref placed);
        }

        Debug.Log($"[Himalaya] Water patches: {placed} (valley fallback)");
    }

    // Keeps the longest `max` rivers/streams, plus every lake and spring.
    static List<WaterFeature> LimitRivers(List<WaterFeature> all, int max)
    {
        var keep = new List<WaterFeature>();
        var lines = new List<WaterFeature>();
        foreach (var f in all)
        {
            if (f.IsSpring || f.IsArea) keep.Add(f);
            else lines.Add(f);
        }
        lines.Sort((a, b) => Length(b).CompareTo(Length(a)));
        if (lines.Count > max) lines.RemoveRange(max, lines.Count - max);
        keep.AddRange(lines);
        return keep;
    }

    static float Length(WaterFeature f)
    {
        float len = 0f;
        for (int i = 1; i < f.UV.Count; i++) len += Vector2.Distance(f.UV[i - 1], f.UV[i]);
        return len;
    }

    bool TryPlace(HimalayaTerrainBuilder tb, Vector2 uv, Material material, ref int placed)
    {
        if (tb.SlopeDegrees(uv.x, uv.y) > maxPatchSlope) return false;
        Spawn(water, tb.WorldPosition(uv.x, uv.y), waterSize, material, $"Water Patch {placed + 1}");
        placed++;
        return true;
    }

    static bool Inside(Vector2 uv) => uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;

    bool IsGrassClass(int c)
    {
        foreach (var g in config.grassClasses) if (g == c) return true;
        return false;
    }

    void Spawn(ResourceDefinition def, Vector3 position, Vector3 size, Material material, string objectName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(transform, true);
        go.transform.position = position + Vector3.up * (size.y * 0.5f);
        go.transform.localScale = size;
        Destroy(go.GetComponent<Collider>());          // visual only, so it doesn't block the NavMesh
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.AddComponent<ResourcePatch>().Configure(def);
        spawned.Add(go);
    }

    static Material MakeMaterial(Color color)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        Material m = pipeline != null && pipeline.defaultMaterial != null
            ? new Material(pipeline.defaultMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = color;
        return m;
    }
}
