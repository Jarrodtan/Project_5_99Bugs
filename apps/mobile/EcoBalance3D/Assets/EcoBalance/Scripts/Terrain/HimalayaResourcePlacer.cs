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
    [Tooltip("Water patches placed in the lowest valleys when the online river data can't be loaded.")]
    [SerializeField] int fallbackWaterPatches = 12;
    [Tooltip("Minimum gap between fallback water patches, as a fraction of the map width.")]
    [SerializeField] float fallbackWaterSpacing = 0.06f;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    [Header("Meadows")]
    [Tooltip("Grass clumps per meadow (each is one food patch).")]
    [SerializeField] int meadowMinClumps = 3;
    [SerializeField] int meadowMaxClumps = 5;
    [Tooltip("Distance between neighbouring clumps in a meadow. About a clump's width, so they join into one lawn.")]
    [SerializeField] float meadowClumpSpacing = 4.2f;
    [Tooltip("Grass regrown per second on each patch (a full patch is 100).")]
    [SerializeField] float grassRegrowPerSecond = 1.5f;
    [Tooltip("A grass patch must regrow to at least this much before animals eat it again (a full patch is 100). " +
             "High values make grazers leave a cropped meadow and move on instead of waiting beside it.")]
    [SerializeField] float grassMinEdibleAmount = 60f;

    [Header("3D water")]
    [SerializeField] float streamWidth = 2.5f;
    [SerializeField] float riverWidth = 5f;
    [Tooltip("One traced river from a mountain down the valleys, dividing the map in two. Off = scattered map streams instead.")]
    [SerializeField] bool useRiver = true;
    [SerializeField] float riverSourceWidth = 4f;   // M3: wider so the river reads as a real river next to bigger animals
    [SerializeField] float riverMouthWidth = 12f;
    [Tooltip("River drinking spots refill this fast (a full spot is 100): meltwater keeps coming down the mountain.")]
    [SerializeField] float riverRefillPerSecond = 40f;
    // [AI-GENERATED END]

    readonly List<GameObject> spawned = new List<GameObject>();
    Mesh grassTuft;
    Material grassMaterial;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): shared with grass painted by TerrainEditTool

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>The grass resource, so the in-game terrain editor can tell grass patches from water.</summary>
    public ResourceDefinition GrassResource => grass;
    /// <summary>The traced river's root object (meshes + no-walk volumes), or null.</summary>
    public Transform RiverRoot { get; private set; }
    /// <summary>Grass is kept at least this far from the river centre line.</summary>
    public float RiverClearance => riverMouthWidth;

    /// <summary>
    /// Adds one grass patch at a world point, same look and regrowth as the generated meadows.
    /// Used by TerrainEditTool's Add Grass brush.
    /// </summary>
    public ResourcePatch SpawnGrassAt(Vector3 position)
    {
        if (grass == null) return null;
        if (grassMaterial == null) grassMaterial = MakeGrassMaterial();
        if (grassTuft == null) grassTuft = GrassTuftMesh.Build(config.seed);
        SpawnTuft(grass, position, Random.value * 360f, grassMaterial, $"Grass Patch (painted) {spawned.Count + 1}");
        return spawned[spawned.Count - 1].GetComponent<ResourcePatch>();
    }

    /// <summary>Name prefix of streams drawn with Edit Terrain's Draw Stream brush.</summary>
    public const string StreamName = "Stream (painted)";
    const float StreamSpotSpacing = 8f;   // a drinking spot every 8 m along a drawn stream
    Material streamMaterial;
    int drawnStreams;

    /// <summary>
    /// Adds a flowing stream along a line drawn on the map (terrain UVs), with drinking spots along it that
    /// refill like the river. Used by TerrainEditTool's Draw Stream brush. Null if the line is too short.
    /// </summary>
    public GameObject SpawnStreamAlong(HimalayaTerrainBuilder tb, List<Vector2> uvs)
    {
        if (water == null || uvs == null || uvs.Count < 2) return null;
        var mesh = WaterMeshBuilder.Stream(tb, uvs, streamWidth);
        if (mesh == null) return null;
        if (streamMaterial == null)
        {
            var shader = Shader.Find("EcoBalance/WaterFlow");
            streamMaterial = shader != null ? new Material(shader) { name = "Stream (flowing)" } : MakeMaterial(waterColor);
        }

        var go = new GameObject($"{StreamName} {++drawnStreams}");
        go.transform.SetParent(tb.Terrain.transform, false);   // the stream mesh is terrain-local (WaterMeshBuilder)
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = streamMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        spawned.Add(go);

        var line = new List<Vector3>(uvs.Count);
        foreach (var uv in uvs) line.Add(tb.WorldPosition(uv.x, uv.y));
        HimalayaRiver.Tributaries.Add(line);
        go.AddComponent<WaterLine>().line = line;

        // Invisible drinking spots along it, children of the stream so they go when it is removed.
        var spotMat = MakeMaterial(waterColor);
        float carried = StreamSpotSpacing;
        for (int i = 0; i < line.Count; i++)
        {
            if (i > 0) carried += Vector3.Distance(line[i - 1], line[i]);
            if (carried < StreamSpotSpacing) continue;
            carried = 0f;
            Spawn(water, line[i], waterSize, spotMat, $"{go.name} drinking spot");
            var spot = spawned[spawned.Count - 1];
            spot.transform.SetParent(go.transform, true);
            if (spot.TryGetComponent(out Renderer r)) r.enabled = false;
            if (spot.TryGetComponent(out ResourcePatch patch)) patch.RegrowPerSecond = riverRefillPerSecond;
        }
        return go;
    }

    // [AI-GENERATED END]

    public void Clear()
    {
        foreach (var go in spawned) if (go != null) Destroy(go);
        spawned.Clear();
    }

    public void PlaceGrass(HimalayaTerrainBuilder tb, HimalayaLandCover landCover)
    {
        if (grass == null) return;
        var rng = new System.Random(config.seed + 1);
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Meadows: each seed spot grows into a lawn of several touching clumps, below the tree line
        // (alpine grassland, where blue sheep graze). Blades sway in the wind (EcoBalance/GrassWind).
        if (grassMaterial == null) grassMaterial = MakeGrassMaterial();
        var material = grassMaterial;
        if (grassTuft == null) grassTuft = GrassTuftMesh.Build(config.seed);
        // Alpine meadows grow ABOVE the tree line, up to the snow line (Khumbu: about 4,200-5,200 m).
        float meadowTop = config.snowLineMeters;
        int meadows = 0, placed = 0, tries = 0, maxTries = Mathf.Max(1, config.grassPatches) * 40;

        while (meadows < config.grassPatches && tries++ < maxTries)
        {
            float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
            if (!GoodGrassSpot(tb, landCover, u, v, meadowTop)) continue;
            meadows++;

            Vector3 centre = tb.WorldPosition(u, v);
            int clumps = meadowMinClumps + rng.Next(meadowMaxClumps - meadowMinClumps + 1);
            for (int c = 0; c < clumps; c++)
            {
                // First clump at the centre, the rest in a ring around it so the lawn has no gaps.
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = c == 0 ? 0f : meadowClumpSpacing * (0.8f + 0.5f * (float)rng.NextDouble());
                Vector3 p = centre + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
                Vector2 puv = tb.WorldToUV(p);
                if (c > 0 && !GoodGrassSpot(tb, landCover, puv.x, puv.y, meadowTop)) continue;

                SpawnTuft(grass, tb.WorldPosition(puv.x, puv.y), (float)rng.NextDouble() * 360f, material, $"Grass Patch {placed + 1}");
                placed++;
            }
        }
        Debug.Log($"[Himalaya] Meadows: {meadows}, grass patches: {placed}");
    }

    bool GoodGrassSpot(HimalayaTerrainBuilder tb, HimalayaLandCover landCover, float u, float v, float topMeters)
    {
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
        if (landCover != null && !IsGrassClass(landCover.ClassAt(u, v))) return false;
        if (tb.ElevationMeters(u, v) > topMeters) return false;
        // No land-cover map: keep grass out of the forest belt below the tree line.
        if (landCover == null && tb.ElevationMeters(u, v) < config.treeLineMeters) return false;
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): grass grows on the meadow beside the water, never on it: keep half the
        // widest river plus a tuft's width plus a gap clear of the river and the glacier streams.
        if (HimalayaRiver.DistanceToAnyWater(tb.WorldPosition(u, v)) < riverMouthWidth * 0.5f + meadowClumpSpacing + 2f) return false;
        return tb.SlopeDegrees(u, v) <= maxPatchSlope;
    }

    /// <summary>The shared wind-grass material (null until the meadows are placed).</summary>
    public static Material GrassMaterial { get; private set; }

    Material MakeGrassMaterial()
    {
        var shader = Shader.Find("EcoBalance/GrassWind");
        if (shader == null)
        {
            var m = MakeMaterial(grassColor);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);
            return m;
        }
        // M3 realism: alpine grass at 4,500 m is short, dry and olive, not bright lawn green.
        var grassMat = new Material(shader) { name = "Grass (wind)" };
        grassMat.SetColor("_BaseColor", new Color(0.18f, 0.25f, 0.11f, 1f));
        grassMat.SetColor("_TipColor", new Color(0.55f, 0.56f, 0.30f, 1f));
        GrassMaterial = grassMat;   // the weather dries it to straw in a heatwave (WeatherVisualController)
        return grassMat;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    /// <summary>
    /// One river from a mountain on one edge down the valleys to the other edge (see HimalayaRiver),
    /// with drinking spots along both banks. Call before the NavMesh bake so the river blocks walking.
    /// </summary>
    public void PlaceRiver(HimalayaTerrainBuilder tb)
    {
        if (!useRiver || water == null) return;
        var shader = Shader.Find("EcoBalance/WaterFlow");
        var mat = shader != null ? new Material(shader) { name = "River (flowing)" } : MakeMaterial(waterColor);

        var system = new GameObject("River System");
        system.transform.SetParent(transform, true);
        spawned.Add(system);
        RiverRoot = system.transform;

        var banks = HimalayaRiver.Build(tb, system.transform, mat, riverSourceWidth, riverMouthWidth);
        ClearTreesFromRiver(tb);
        var spotMat = MakeMaterial(waterColor);
        int placed = 0;
        // The 5x map has ~200 bank spots but the cap is lower. Taking the first N left only the top of the river
        // (near the glacier) with drinking spots, so animals downstream died of thirst. Spread the cap evenly instead.
        int cap = Mathf.Max(1, config.maxWaterPatches);
        float step = banks.Count > cap ? banks.Count / (float)cap : 1f;
        for (float k = 0f; k < banks.Count && placed < cap; k += step)
        {
            var p = banks[(int)k];
            Spawn(water, p, waterSize, spotMat, $"Water Patch {placed + 1}");
            if (spawned[spawned.Count - 1].TryGetComponent(out Renderer r)) r.enabled = false;   // invisible drinking spot
            if (spawned[spawned.Count - 1].TryGetComponent(out ResourcePatch spot))
                spot.RegrowPerSecond = riverRefillPerSecond;   // fed by the mountain: refills fast (seasons can slow it)
            placed++;
        }
        Debug.Log($"[Himalaya] River: {HimalayaRiver.Path.Count} points, {placed} drinking spots on the banks.");
    }

    // Trees are placed before the river exists, so drop any that ended up in the water or on its banks.
    void ClearTreesFromRiver(HimalayaTerrainBuilder tb)
    {
        var t = tb.Terrain;
        if (t == null || !HimalayaRiver.Exists) return;
        var td = t.terrainData;
        var keep = new List<TreeInstance>(td.treeInstances.Length);
        Vector3 origin = t.GetPosition();
        foreach (var tree in td.treeInstances)
        {
            Vector3 world = origin + Vector3.Scale(tree.position, td.size);
            if (HimalayaRiver.DistanceTo(world) > riverMouthWidth * 0.5f + 2.5f) keep.Add(tree);
        }
        int removed = td.treeInstances.Length - keep.Count;
        if (removed > 0) td.SetTreeInstances(keep.ToArray(), true);
        Debug.Log($"[Himalaya] Removed {removed} trees from the river.");
    }
    // [AI-GENERATED END]

    public void PlaceWater(HimalayaTerrainBuilder tb, List<WaterFeature> features, int maxRivers = -1)
    {
        if (water == null) return;
        if (useRiver && HimalayaRiver.Exists) return;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): the traced river replaces the scattered map streams
        if (features == null || features.Count == 0) { PlaceFallbackWater(tb); return; }
        if (maxRivers > 0) features = LimitRivers(features, maxRivers);
        var material = MakeMaterial(waterColor);
        int placed = 0;
        bool drewWater = BuildWaterVisuals(tb, features);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
        int firstWaterIndex = spawned.Count;                 // after the streams, so only drinking spots get hidden

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
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): with real streams drawn, the patches are just
        // invisible drinking spots along them, not blue boxes.
        if (drewWater)
            for (int i = firstWaterIndex; i < spawned.Count; i++)
                if (spawned[i] != null && spawned[i].TryGetComponent(out Renderer r)) r.enabled = false;

        Debug.Log($"[Himalaya] Water patches: {placed}");
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Flowing ribbons for streams and rivers, flat shimmering surfaces for lakes (EcoBalance/WaterFlow).
    bool BuildWaterVisuals(HimalayaTerrainBuilder tb, List<WaterFeature> features)
    {
        var shader = Shader.Find("EcoBalance/WaterFlow");
        if (shader == null) { Debug.LogWarning("[Himalaya] WaterFlow shader not found; keeping water boxes.", this); return false; }

        var flowing = new Material(shader) { name = "Stream (flowing)" };
        var still = new Material(shader) { name = "Lake (still)" };
        still.SetFloat("_FlowSpeed", 0.08f);
        still.SetFloat("_RippleScale", 0.3f);

        int streams = 0, lakes = 0;
        foreach (var f in features)
        {
            if (f.IsSpring) continue;
            Mesh mesh = f.IsArea ? WaterMeshBuilder.Lake(tb, f.UV)
                                 : WaterMeshBuilder.Stream(tb, f.UV, f.IsRiver ? riverWidth : streamWidth);
            if (mesh == null) continue;

            var go = new GameObject(f.IsArea ? $"Lake {lakes + 1}" : $"Stream {streams + 1}");
            go.transform.SetParent(transform, true);   // keeps it at the world origin: the mesh is in world space
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = f.IsArea ? still : flowing;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            spawned.Add(go);
            if (f.IsArea) lakes++; else streams++;
        }
        Debug.Log($"[Himalaya] 3D water: {streams} streams, {lakes} lakes.");
        return streams + lakes > 0;
    }
    // [AI-GENERATED END]

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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // No online river data (offline tablet, slow server): put water in the lowest gentle spots,
    // where streams would collect, so animals that need water can still drink.
    void PlaceFallbackWater(HimalayaTerrainBuilder tb)
    {
        var rng = new System.Random(config.seed + 2);
        var candidates = new List<(float elev, Vector2 uv)>();
        for (int i = 0; i < 3000; i++)
        {
            var uv = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            if (tb.SlopeDegrees(uv.x, uv.y) > maxPatchSlope) continue;
            candidates.Add((tb.ElevationMeters(uv.x, uv.y), uv));
        }
        candidates.Sort((a, b) => a.elev.CompareTo(b.elev));

        var material = MakeMaterial(waterColor);
        var used = new List<Vector2>();
        int placed = 0, wanted = Mathf.Min(fallbackWaterPatches, config.maxWaterPatches);
        foreach (var c in candidates)
        {
            if (placed >= wanted) break;
            bool tooClose = false;
            foreach (var u in used) if ((u - c.uv).sqrMagnitude < fallbackWaterSpacing * fallbackWaterSpacing) { tooClose = true; break; }
            if (tooClose) continue;
            if (TryPlace(tb, c.uv, material, ref placed)) used.Add(c.uv);
        }
        Debug.Log($"[Himalaya] No online water data; placed {placed} water patches in the lowest valleys.");
    }
    // [AI-GENERATED END]

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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // A clump of grass blades. ResourcePatch shrinks it as it is eaten and grows it back as it regrows.
    void SpawnTuft(ResourceDefinition def, Vector3 position, float spin, Material material, string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, true);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, spin, 0f));
        go.AddComponent<MeshFilter>().sharedMesh = grassTuft;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;   // hundreds of thin blades: shadows cost a lot on a tablet
        var patch = go.AddComponent<ResourcePatch>();
        patch.Configure(def);
        patch.RegrowPerSecond = grassRegrowPerSecond;
        patch.MinEdibleAmount = grassMinEdibleAmount;
        spawned.Add(go);
    }
    // [AI-GENERATED END]

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
