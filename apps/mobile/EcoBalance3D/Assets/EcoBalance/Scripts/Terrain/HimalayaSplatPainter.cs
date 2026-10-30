using UnityEngine;

/// <summary>
/// Paints the terrain from the data: snow above the snow line (or on WorldCover snow), rock on steep slopes
/// (or bare ground), grass elsewhere. Terrain layer order must be: 0 grass, 1 rock, 2 snow.
/// </summary>
public class HimalayaSplatPainter : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    public void Paint(HimalayaTerrainBuilder builder, HimalayaLandCover landCover)
    {
        var td = builder.Terrain.terrainData;
        if (td.terrainLayers == null || td.terrainLayers.Length < 3)
        {
            Debug.LogWarning("[Himalaya] Terrain needs 3 TerrainLayers (grass, rock, snow) to be painted.", this);
            return;
        }

        int res = td.alphamapResolution;
        var alphas = new float[res, res, td.alphamapLayers];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = x / (res - 1f), v = y / (res - 1f);
                float elev = builder.ElevationMeters(u, v);
                float slope = td.GetSteepness(u, v);

                float snow = Smooth(config.snowLineMeters - 250f, config.snowLineMeters + 250f, elev);
                float rock = Smooth(config.rockSlope - 8f, config.rockSlope + 8f, slope);
                if (landCover != null)
                {
                    int c = landCover.ClassAt(u, v);
                    if (c == HimalayaLandCover.Snow) snow = 1f;
                    else if (c == HimalayaLandCover.Bare) rock = Mathf.Max(rock, 0.8f);
                }

                alphas[y, x, 2] = snow;
                alphas[y, x, 1] = (1f - snow) * rock;
                alphas[y, x, 0] = (1f - snow) * (1f - rock);
            }
        }
        td.SetAlphamaps(0, 0, alphas);
    }

    static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));
}
