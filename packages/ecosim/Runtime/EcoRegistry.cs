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
    }

    public static void Unregister(Transform instance, ScriptableObject definition)
    {
        if (instance == null || definition == null) return;
        if (byDefinition.TryGetValue(definition, out var list))
            list.Remove(instance);
    }

    /// <summary>Removes everything. Useful for tests and scene resets.</summary>
    public static void Clear() => byDefinition.Clear();

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
            list.RemoveAll(t => t == null);
            foreach (var t in list)
            {
                if (t == exclude || !t.gameObject.activeInHierarchy) continue;
                float sqr = (t.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = t; }
            }
        }
        return best;
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
