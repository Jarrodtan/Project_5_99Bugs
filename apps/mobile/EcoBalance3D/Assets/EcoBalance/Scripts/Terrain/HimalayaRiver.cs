// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// One river across the map, the way meltwater really runs in the Himalaya: it starts high on a
/// mountain at one edge and follows the valley floors down to the lowest ground at the far edge.
/// The path is the cheapest route over the heightmap where low ground is cheap and climbing is
/// expensive, so it naturally sits in the valleys.
///
/// The river is cut out of the NavMesh (animals can't wade or swim across), so it divides the map
/// into two sides; <see cref="Side"/> tells which side a point is on, so each side can have its own
/// herd and territory.
/// </summary>
public static class HimalayaRiver
{
    /// <summary>The river centre line in world space, source first. Empty until <see cref="Build"/>.</summary>
    public static readonly List<Vector3> Path = new List<Vector3>();
    public static bool Exists => Path.Count >= 2;

    const int Grid = 96;   // heightmap cells per side used for the route search

    /// <summary>Finds the route, returns it as terrain UVs (source first).</summary>
    public static List<Vector2> Trace(HimalayaTerrainBuilder tb)
    {
        // Elevation grid, 0..1.
        var h = new float[Grid, Grid];
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int x = 0; x < Grid; x++)
            for (int y = 0; y < Grid; y++)
            {
                float e = tb.ElevationMeters(x / (Grid - 1f), y / (Grid - 1f));
                h[x, y] = e; min = Mathf.Min(min, e); max = Mathf.Max(max, e);
            }
        float range = Mathf.Max(1f, max - min);
        for (int x = 0; x < Grid; x++) for (int y = 0; y < Grid; y++) h[x, y] = (h[x, y] - min) / range;

        // Source: the highest point on the top edge. Mouth: the lowest point on the bottom edge.
        // Both on the very edge, so the river cuts the map fully in two (no way round the ends).
        int band = Grid / 10;
        Vector2Int source = default, mouth = default;
        float hi = -1f, lo = 2f;
        for (int x = band; x < Grid - band; x++)
        {
            if (h[x, Grid - 1] > hi) { hi = h[x, Grid - 1]; source = new Vector2Int(x, Grid - 1); }
            if (h[x, 0] < lo) { lo = h[x, 0]; mouth = new Vector2Int(x, 0); }
        }

        // Dijkstra on the grid: low ground is cheap, climbing is very expensive.
        var cost = new float[Grid, Grid];
        var from = new Vector2Int[Grid, Grid];
        for (int x = 0; x < Grid; x++) for (int y = 0; y < Grid; y++) cost[x, y] = float.PositiveInfinity;
        cost[source.x, source.y] = 0f;
        var open = new SortedSet<(float c, int x, int y)>();
        open.Add((0f, source.x, source.y));

        while (open.Count > 0)
        {
            var cur = open.Min; open.Remove(cur);
            if (cur.x == mouth.x && cur.y == mouth.y) break;
            if (cur.c > cost[cur.x, cur.y]) continue;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cur.x + dx, ny = cur.y + dy;
                    if (nx < 0 || ny < 0 || nx >= Grid || ny >= Grid) continue;
                    float step = (dx != 0 && dy != 0) ? 1.414f : 1f;
                    float rise = Mathf.Max(0f, h[nx, ny] - h[cur.x, cur.y]);
                    float c = cur.c + step * (0.05f + h[nx, ny] * h[nx, ny] * 4f) + rise * 60f;
                    if (c < cost[nx, ny])
                    {
                        cost[nx, ny] = c;
                        from[nx, ny] = new Vector2Int(cur.x, cur.y);
                        open.Add((c, nx, ny));
                    }
                }
        }

        var cells = new List<Vector2Int>();
        for (var p = mouth; ; p = from[p.x, p.y])
        {
            cells.Add(p);
            if (p == source || cells.Count > Grid * Grid) break;
        }
        cells.Reverse();   // source first, so the water flows downhill along the ribbon

        // Smooth the zig-zag of the grid into a natural bend.
        var uv = new List<Vector2>();
        foreach (var c in cells) uv.Add(new Vector2(c.x / (Grid - 1f), c.y / (Grid - 1f)));
        for (int pass = 0; pass < 3; pass++)
            for (int i = 1; i < uv.Count - 1; i++)
                uv[i] = (uv[i - 1] + uv[i] * 2f + uv[i + 1]) * 0.25f;
        return uv;
    }

    /// <summary>
    /// Traces the river, draws it, blocks it on the NavMesh and returns spots along both banks for
    /// drinking. Call before the NavMesh bake.
    /// </summary>
    public static List<Vector3> Build(HimalayaTerrainBuilder tb, Transform parent, Material water, float sourceWidth, float mouthWidth)
    {
        Path.Clear();
        var banks = new List<Vector3>();
        var uv = Trace(tb);
        if (uv.Count < 2) return banks;

        foreach (var p in uv) Path.Add(tb.WorldPosition(p.x, p.y));

        var mesh = WaterMeshBuilder.Stream(tb, uv, sourceWidth, mouthWidth);
        if (mesh != null)
        {
            var go = new GameObject("River");
            go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = water;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Not-walkable boxes along the river, and drinking spots just outside each bank.
        var blockers = new GameObject("River (no walking)");
        blockers.transform.SetParent(parent, true);
        int notWalkable = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
        float sinceBank = 99f;
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Vector3 a = Path[i], b = Path[i + 1];
            Vector3 flat = b - a; flat.y = 0f;
            float len = flat.magnitude;
            if (len < 0.01f) continue;
            float t = i / (float)(Path.Count - 1);
            float width = Mathf.Lerp(sourceWidth, mouthWidth, t);

            var seg = new GameObject("Segment");
            seg.transform.SetParent(blockers.transform, true);
            seg.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(flat));
            var vol = seg.AddComponent<NavMeshModifierVolume>();
            vol.size = new Vector3(width + 0.6f, 40f, len + 0.6f);   // overlap so there are no gaps to sneak through
            vol.center = Vector3.zero;
            vol.area = notWalkable;

            sinceBank += len;
            if (sinceBank >= 10f)
            {
                sinceBank = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, flat.normalized) * (width * 0.5f + 1.5f);
                banks.Add(a + side);
                banks.Add(a - side);
            }
        }
        return banks;
    }

    /// <summary>Flat distance from a point to the river centre line (infinity if there is no river).</summary>
    public static float DistanceTo(Vector3 p)
    {
        float best = float.PositiveInfinity;
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Vector2 a = new Vector2(Path[i].x, Path[i].z), b = new Vector2(Path[i + 1].x, Path[i + 1].z), q = new Vector2(p.x, p.z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            best = Mathf.Min(best, (a + ab * t - q).magnitude);
        }
        return best;
    }

    /// <summary>+1 or -1: which side of the river a point is on (0 if there is no river).</summary>
    public static int Side(Vector3 p)
    {
        if (!Exists) return 0;
        float best = float.PositiveInfinity;
        int side = 0;
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Vector3 a = Path[i], b = Path[i + 1];
            Vector2 a2 = new Vector2(a.x, a.z), b2 = new Vector2(b.x, b.z), p2 = new Vector2(p.x, p.z);
            Vector2 ab = b2 - a2;
            float t = Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            float d = (a2 + ab * t - p2).sqrMagnitude;
            if (d < best)
            {
                best = d;
                side = (ab.x * (p2 - a2).y - ab.y * (p2 - a2).x) >= 0f ? 1 : -1;
            }
        }
        return side;
    }
}
