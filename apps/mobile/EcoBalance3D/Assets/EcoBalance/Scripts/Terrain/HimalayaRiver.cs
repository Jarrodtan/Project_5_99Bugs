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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    /// <summary>Side streams from glacier snouts down to the main river, world space, source first.
    /// Shallow: animals can wade them, so they are not cut out of the NavMesh and don't split the map.</summary>
    public static readonly List<List<Vector3>> Tributaries = new List<List<Vector3>>();
    const float RiverStartMeters = 4950f;   // Khumbu Glacier snout is about 4,900-5,000 m
    const int TributaryCount = 5;   // a stream in most side valleys, like the real Khumbu
    const float StreamSourceWidth = 2f, StreamMouthWidth = 3.5f;   // wide enough to see on the 200 m map
    const float FordEvery = 30f, FordWidth = 4f;                     // a shallow crossing every 30 m
    const float GlacierSnoutMinMeters = 4500f, GlacierSnoutMaxMeters = 5300f;   // valley heads just under the ice, where meltwater starts
    // [AI-GENERATED END]

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
        lastGrid = h; lastCells = cells;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): kept for the side streams

        // Smooth the zig-zag of the grid into a natural bend.
        var uv = new List<Vector2>();
        foreach (var c in cells) uv.Add(new Vector2(c.x / (Grid - 1f), c.y / (Grid - 1f)));
        for (int pass = 0; pass < 8; pass++)   // [AI-GENERATED] Claude Code (Claude Opus 5.5): more passes = natural curves, not grid zig-zags
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
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // The river starts where the glacier ends (the snout), not at the map edge. Above it is ice the
        // animals can walk over, so the long way round, high over the valley head, joins the two sides.
        int snout = 0;
        while (snout < uv.Count - 2 && tb.ElevationMeters(uv[snout].x, uv[snout].y) > RiverStartMeters) snout++;
        if (snout > 0) { uv.RemoveRange(0, snout); if (lastCells != null && lastCells.Count > snout) lastCells.RemoveRange(0, snout); }   // streams join the real river only
        // [AI-GENERATED END]
        if (uv.Count < 2) return banks;

        foreach (var p in uv) Path.Add(tb.WorldPosition(p.x, p.y));
        BuildTributaries(tb, parent, water, banks);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

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
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): overlap by a full river width, so the outside of a bend
            // has no wedge-shaped gap animals could walk through onto the water.
            vol.size = new Vector3(width + 1f, 40f, len + width + 1f);
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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    static float[,] lastGrid;
    static List<Vector2Int> lastCells;

    // Glacier snouts (valley heads at 4,500-5,300 m, away from the map edge and from other water),
    // each traced to the main river. A route that climbs over a ridge is dropped: water only runs downhill.
    static void BuildTributaries(HimalayaTerrainBuilder tb, Transform parent, Material water, List<Vector3> banks)
    {
        Tributaries.Clear();
        if (lastGrid == null || lastCells == null || lastCells.Count < 2) return;
        var onRiver = new HashSet<Vector2Int>(lastCells);
        var sources = new List<Vector2Int>();
        var wet = new List<Vector2Int>(lastCells);   // river + accepted streams
        int margin = Grid / 10;
        for (int k = 0; k < TributaryCount * 3 && Tributaries.Count < TributaryCount; k++)
        {
            Vector2Int best = default; float bestScore = float.NegativeInfinity;
            for (int x = margin; x < Grid - margin; x++)
                for (int y = margin; y < Grid - margin; y++)
                {
                    float m = tb.ElevationMeters(x / (Grid - 1f), y / (Grid - 1f));
                    if (m < GlacierSnoutMinMeters || m > GlacierSnoutMaxMeters) continue;
                    var c = new Vector2Int(x, y);
                    float dRiver = MinCellDistance(c, wet);
                    if (dRiver < Grid / 6f) continue;
                    float dOthers = sources.Count == 0 ? Grid : MinCellDistance(c, sources);
                    if (dOthers < Grid / 4f) continue;
                    // Valley floor: lower than its neighbours on average.
                    float around = (lastGrid[x - 2, y] + lastGrid[x + 2, y] + lastGrid[x, y - 2] + lastGrid[x, y + 2]) * 0.25f;
                    float score = (around - lastGrid[x, y]) * 20f + Mathf.Min(dRiver, dOthers) * 0.5f;   // a valley, far from water
                    if (score > bestScore) { bestScore = score; best = c; }
                }
            if (bestScore == float.NegativeInfinity) break;
            sources.Add(best);

            var cells = Route(lastGrid, best, onRiver);
            if (cells.Count < 3) continue;
            float srcMeters = tb.ElevationMeters(best.x / (Grid - 1f), best.y / (Grid - 1f)), top = 0f;
            foreach (var c in cells) top = Mathf.Max(top, tb.ElevationMeters(c.x / (Grid - 1f), c.y / (Grid - 1f)));
            if (top > srcMeters + 50f) continue;   // would flow uphill over a ridge
            wet.AddRange(cells);
            var uv = new List<Vector2>();
            foreach (var c in cells) uv.Add(new Vector2(c.x / (Grid - 1f), c.y / (Grid - 1f)));
            for (int pass = 0; pass < 8; pass++)   // [AI-GENERATED] Claude Code (Claude Opus 5.5): more passes = natural curves, not grid zig-zags
                for (int i = 1; i < uv.Count - 1; i++)
                    uv[i] = (uv[i - 1] + uv[i] * 2f + uv[i + 1]) * 0.25f;

            var path = new List<Vector3>();
            foreach (var p in uv) path.Add(tb.WorldPosition(p.x, p.y));
            Tributaries.Add(path);

            var mesh = WaterMeshBuilder.Stream(tb, uv, StreamSourceWidth, StreamMouthWidth);
            if (mesh != null)
            {
                var go = new GameObject($"Glacier stream {Tributaries.Count}");
                go.transform.SetParent(parent, true);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = water;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): animals don't walk in the stream, only cross at
            // shallow fords (a gap in the blocker every FordEvery metres), like real meltwater streams.
            var noWalk = new GameObject($"Glacier stream {Tributaries.Count} (no walking)");
            noWalk.transform.SetParent(parent, true);
            int notWalkableArea = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            float run = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i], b = path[i + 1];
                Vector3 flat = b - a; flat.y = 0f;
                float len = flat.magnitude;
                if (len < 0.01f) continue;
                run += len;
                if (run % FordEvery < FordWidth) continue;   // leave a ford here
                float w = Mathf.Lerp(StreamSourceWidth, StreamMouthWidth, i / (float)(path.Count - 1));
                var seg = new GameObject("Segment");
                seg.transform.SetParent(noWalk.transform, true);
                seg.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(flat));
                var vol = seg.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
                vol.size = new Vector3(w + 0.6f, 40f, len + w * 0.5f);
                vol.center = Vector3.zero;
                vol.area = notWalkableArea;
            }

            // Drinking spots along it, about every 10 m.
            float since = 99f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                since += Vector3.Distance(path[i], path[i + 1]);
                if (since >= 10f)
                {
                    since = 0f;
                    // On the bank, beside the visible water, so animals drink where you can see the stream.
                    Vector3 flat = path[i + 1] - path[i]; flat.y = 0f;
                    float w = Mathf.Lerp(StreamSourceWidth, StreamMouthWidth, i / (float)(path.Count - 1));
                    Vector3 side = Vector3.Cross(Vector3.up, flat.normalized) * (w * 0.5f + 1f);
                    banks.Add(path[i] + side);
                    banks.Add(path[i] - side);
                }
            }
        }
    }

    // Cheapest downhill route from a cell to any cell of the target set (same costs as the main river).
    static List<Vector2Int> Route(float[,] h, Vector2Int start, HashSet<Vector2Int> targets)
    {
        var cost = new float[Grid, Grid];
        var from = new Vector2Int[Grid, Grid];
        for (int x = 0; x < Grid; x++) for (int y = 0; y < Grid; y++) cost[x, y] = float.PositiveInfinity;
        cost[start.x, start.y] = 0f;
        var open = new SortedSet<(float c, int x, int y)> { (0f, start.x, start.y) };
        Vector2Int end = start; bool found = false;
        while (open.Count > 0)
        {
            var cur = open.Min; open.Remove(cur);
            var here = new Vector2Int(cur.x, cur.y);
            if (targets.Contains(here)) { end = here; found = true; break; }
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
                    if (c < cost[nx, ny]) { cost[nx, ny] = c; from[nx, ny] = here; open.Add((c, nx, ny)); }
                }
        }
        var cells = new List<Vector2Int>();
        if (!found) return cells;
        for (var p = end; ; p = from[p.x, p.y])
        {
            cells.Add(p);
            if (p == start || cells.Count > Grid * Grid) break;
        }
        cells.Reverse();
        return cells;
    }

    static float MinCellDistance(Vector2Int c, List<Vector2Int> set)
    {
        float best = float.PositiveInfinity;
        foreach (var s in set) best = Mathf.Min(best, Vector2Int.Distance(c, s));
        return best;
    }

    /// <summary>Flat distance to the nearest water: main river or a side stream.</summary>
    public static float DistanceToAnyWater(Vector3 p)
    {
        float best = DistanceTo(p);
        foreach (var t in Tributaries)
            for (int i = 0; i < t.Count; i++)
            {
                float d = new Vector2(t[i].x - p.x, t[i].z - p.z).magnitude;
                if (d < best) best = d;
            }
        return best;
    }
    // [AI-GENERATED END]

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
