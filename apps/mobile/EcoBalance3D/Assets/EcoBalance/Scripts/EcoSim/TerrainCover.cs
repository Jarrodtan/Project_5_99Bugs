// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
// What the ground is made of at a point: how much grass, rock and snow is painted there.
// Used for camouflage (a pale snow leopard is hard to see on snow, easy on green grass) and so
// leopards choose snowy ground to roam.
//
// Layer order matches HimalayaSplatPainter: 0 grass, 1 rock, 2 snow. On a terrain without those
// three layers (or off the terrain) it reports "no idea" and callers fall back to neutral behaviour.

using UnityEngine;

public static class TerrainCover
{
    public struct Cover
    {
        public float grass, rock, snow;
    }

    /// <summary>Grass / rock / snow weights (summing to about 1) under p. False if unknown.</summary>
    public static bool TryGet(Vector3 p, out Cover cover)
    {
        cover = default;
        Terrain t = TerrainAt(p);
        if (t == null) return false;

        TerrainData td = t.terrainData;
        if (td == null || td.alphamapLayers < 3) return false;

        Vector3 local = p - t.GetPosition();
        float u = local.x / td.size.x, v = local.z / td.size.z;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        int res = td.alphamapResolution;
        int x = Mathf.Clamp(Mathf.RoundToInt(u * (res - 1)), 0, res - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (res - 1)), 0, res - 1);

        // One texel: a small read, and it always shows the latest paint (e.g. after terrain editing).
        float[,,] a = td.GetAlphamaps(x, y, 1, 1);
        cover.grass = a[0, 0, 0];
        cover.rock = a[0, 0, 1];
        cover.snow = a[0, 0, 2];
        return true;
    }

    /// <summary>0..1 snow under p (0 when unknown).</summary>
    public static float SnowAt(Vector3 p) => TryGet(p, out var c) ? c.snow : 0f;

    static Terrain TerrainAt(Vector3 p)
    {
        Terrain active = Terrain.activeTerrain;
        if (Contains(active, p)) return active;
        foreach (Terrain t in Terrain.activeTerrains)
            if (Contains(t, p)) return t;
        return null;
    }

    static bool Contains(Terrain t, Vector3 p)
    {
        if (t == null || t.terrainData == null) return false;
        Vector3 o = t.GetPosition(), s = t.terrainData.size;
        return p.x >= o.x && p.x <= o.x + s.x && p.z >= o.z && p.z <= o.z + s.z;
    }
}
// [AI-GENERATED END]
