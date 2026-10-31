using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

/// <summary>
/// Downloads AWS Terrarium elevation tiles covering the config's box, stitches them, and writes the
/// result into a Unity Terrain. Also answers "what is the elevation / slope / world position at (u,v) or (lat,lon)?"
/// for the other generator scripts.
/// </summary>
public class HimalayaTerrainBuilder : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    [Tooltip("Optional build profile. When flattenHeights is set, the DEM cut is written flat.")]
    [SerializeField] HimalayaBuildProfile profile;

    [Tooltip("Terrain to fill. Leave empty to create one at runtime.")]
    [SerializeField] Terrain terrain;

    [Tooltip("Layers applied when creating a Terrain at runtime (grass, rock, snow).")]
    [SerializeField] TerrainLayer[] bootstrapLayers;

    [Tooltip("URP TerrainLit (or project default terrain material). Required so runtime Terrain is not magenta/pink.")]
    [SerializeField] Material terrainMaterialTemplate;

    public Terrain Terrain => terrain;

    /// <summary>Build variant on this island, used to tell a flat cut from the mountain cut.</summary>
    public HimalayaBuildProfile BuildProfile => profile;

    /// <summary>Grass, rock, and snow layers applied when the terrain is created.</summary>
    public TerrainLayer[] BootstrapLayers => bootstrapLayers;
    public GeoBounds Bounds { get; private set; }
    public bool IsBuilt { get; private set; }
    public float MinElevation { get; private set; }
    public float MaxElevation { get; private set; }
    public float MetersToUnits { get; private set; }

    /// <summary>NavMeshSurface on the runtime or assigned terrain, if any.</summary>
    public NavMeshSurface NavSurface { get; private set; }

    TerrainData _runtimeTerrainData;

    void OnDisable()
    {
        // Only when leaving Play (isPlaying already false) — mid-Play disable must keep TerrainData.
        if (!Application.isPlaying)
            ReleaseRuntimeTerrainData();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        if (NavSurface != null)
            NavSurface.RemoveData();
        ReleaseRuntimeTerrainData();
    }

    /// <summary>Unbinds and destroys the runtime TerrainData copy so Editor domain reload does not leak GC handles.</summary>
    public void ReleaseRuntimeTerrainData()
    {
        if (_runtimeTerrainData == null)
            return;

        var data = _runtimeTerrainData;
        _runtimeTerrainData = null;

        if (terrain != null && terrain.terrainData == data)
            terrain.terrainData = null;

        var col = terrain != null ? terrain.GetComponent<TerrainCollider>() : null;
        if (col != null && col.terrainData == data)
            col.terrainData = null;

        // Always Immediate: deferred Destroy survives into domain reload and spams GC-handle warnings.
        DestroyImmediate(data, allowDestroyingAssets: false);
    }

    public IEnumerator Build()
    {
        IsBuilt = false;
        if (config == null)
        {
            Debug.LogError($"{nameof(HimalayaTerrainBuilder)} needs a config.", this);
            yield break;
        }

        EnsureTerrain();
        if (terrain == null)
        {
            Debug.LogError($"{nameof(HimalayaTerrainBuilder)} could not create a Terrain.", this);
            yield break;
        }

        var b = config.Bounds;
        Bounds = b;
        int z = config.tileZoom;

        int x0 = (int)Math.Floor(HimalayaGeo.WorldPixelX(b.West, z) / 256.0);
        int x1 = (int)Math.Floor(HimalayaGeo.WorldPixelX(b.East, z) / 256.0);
        int y0 = (int)Math.Floor(HimalayaGeo.WorldPixelY(b.North, z) / 256.0);
        int y1 = (int)Math.Floor(HimalayaGeo.WorldPixelY(b.South, z) / 256.0);
        int tilesX = x1 - x0 + 1, tilesY = y1 - y0 + 1;
        if (tilesX * tilesY > 16)
        {
            Debug.LogError($"Area needs {tilesX * tilesY} tiles. Lower the zoom or shrink the box.", this);
            yield break;
        }

        // Stitched elevation in metres. Row 0 = north edge (same as the tile images).
        var elev = new float[tilesY * 256, tilesX * 256];
        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                byte[] png = null;
                yield return FetchTile(z, x0 + tx, y0 + ty, bytes => png = bytes);
                if (png == null) yield break;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(png);
                var px = tex.GetPixels32();                   // row 0 is the BOTTOM of the image
                for (int j = 0; j < 256; j++)
                {
                    int imageRow = 255 - j;
                    for (int i = 0; i < 256; i++)
                    {
                        var c = px[j * 256 + i];
                        elev[ty * 256 + imageRow, tx * 256 + i] = (c.r * 256f + c.g + c.b / 256f) - 32768f;
                    }
                }
                Destroy(tex);
            }
        }

        // Sample the stitched grid at each heightmap cell. Latitude is linear across the box;
        // the Mercator pixel lookup handles the tile projection.
        int res = config.heightmapResolution;
        var metres = new float[res, res];
        float min = float.MaxValue, max = float.MinValue;
        for (int j = 0; j < res; j++)
        {
            double lat = b.South + (b.North - b.South) * j / (res - 1.0);
            double py = HimalayaGeo.WorldPixelY(lat, z) - y0 * 256.0;
            for (int i = 0; i < res; i++)
            {
                double lon = b.West + (b.East - b.West) * i / (res - 1.0);
                double px = HimalayaGeo.WorldPixelX(lon, z) - x0 * 256.0;
                float e = Bilinear(elev, px, py);
                metres[j, i] = e;
                if (e < min) min = e;
                if (e > max) max = e;
            }
        }

        bool flatten = profile != null && profile.flattenHeights;
        var heights = new float[res, res];
        float range = Mathf.Max(1f, max - min);
        if (flatten)
        {
            float mid = (min + max) * 0.5f;
            MinElevation = mid;
            MaxElevation = mid;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                    heights[j, i] = 0.5f;
            range = 1f;
        }
        else
        {
            MinElevation = min;
            MaxElevation = max;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                    heights[j, i] = (metres[j, i] - min) / range;
        }

        MetersToUnits = (float)(config.worldWidth / b.WidthMeters);

        // Work on a copy so the TerrainData asset in the project isn't overwritten.
        var source = terrain.terrainData;
        var previous = _runtimeTerrainData;
        var td = Instantiate(source);
        _runtimeTerrainData = td;
        terrain.terrainData = td;
        td.heightmapResolution = res;
        float depth = (float)(config.worldWidth * b.HeightMeters / b.WidthMeters);
        float height = flatten
            ? 1f
            : Mathf.Max(1f, range * MetersToUnits * config.verticalExaggeration);
        td.size = new Vector3(config.worldWidth, height, depth);
        td.SetHeights(0, 0, heights);

        var col = terrain.GetComponent<TerrainCollider>();
        if (col != null) col.terrainData = td;

        // Drop the previous runtime copy only after Terrain no longer references it.
        if (previous != null && previous != td)
            DestroyImmediate(previous, allowDestroyingAssets: false);

        IsBuilt = true;
        Debug.Log($"[Himalaya] Terrain built: {MinElevation:0}-{MaxElevation:0} m, {config.worldWidth:0} x {depth:0} units" +
                  (flatten ? " (flat cut)." : "."));
    }

    void EnsureTerrain()
    {
        if (terrain != null)
        {
            NavSurface = terrain.GetComponent<NavMeshSurface>();
            if (NavSurface == null)
                NavSurface = terrain.gameObject.AddComponent<NavMeshSurface>();
            ConfigureNavSurface(NavSurface);
            return;
        }

        var go = new GameObject("Terrain");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var td = new TerrainData();
        _runtimeTerrainData = td;
        td.heightmapResolution = Mathf.Max(33, config.heightmapResolution);
        td.alphamapResolution = Mathf.Clamp(config.heightmapResolution - 1, 16, 512);
        td.size = new Vector3(config.worldWidth, 1f, config.worldWidth);
        if (bootstrapLayers != null && bootstrapLayers.Length > 0)
            td.terrainLayers = bootstrapLayers;

        terrain = go.AddComponent<Terrain>();
        terrain.terrainData = td;
        if (terrainMaterialTemplate != null)
            terrain.materialTemplate = terrainMaterialTemplate;
        else
            Debug.LogWarning($"{nameof(HimalayaTerrainBuilder)}: assign terrainMaterialTemplate (URP TerrainLit) or the terrain will render pink.", this);

        var col = go.AddComponent<TerrainCollider>();
        col.terrainData = td;
        NavSurface = go.AddComponent<NavMeshSurface>();
        ConfigureNavSurface(NavSurface);
    }

    static void ConfigureNavSurface(NavMeshSurface surface)
    {
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.defaultArea = 0;
        // Narrow valley strips on a steep DEM are smaller than the default 2 m² cutoff.
        surface.minRegionArea = 0.1f;
    }

    IEnumerator FetchTile(int z, int x, int y, Action<byte[]> done)
    {
        string file = $"terrarium_{z}_{x}_{y}.png";
        if (config.useCache && GeoCache.TryReadBytes(file, out var cached))
        {
            done(cached);
            yield break;
        }

        string url = $"https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png";
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Himalaya] Tile download failed ({url}): {req.error}", this);
                done(null);
                yield break;
            }

            var bytes = req.downloadHandler.data;
            if (config.useCache) GeoCache.WriteBytes(file, bytes);
            done(bytes);
        }
    }

    static float Bilinear(float[,] a, double x, double y)
    {
        int w = a.GetLength(1), h = a.GetLength(0);
        x = Math.Max(0.0, Math.Min(w - 1.001, x));
        y = Math.Max(0.0, Math.Min(h - 1.001, y));
        int x0 = (int)x, y0 = (int)y;
        float tx = (float)(x - x0), ty = (float)(y - y0);
        return Mathf.Lerp(
            Mathf.Lerp(a[y0, x0], a[y0, x0 + 1], tx),
            Mathf.Lerp(a[y0 + 1, x0], a[y0 + 1, x0 + 1], tx), ty);
    }

    // ---- Queries used by the other generators ----

    /// <summary>Real elevation in metres at normalised terrain coordinates (u west-east, v south-north).</summary>
    public float ElevationMeters(float u, float v)
    {
        var td = terrain.terrainData;
        float h01 = td.GetInterpolatedHeight(u, v) / td.size.y;
        return MinElevation + h01 * (MaxElevation - MinElevation);
    }

    public float SlopeDegrees(float u, float v) => terrain.terrainData.GetSteepness(u, v);

    public Vector3 WorldPosition(float u, float v)
    {
        var td = terrain.terrainData;
        var origin = terrain.transform.position;
        return new Vector3(origin.x + u * td.size.x, origin.y + td.GetInterpolatedHeight(u, v), origin.z + v * td.size.z);
    }

    public Vector3 WorldPosition(double lat, double lon)
    {
        var uv = Bounds.ToUV(lat, lon);
        return WorldPosition(uv.x, uv.y);
    }
}
