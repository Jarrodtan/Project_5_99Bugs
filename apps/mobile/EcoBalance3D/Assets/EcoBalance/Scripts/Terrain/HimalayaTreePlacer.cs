using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places trees on a jittered grid: only where WorldCover says "tree cover" (or anywhere if no
/// land-cover image is set), below the tree line, on gentle slopes.
/// M3: uses UXGD's models (Prefabs/Resources: Pine Tree, Dead Tree, Clutter Plant, by Nomitha) placed as
/// normal objects, so they keep their own materials and multi-part meshes (the Terrain tree system needs
/// one mesh + a Nature shader, which these models don't have). Falls back to Terrain tree prototypes.
/// No colliders: animals walk past and leopards still see through them (only mountains block sight).
/// </summary>
public class HimalayaTreePlacer : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;
    [Tooltip("Size of every tree. Raise if trees look tiny, lower if huge. Change, then press Play again.")]
    [SerializeField] float treeScale = 1f;
    [Tooltip("Most UXGD tree objects to place (each is a real object, so keep this low for tablets).")]
    [SerializeField] int maxObjectTrees = 600;
    [Tooltip("No trees closer than this to the river or a stream (world units).")]
    [SerializeField] float riverGap = 10f;
    [Tooltip("No trees closer than this to a grass clump (world units), so meadows stay open for grazing.")]
    [SerializeField] float grassGap = 5f;
    [Tooltip("Width of the no-walk spot around each trunk (world units).")]
    [SerializeField] float trunkSize = 1.2f;

    static readonly string[] UxgdTrees = { "Pine Tree", "Pine Tree", "Pine Tree", "Pine Tree", "Dead Tree" };   // Clutter Plant left out: palms + monstera are tropical, not Himalayan
    // Wanted height in world units (an adult Blue Sheep is about 1.5). Each model is scaled to this from its
    // own size, so it fits however the FBX was exported (cm or m).
    static readonly float[] UxgdHeights = { 7f, 7f, 7f, 7f, 5f };

    GameObject treeRoot;

    public void Place(HimalayaTerrainBuilder builder, HimalayaLandCover landCover, int maxTreesOverride = -1)
    {
        var td = builder.Terrain.terrainData;

        var models = new List<GameObject>();
        var heights = new List<float>();
        for (int i = 0; i < UxgdTrees.Length; i++)
        {
            var prefab = Resources.Load<GameObject>(UxgdTrees[i]);
            if (prefab == null) continue;
            models.Add(prefab);
            heights.Add(UxgdHeights[i]);
        }
        bool useObjects = models.Count > 0;
        int kinds = useObjects ? models.Count : (td.treePrototypes?.Length ?? 0);
        if (kinds == 0)
        {
            Debug.LogWarning("[Himalaya] No tree models or prototypes; skipping trees.", this);
            return;
        }

        var rng = new System.Random(config.seed);
        int nx = Mathf.Max(1, Mathf.FloorToInt(td.size.x / config.treeSpacing));
        int nz = Mathf.Max(1, Mathf.FloorToInt(td.size.z / config.treeSpacing));
        var trees = new List<TreeInstance>();

        for (int gz = 0; gz < nz; gz++)
        {
            for (int gx = 0; gx < nx; gx++)
            {
                float u = (gx + (float)rng.NextDouble()) / nx;
                float v = (gz + (float)rng.NextDouble()) / nz;
                double roll = rng.NextDouble();
                double fadeRoll = rng.NextDouble();
                int prototype = rng.Next(kinds);
                float size = (0.8f + (float)rng.NextDouble() * 0.4f) * treeScale;
                float spin = (float)rng.NextDouble() * Mathf.PI * 2f;

                if (roll > config.treeDensity) continue;
                if (landCover != null && landCover.ClassAt(u, v) != HimalayaLandCover.Tree) continue;

                float elev = builder.ElevationMeters(u, v);
                if (elev > config.treeLineMeters) continue;
                // Thin out toward the tree line.
                float nearLine = Mathf.InverseLerp(config.treeLineMeters - config.treeFadeMeters, config.treeLineMeters, elev);
                if (fadeRoll < nearLine) continue;
                if (td.GetSteepness(u, v) > config.maxTreeSlope) continue;

                trees.Add(new TreeInstance
                {
                    prototypeIndex = prototype,
                    position = new Vector3(u, 0f, v),     // y is snapped to the heightmap below
                    widthScale = size,
                    heightScale = size,
                    rotation = spin,
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }
        }

        // Too many for a phone? Drop random ones.
        int cap = maxTreesOverride > 0 ? maxTreesOverride : config.maxTrees;
        // Tablets get half: each tree is a real object and the build is slower there.
        if (useObjects) cap = Mathf.Min(cap, Application.isMobilePlatform ? maxObjectTrees / 2 : maxObjectTrees);
        while (trees.Count > cap)
        {
            int i = rng.Next(trees.Count);
            trees[i] = trees[trees.Count - 1];
            trees.RemoveAt(trees.Count - 1);
        }

        if (!useObjects)
        {
            td.SetTreeInstances(trees.ToArray(), true);
            Debug.Log($"[Himalaya] Placed {trees.Count} terrain trees.");
            return;
        }

        td.SetTreeInstances(new TreeInstance[0], true);   // no old terrain trees under the new ones
        SpawnObjects(builder.Terrain, trees, models, heights);
    }

    void SpawnObjects(Terrain terrain, List<TreeInstance> trees, List<GameObject> models, List<float> heights)
    {
        if (treeRoot != null) Destroy(treeRoot);
        treeRoot = new GameObject("[Trees]");
        treeRoot.transform.SetParent(terrain.transform, false);

        // Scale per model so its real height matches the wanted height.
        var unitScale = new float[models.Count];
        for (int m = 0; m < models.Count; m++)
        {
            float h = ModelHeight(models[m]);
            unitScale[m] = h > 0.0001f ? heights[m] / h : 1f;
        }

        var size = terrain.terrainData.size;
        var origin = terrain.transform.position;
        int skipped = 0;
        foreach (var t in trees)
        {
            var model = models[t.prototypeIndex];
            var pos = origin + new Vector3(t.position.x * size.x, 0f, t.position.z * size.z);
            if (HimalayaRiver.Exists && HimalayaRiver.DistanceToAnyWater(pos) < riverGap) { skipped++; continue; }
            if (NearGrass(pos)) { skipped++; continue; }
            pos.y = terrain.SampleHeight(pos) + origin.y;
            // Keep the model's own rotation (FBX exports are often turned -90 on X) and spin it around up.
            var rot = Quaternion.Euler(0f, t.rotation * Mathf.Rad2Deg, 0f) * model.transform.rotation;
            var go = Instantiate(model, pos, rot, treeRoot.transform);
            go.transform.localScale = model.transform.localScale * (unitScale[t.prototypeIndex] * t.heightScale);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            // Layer 2 "Ignore Raycast": the NavMesh skips it (else it reads every tree mesh: slow + errors)
            // and taps go through trees to the animals.
            foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 2;
            // Trunk: a small no-walk spot in the NavMesh (default layer so the bake sees it), so animals
            // go around trees instead of through them. Only the trunk, so herds still pass between trees.
            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(treeRoot.transform, false);
            trunk.transform.position = pos;
            var vol = trunk.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
            vol.size = new Vector3(trunkSize, 6f, trunkSize);
            vol.center = new Vector3(0f, 2f, 0f);
            vol.area = 1;   // built-in "Not Walkable"
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // tablet: shadows are the costly part
        }
        Debug.Log($"[Himalaya] Placed {trees.Count - skipped} UXGD tree objects ({skipped} skipped near water or grass).");
    }

    // Height of the model as it is in the prefab (all parts together).
    static float ModelHeight(GameObject prefab)
    {
        var probe = Instantiate(prefab);
        probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var rends = probe.GetComponentsInChildren<Renderer>();
        float h = 0f;
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            h = b.size.y;
        }
        DestroyImmediate(probe);
        return h;
    }

    bool NearGrass(Vector3 p)
    {
        float gap2 = grassGap * grassGap;
        foreach (var g in HimalayaResourcePlacer.GrassSpots)
        {
            float dx = g.x - p.x, dz = g.z - p.z;
            if (dx * dx + dz * dz < gap2) return true;
        }
        return false;
    }
}
