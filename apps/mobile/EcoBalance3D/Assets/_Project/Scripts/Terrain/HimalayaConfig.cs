using UnityEngine;

/// <summary>
/// Single hub for the Himalaya world generator. Area, scale, data files and placement rules live here,
/// never in the scripts that read it.  Create > EcoSim > Himalaya Config.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Himalaya Config", fileName = "HimalayaConfig")]
public class HimalayaConfig : ScriptableObject
{
    [Header("Area (degrees). Use the SAME box for every data layer.")]
    public double west = 86.80;
    public double south = 27.85;
    public double east = 87.05;
    public double north = 28.10;

    [Header("Elevation")]
    [Tooltip("Terrarium tile zoom. 10 = ~120 m/pixel, 11 = ~60 m/pixel, 12 = ~30 m/pixel.")]
    [Range(8, 12)] public int tileZoom = 11;

    [Tooltip("Heightmap size, must be 2^n + 1 (257 or 513 for mobile).")]
    public int heightmapResolution = 513;

    [Tooltip("Terrain width in Unity units. Depth follows the real aspect ratio of the area.")]
    public float worldWidth = 500f;

    [Tooltip("1 = true proportions. Above 1 makes mountains steeper than real.")]
    [Min(0.1f)] public float verticalExaggeration = 1f;

    [Tooltip("Size of the box (km) when another script supplies only a centre point, e.g. from Wikidata.")]
    [Min(1f)] public float areaSizeKm = 25f;

    [Header("Land cover (optional)")]
    [Tooltip("ESA WorldCover clip as a PNG: Read/Write ON, sRGB OFF, Filter Point, Compression None. Class value is read from the red channel.")]
    public Texture2D landCover;

    [Header("Trees")]
    public float treeLineMeters = 3800f;
    [Tooltip("Tree chance fades out over this many metres below the tree line.")]
    public float treeFadeMeters = 400f;
    public float maxTreeSlope = 35f;
    [Tooltip("Grid cell size in world units; one tree is tried per cell.")]
    public float treeSpacing = 4f;
    public int maxTrees = 5000;
    [Tooltip("When a script supplies a tree count (TerrainConfig via AutoBakeFloor), terrain trees = count x this.")]
    public int treesPerCount = 40;
    [Range(0f, 1f)] public float treeDensity = 0.8f;

    [Header("Terrain painting")]
    public float snowLineMeters = 5200f;
    public float rockSlope = 38f;

    [Header("Resource patches (ResourcePatch)")]
    public int[] grassClasses = { 30, 20 };       // ESA: 30 grassland, 20 shrubland
    public int grassPatches = 40;
    public int maxWaterPatches = 60;
    [Tooltip("World units between water patches along a stream.")]
    public float waterPatchSpacing = 12f;

    [Header("Network")]
    public string overpassUrl = "https://overpass-api.de/api/interpreter";
    [Tooltip("Save downloads to disk and reuse them next run.")]
    public bool useCache = true;

    [Header("Randomness")]
    public int seed = 1;

    GeoBounds overrideBounds;
    bool hasOverride;

    /// <summary>True while a script has supplied its own centre point (e.g. from Wikidata).</summary>
    public bool HasOverride => hasOverride;

    /// <summary>The box every generator uses: the one above, or the one around a supplied centre.</summary>
    public GeoBounds Bounds => hasOverride ? overrideBounds : new GeoBounds(west, south, east, north);

    /// <summary>Builds the box around a point. Lasts until <see cref="ClearOverride"/>; the asset itself is not changed.</summary>
    public void SetCenter(double lat, double lon)
    {
        double halfLat = areaSizeKm / 110.574 * 0.5;
        double halfLon = areaSizeKm / (111.32 * System.Math.Cos(lat * System.Math.PI / 180.0)) * 0.5;
        overrideBounds = new GeoBounds(lon - halfLon, lat - halfLat, lon + halfLon, lat + halfLat);
        hasOverride = true;
    }

    public void ClearOverride() => hasOverride = false;

    void OnValidate()
    {
        heightmapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1, 33, 2049);
        if (east <= west) east = west + 0.01;
        if (north <= south) north = south + 0.01;
    }
}
