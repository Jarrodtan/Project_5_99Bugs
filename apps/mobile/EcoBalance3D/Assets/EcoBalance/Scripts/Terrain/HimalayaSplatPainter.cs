using UnityEngine;

/// <summary>
/// Paints the terrain from the data: snow above the snow line (or on WorldCover snow), rock on steep slopes
/// (or bare ground), grass elsewhere. Terrain layer order must be: 0 grass, 1 rock, 2 snow.
/// </summary>
public class HimalayaSplatPainter : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): kept from the last full paint so TerrainEditTool can repaint parts with the same rules.
    HimalayaLandCover lastLandCover;

    public void Paint(HimalayaTerrainBuilder builder, HimalayaLandCover landCover)
    {
        lastLandCover = landCover;
        PaintRegion(builder, new Rect(0f, 0f, 1f, 1f), true);
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>
    /// Repaints only part of the terrain (u/v rect, 0-1) from its current heights, e.g. after it was
    /// sculpted in game: ground lowered below the snow line turns to grass, ground made steep turns to rock.
    /// Edited ground uses height and slope only (useLandCover false), since the land-cover image is of the original ground.
    /// </summary>
    public void PaintRegion(HimalayaTerrainBuilder builder, Rect uv, bool useLandCover = false)
    {
        var td = builder.Terrain.terrainData;
        if (td.terrainLayers == null || td.terrainLayers.Length < 3)
        {
            Debug.LogWarning("[Himalaya] Terrain needs 3 TerrainLayers (grass, rock, snow) to be painted.", this);
            return;
        }

        int res = td.alphamapResolution;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(uv.xMin * (res - 1)), 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(uv.xMax * (res - 1)), 0, res - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(uv.yMin * (res - 1)), 0, res - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(uv.yMax * (res - 1)), 0, res - 1);
        int w = x1 - x0 + 1, h = y1 - y0 + 1;
        if (w <= 0 || h <= 0) return;

        var alphas = td.GetAlphamaps(x0, y0, w, h);   // keeps any extra layers as they are
        int layers = alphas.GetLength(2);
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                float u = (x0 + i) / (res - 1f), v = (y0 + j) / (res - 1f);
                Weights(builder, td, u, v, useLandCover, out float grass, out float rock, out float snow);
                for (int l = 3; l < layers; l++) alphas[j, i, l] = 0f;
                alphas[j, i, 2] = snow;
                alphas[j, i, 1] = rock;
                alphas[j, i, 0] = grass;
            }
        }
        td.SetAlphamaps(x0, y0, alphas);
    }

    void Weights(HimalayaTerrainBuilder builder, TerrainData td, float u, float v, bool useLandCover, out float grass, out float rock, out float snow)
    {
        float elev = builder.ElevationMeters(u, v);
        float slope = td.GetSteepness(u, v);

        snow = Smooth(config.snowLineMeters - 250f, config.snowLineMeters + 250f, elev);
        float steep = Smooth(config.rockSlope - 8f, config.rockSlope + 8f, slope);
        if (useLandCover && lastLandCover != null)
        {
            int c = lastLandCover.ClassAt(u, v);
            if (c == HimalayaLandCover.Snow) snow = 1f;
            else if (c == HimalayaLandCover.Bare) steep = Mathf.Max(steep, 0.8f);
        }

        rock = (1f - snow) * steep;
        grass = (1f - snow) * (1f - steep);
    }
    // [AI-GENERATED END]

    static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));
}
