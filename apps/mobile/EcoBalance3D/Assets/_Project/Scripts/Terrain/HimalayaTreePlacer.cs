using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places Terrain trees on a jittered grid: only where WorldCover says "tree cover" (or anywhere if no
/// land-cover image is set), below the tree line, on gentle slopes. Add tree prototypes to the Terrain first.
/// </summary>
public class HimalayaTreePlacer : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    public void Place(HimalayaTerrainBuilder builder, HimalayaLandCover landCover, int maxTreesOverride = -1)
    {
        var td = builder.Terrain.terrainData;
        if (td.treePrototypes == null || td.treePrototypes.Length == 0)
        {
            Debug.LogWarning("[Himalaya] Terrain has no tree prototypes; skipping trees.", this);
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
                int prototype = rng.Next(td.treePrototypes.Length);
                float size = 0.8f + (float)rng.NextDouble() * 0.4f;
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
        while (trees.Count > cap)
        {
            int i = rng.Next(trees.Count);
            trees[i] = trees[trees.Count - 1];
            trees.RemoveAt(trees.Count - 1);
        }

        td.SetTreeInstances(trees.ToArray(), true);
        Debug.Log($"[Himalaya] Placed {trees.Count} trees.");
    }
}
