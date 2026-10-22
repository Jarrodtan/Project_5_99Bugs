// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared lookup of in-world ecosystem objects (animals, grass patches, ...), keyed by their
/// definition asset. Lets behaviour code ask "where is the nearest X?" without tags or
/// FindObjectsOfType. Objects join via <see cref="EcoEntity"/>; "who eats whom" comes from
/// the T01 relationships on <see cref="SpeciesDefinition"/>.
/// </summary>
public static class EcoRegistry
{
    static readonly Dictionary<ScriptableObject, List<Transform>> byDefinition = new Dictionary<ScriptableObject, List<Transform>>();

    public static void Register(Transform instance, ScriptableObject definition)
    {
        if (instance == null || definition == null) return;
        if (!byDefinition.TryGetValue(definition, out var list))
            byDefinition[definition] = list = new List<Transform>();
        if (!list.Contains(instance))
            list.Add(instance);
        if (grids.TryGetValue(definition, out var grid)) grid.builtFrame = -1;
    }

    public static void Unregister(Transform instance, ScriptableObject definition)
    {
        if (instance == null || definition == null) return;
        if (byDefinition.TryGetValue(definition, out var list))
            list.Remove(instance);
        if (grids.TryGetValue(definition, out var grid)) grid.builtFrame = -1;
    }

    /// <summary>Removes everything. Useful for tests and scene resets.</summary>
    public static void Clear()
    {
        byDefinition.Clear();
        grids.Clear();
    }

    /// <summary>Number of live registered instances of a definition.</summary>
    public static int Count(ScriptableObject definition)
    {
        if (definition == null || !byDefinition.TryGetValue(definition, out var list)) return 0;
        list.RemoveAll(t => t == null);
        return list.Count;
    }

    /// <summary>Nearest instance of any of <paramref name="definitions"/> within <paramref name="maxDistance"/>, or null.</summary>
    public static Transform Nearest(Vector3 from, IEnumerable<ScriptableObject> definitions, float maxDistance = float.PositiveInfinity, Transform exclude = null)
    {
        Transform best = null;
        float bestSqr = maxDistance * maxDistance;
        if (definitions == null) return null;

        foreach (var def in definitions)
        {
            if (def == null || !byDefinition.TryGetValue(def, out var list)) continue;
            if (list.Count < GridThreshold)
                NearestLinear(list, from, exclude, ref best, ref bestSqr);
            else
                GridFor(def, list).Nearest(from, exclude, ref best, ref bestSqr);
        }
        return best;
    }

    static void NearestLinear(List<Transform> list, Vector3 from, Transform exclude, ref Transform best, ref float bestSqr)
    {
        list.RemoveAll(t => t == null);
        foreach (var t in list)
        {
            if (t == exclude || !t.gameObject.activeInHierarchy) continue;
            float sqr = (t.position - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = t; }
        }
    }

    // ---- Spatial hash grid (SCRUM-265) ----
    // With many animals, every Hunt/Flee/Forage query scanning every instance is O(n) per animal,
    // O(n^2) per frame. Each definition with GridThreshold+ instances gets a grid of CellSize-wide
    // XZ cells, rebuilt at most once per frame (animals move), so a query only looks at nearby cells.

    /// <summary>Below this many instances a plain scan is faster than building a grid.</summary>
    public const int GridThreshold = 32;
    /// <summary>Grid cell width in world units (about one hunt/flee radius).</summary>
    public const float CellSize = 8f;

    static readonly Dictionary<ScriptableObject, SpatialGrid> grids = new Dictionary<ScriptableObject, SpatialGrid>();

    /// <summary>
    /// Debug view: fills <paramref name="into"/> with every grid cell (x, z index) that holds a live
    /// registered object and how many it holds. Cell (x, z) spans [x*CellSize, (x+1)*CellSize) in world XZ.
    /// </summary>
    public static void CountPerCell(Dictionary<Vector2Int, int> into)
    {
        into.Clear();
        foreach (var list in byDefinition.Values)
            foreach (var t in list)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                var key = new Vector2Int(Mathf.FloorToInt(t.position.x / CellSize), Mathf.FloorToInt(t.position.z / CellSize));
                into[key] = into.GetValueOrDefault(key) + 1;
            }
    }

    /// <summary>True when lookups for <paramref name="definition"/> use the grid instead of a plain scan.</summary>
    public static bool UsesGrid(ScriptableObject definition) => Count(definition) >= GridThreshold;

    /// <summary>Forces grids to rebuild on the next query (objects moved within the same frame, e.g. in tests).</summary>
    public static void InvalidateGrids()
    {
        foreach (var g in grids.Values) g.builtFrame = -1;
    }

    static SpatialGrid GridFor(ScriptableObject def, List<Transform> list)
    {
        if (!grids.TryGetValue(def, out var grid))
            grids[def] = grid = new SpatialGrid();
        if (grid.builtFrame != Time.frameCount)
        {
            list.RemoveAll(t => t == null);
            grid.Rebuild(list);
            grid.builtFrame = Time.frameCount;
        }
        return grid;
    }

    sealed class SpatialGrid
    {
        public int builtFrame = -1;
        readonly Dictionary<(int, int), List<Transform>> cells = new Dictionary<(int, int), List<Transform>>();
        int minX, maxX, minZ, maxZ;

        static int Cell(float v) => Mathf.FloorToInt(v / CellSize);

        public void Rebuild(List<Transform> list)
        {
            foreach (var c in cells.Values) c.Clear();
            minX = minZ = int.MaxValue;
            maxX = maxZ = int.MinValue;
            foreach (var t in list)
            {
                var p = t.position;
                var key = (Cell(p.x), Cell(p.z));
                if (!cells.TryGetValue(key, out var c)) cells[key] = c = new List<Transform>();
                c.Add(t);
                minX = Mathf.Min(minX, key.Item1); maxX = Mathf.Max(maxX, key.Item1);
                minZ = Mathf.Min(minZ, key.Item2); maxZ = Mathf.Max(maxZ, key.Item2);
            }
        }

        // Searches rings of cells outward from the query cell. Anything outside ring r is at
        // least r*CellSize away, so it stops once the best hit is closer than that.
        public void Nearest(Vector3 from, Transform exclude, ref Transform best, ref float bestSqr)
        {
            if (minX > maxX) return; // empty
            int cx = Cell(from.x), cz = Cell(from.z);
            int lastRing = Mathf.Max(Mathf.Max(Mathf.Abs(cx - minX), Mathf.Abs(cx - maxX)),
                                     Mathf.Max(Mathf.Abs(cz - minZ), Mathf.Abs(cz - maxZ)));

            for (int r = 0; r <= lastRing; r++)
            {
                float ringDist = (r - 1) * CellSize;
                if (r > 0 && ringDist * ringDist >= bestSqr) return;

                for (int x = cx - r; x <= cx + r; x++)
                for (int z = cz - r; z <= cz + r; z++)
                {
                    if (Mathf.Abs(x - cx) != r && Mathf.Abs(z - cz) != r) continue; // ring edge only
                    if (!cells.TryGetValue((x, z), out var c)) continue;
                    foreach (var t in c)
                    {
                        if (t == null || t == exclude || !t.gameObject.activeInHierarchy) continue;
                        float sqr = (t.position - from).sqrMagnitude;
                        if (sqr < bestSqr) { bestSqr = sqr; best = t; }
                    }
                }
            }
        }
    }

    public static Transform Nearest(Vector3 from, ScriptableObject definition, float maxDistance = float.PositiveInfinity, Transform exclude = null)
        => Nearest(from, new[] { definition }, maxDistance, exclude);

    /// <summary>Nearest animal that <paramref name="eater"/> Eats.</summary>
    public static Transform NearestPrey(SpeciesDefinition eater, Vector3 from, float maxDistance = float.PositiveInfinity)
        => eater == null ? null : Nearest(from, eater.GetSpeciesTargets(RelationshipType.Eats), maxDistance);

    /// <summary>Nearest resource object (e.g. a grass patch) that <paramref name="eater"/> Eats.</summary>
    public static Transform NearestFood(SpeciesDefinition eater, Vector3 from, float maxDistance = float.PositiveInfinity)
        => eater == null ? null : Nearest(from, eater.GetResourceTargets(RelationshipType.Eats), maxDistance);

    /// <summary>Nearest animal whose species Eats <paramref name="prey"/>.</summary>
    public static Transform NearestPredator(SpeciesDefinition prey, Vector3 from, float maxDistance = float.PositiveInfinity)
    {
        if (prey == null) return null;
        var predators = new List<ScriptableObject>();
        foreach (var def in byDefinition.Keys)
            if (def is SpeciesDefinition s && s.HasRelationship(RelationshipType.Eats, prey))
                predators.Add(s);
        return Nearest(from, predators, maxDistance);
    }
}
// [AI-GENERATED END]
