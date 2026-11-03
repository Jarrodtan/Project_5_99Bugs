// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns OpenStreetMap water lines into meshes on the terrain: a ribbon that follows each stream
/// down the mountain (UV.x = distance along / width, so the WaterFlow shader's ripples run downstream;
/// OSM draws waterways in the direction they flow), and a flat surface for each lake outline.
/// Meshes are in the terrain's local space, for a child of the terrain at the local origin.
/// The island fade scales that parent; world-space vertices were left behind in the wrong place.
/// </summary>
public static class WaterMeshBuilder
{
    const float Step = 2.5f;     // metres between cross-sections, so the ribbon hugs the ground
    const float Lift = 0.3f;     // above the terrain, so bumps between samples don't poke through

    /// <param name="endWidth">Width at the last point (the river widens downstream); below 0 = same as width.</param>
    public static Mesh Stream(HimalayaTerrainBuilder tb, IList<Vector2> uvs, float width, float endWidth = -1f)
    {
        float startWidth = width;
        if (endWidth < 0f) endWidth = width;
        var centre = Resample(tb, uvs);
        if (centre.Count < 2) return null;

        var verts = new List<Vector3>(centre.Count * 2);
        var uv = new List<Vector2>(centre.Count * 2);
        var tris = new List<int>((centre.Count - 1) * 6);
        float along = 0f;

        for (int i = 0; i < centre.Count; i++)
        {
            width = Mathf.Lerp(startWidth, endWidth, i / (float)Mathf.Max(1, centre.Count - 1));
            Vector3 dir = (i < centre.Count - 1 ? centre[i + 1] - centre[i] : centre[i] - centre[i - 1]);
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized) * (width * 0.5f);
            if (i > 0) along += Vector3.Distance(centre[i], centre[i - 1]);

            Vector3 l = centre[i] - side, r = centre[i] + side;
            l.y = Mathf.Max(l.y, Ground(tb, l)) + Lift;   // banks follow the ground too
            r.y = Mathf.Max(r.y, Ground(tb, r)) + Lift;
            verts.Add(l); verts.Add(r);
            uv.Add(new Vector2(along / width, 0f)); uv.Add(new Vector2(along / width, 1f));

            if (i > 0)
            {
                int v = verts.Count - 4;
                tris.Add(v); tris.Add(v + 2); tris.Add(v + 1);
                tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3);
            }
        }
        return Make("Stream", verts, uv, tris);
    }

    public static Mesh Lake(HimalayaTerrainBuilder tb, IList<Vector2> uvs)
    {
        var ring = new List<Vector3>();
        foreach (var p in uvs) if (Inside(p)) ring.Add(tb.LocalPosition(p.x, p.y));
        if (ring.Count < 3) return null;

        // Flat water at the lowest point of the shore, like a real lake level.
        float level = float.PositiveInfinity;
        Vector3 centre = Vector3.zero;
        foreach (var p in ring) { level = Mathf.Min(level, p.y); centre += p; }
        centre /= ring.Count;
        level += Lift + 0.2f;

        var verts = new List<Vector3> { new Vector3(centre.x, level, centre.z) };
        var uv = new List<Vector2> { new Vector2(centre.x * 0.1f, 0.5f) };
        var tris = new List<int>();
        foreach (var p in ring)
        {
            verts.Add(new Vector3(p.x, level, p.z));
            uv.Add(new Vector2(p.x * 0.1f, 0f));   // across = 0 at the shore: lighter edge
        }
        for (int i = 1; i < verts.Count; i++)
        {
            int next = i + 1 < verts.Count ? i + 1 : 1;
            tris.Add(0); tris.Add(i); tris.Add(next);
        }
        return Make("Lake", verts, uv, tris);
    }

    // Points along the line every Step metres, on the terrain surface.
    static List<Vector3> Resample(HimalayaTerrainBuilder tb, IList<Vector2> uvs)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i < uvs.Count - 1; i++)
        {
            Vector2 a = uvs[i], b = uvs[i + 1];
            if (!Inside(a) || !Inside(b)) continue;
            float len = Vector3.Distance(tb.LocalPosition(a.x, a.y), tb.LocalPosition(b.x, b.y));
            int n = Mathf.Max(1, Mathf.CeilToInt(len / Step));
            for (int k = (pts.Count == 0 ? 0 : 1); k <= n; k++)
            {
                Vector2 p = Vector2.Lerp(a, b, k / (float)n);
                pts.Add(tb.LocalPosition(p.x, p.y));
            }
        }
        return pts;
    }

    static float Ground(HimalayaTerrainBuilder tb, Vector3 local)
    {
        var t = tb.Terrain;
        if (t == null || t.terrainData == null) return local.y;
        var size = t.terrainData.size;
        float u = size.x > 0.01f ? local.x / size.x : 0f;
        float v = size.z > 0.01f ? local.z / size.z : 0f;
        return t.terrainData.GetInterpolatedHeight(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    static bool Inside(Vector2 uv) => uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;

    static Mesh Make(string name, List<Vector3> verts, List<Vector2> uv, List<int> tris)
    {
        var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(verts);
        m.SetUVs(0, uv);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
