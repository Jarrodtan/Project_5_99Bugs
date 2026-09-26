using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "EcoSim/Species")]
public class SpeciesDefinition : ScriptableObject
{
    public string speciesName;

    // What this species eats / needs / affects. Replaces the old eats and needs lists.
    [Tooltip("e.g. Blue Sheep: Eats Grass, Needs Water. Snow Leopard: Eats Blue Sheep.")]
    public List<Relationship> relationships = new List<Relationship>();

    public float reproductionRate = 0.1f;
    public float starvationThreshold = 0.2f;

    [Tooltip("Fraction of a prey species' population this species can eat, per predator, per tick. Only relevant if 'Eats' is non-empty.")]
    public float predationRate = 0.3f;
 
    [Tooltip("How much food (prey eaten) one individual of this species needs per tick to grow normally. Only relevant if 'Eats' is non-empty.")]
    public float foodNeededPerCapita = 0.5f;
    public GameObject visualPrefab;

    public bool HasRelationship(RelationshipType type, ScriptableObject target)
    {
        foreach (var r in relationships)
        {
            if (r != null && r.type == type && r.Targets(target)) return true;
        }
        return false;
    }

    private Dictionary<ScriptableObject, Relationship> relationship_cache;
    public void InitializeCache()
    {
        relationship_cache = new Dictionary<ScriptableObject, Relationship>();
        foreach (var r in relationships)
        {
            if (r != null && r.Target != null && !relationship_cache.ContainsKey(r.Target))
            {
                relationship_cache.Add(r.Target, r);
            }
        }
    }

    // The relationship this species has with target, or null if there isn't one.
    public Relationship FindRelationship(ScriptableObject target)
    {
        if (relationship_cache == null) InitializeCache();

        if (relationship_cache.TryGetValue(target, out Relationship rel)) return rel;

        return null;
    }

    public List<SpeciesDefinition> GetSpeciesTargets(RelationshipType type)
    {
        var result = new List<SpeciesDefinition>();
        foreach (var r in relationships)
        {
            if (r != null && r.type == type && r.targetSpecies != null) result.Add(r.targetSpecies);
        }
        return result;
    }

    public List<ResourceDefinition> GetResourceTargets(RelationshipType type)
    {
        var result = new List<ResourceDefinition>();
        foreach (var r in relationships)
        {
            if (r != null && r.type == type && r.targetResource != null) result.Add(r.targetResource);
        }
        return result;
    }

    // Adds a relationship to target, or changes the type of the existing one.
    // A species has at most one relationship per target.
    public void SetRelationship(RelationshipType type, SpeciesDefinition target)
    {
        if (type == RelationshipType.Needs)
            throw new System.ArgumentException("Needs must target a resource (e.g. Water), not a species.", nameof(type));

        var existing = FindRelationship(target);
        if (existing != null) existing.type = type;
        else relationships.Add(new Relationship(type, target));
    }

    public void SetRelationship(RelationshipType type, ResourceDefinition target)
    {
        var existing = FindRelationship(target);
        if (existing != null) existing.type = type;
        else relationships.Add(new Relationship(type, target));
    }

    public bool RemoveRelationship(ScriptableObject target)
    {
        return relationships.RemoveAll(r => r != null && r.Targets(target)) > 0;
    }

    void OnValidate()
    {
        for (int i = 0; i < relationships.Count; i++)
        {
            var r = relationships[i];
            // Skip rows the designer has just added and not filled in yet.
            if (r == null || r.Target == null) continue;

            if (!r.IsValid)
                Debug.LogWarning($"{name}: relationship {i} {r.ValidationError}.", this);
            if (r.targetSpecies == this && r.type == RelationshipType.Eats)
                Debug.LogWarning($"{name}: relationship {i} makes this species eat itself.", this);
        }
    }
}
