using UnityEngine;

/// <summary>
/// Paints the terrain from the data: snow above the snow line (or on WorldCover snow), rock on steep slopes
/// (or bare ground), grass elsewhere. Terrain layer order must be: 0 grass, 1 rock, 2 snow.
/// </summary>
public class HimalayaSplatPainter : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    [Tooltip("Optional (Yande, PR #51). When forceGrassSplat is set, paints grass-only cover.")]
    [SerializeField] HimalayaBuildProfile profile;

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
                float grass, rock, snow;
                if (profile != null && profile.forceGrassSplat) { grass = 1f; rock = 0f; snow = 0f; }
                else Weights(builder, td, u, v, useLandCover, out grass, out rock, out snow);
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

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): noise shifts the snow line and rock angle from place to place,
        // so snow dips and lifts along the ridges and rock edges break up, instead of following one exact height / angle.
        // Fixed per (u,v) and seed, so a partial repaint after terrain editing matches the original paint.
        float wx = u * td.size.x / config.paintNoiseScale, wz = v * td.size.z / config.paintNoiseScale;
        float snowLine = config.snowLineMeters + Noise(wx, wz, 0) * config.snowLineNoiseMeters;
        float rockAngle = config.rockSlope + Noise(wx, wz, 1) * config.rockSlopeNoise;

        snow = Smooth(snowLine - 250f, snowLine + 250f, elev);
        float steep = Smooth(rockAngle - 8f, rockAngle + 8f, slope);
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

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): two octaves of Perlin, -1..1. channel gives independent patterns for snow and rock.
    float Noise(float x, float z, int channel)
    {
        float o = (config.seed * 7.31f + channel * 53.7f) % 1000f;
        float n = Mathf.PerlinNoise(x + o, z + o) * 0.65f
                + Mathf.PerlinNoise(x * 2.7f + o + 31.7f, z * 2.7f + o + 11.3f) * 0.35f;
        return Mathf.Clamp(n * 2f - 1f, -1f, 1f);
    }

    static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));
}
