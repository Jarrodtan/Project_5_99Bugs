using UnityEngine;

// Relationship types from the design doc: "what eats/needs/affects what, configurable per entity".
public enum RelationshipType
{
    // Food. Target a resource to graze (Blue Sheep eats Grass)
    // or a species to hunt (Snow Leopard eats Blue Sheep).
    Eats,

    // Something the species must have that isn't food, e.g. Blue Sheep needs Water.
    // Must target a resource.
    Needs,

    // Looser effect on another species or resource. The design doc has no example yet.
    Affects
}

// One link from a species to something else, e.g. Snow Leopard -> Eats -> Blue Sheep.
// Stored in SpeciesDefinition.relationships and set up by dragging assets in the Inspector.
[System.Serializable]
public class Relationship
{
    [Tooltip("Eats: food. A resource is grazed (Blue Sheep eats Grass), a species is hunted (Snow Leopard eats Blue Sheep).\n" +
             "Needs: a resource it must have, e.g. Water.\n" +
             "Affects: looser effect, no example in the design doc yet.")]
    public RelationshipType type;

    // Only one of these should be set, depending on what this relationship targets.
    [Tooltip("Set for Eats (hunting) or Affects. Leave empty if Target Resource is set. Not allowed for Needs.")]
    public SpeciesDefinition targetSpecies;
    [Tooltip("Set for Eats (grazing), Needs (e.g. Water) or Affects. Leave empty if Target Species is set.")]
    public ResourceDefinition targetResource;

    public Relationship() { }

    public Relationship(RelationshipType type, SpeciesDefinition targetSpecies)
    {
        this.type = type;
        this.targetSpecies = targetSpecies;
    }

    public Relationship(RelationshipType type, ResourceDefinition targetResource)
    {
        this.type = type;
        this.targetResource = targetResource;
    }

    // Whichever target is set (species or resource), or null if neither.
    public ScriptableObject Target => targetSpecies != null ? targetSpecies : targetResource;

    // True when exactly one target is set and it's a kind the type allows.
    public bool IsValid => ValidationError == null;

    // Why this relationship is invalid, or null if it's fine.
    public string ValidationError
    {
        get
        {
            if (targetSpecies == null && targetResource == null) return "has no target";
            if (targetSpecies != null && targetResource != null) return "has both a species and a resource target; set only one";
            if (type == RelationshipType.Needs && targetSpecies != null) return "is Needs but targets a species; Needs must target a resource (e.g. Water)";
            return null;
        }
    }

    public bool Targets(ScriptableObject target)
    {
        return target != null && (targetSpecies == target || targetResource == target);
    }

    public override string ToString()
    {
        return $"{type} {(Target != null ? Target.name : "<none>")}";
    }
}
