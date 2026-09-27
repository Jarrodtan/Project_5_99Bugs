using NUnit.Framework;
using UnityEngine;

// T01 success test: Blue Sheep can eat Grass, Snow Leopards can eat Blue Sheep,
// and relationships can be changed without code.
public class RelationshipTests
{
    SpeciesDefinition sheep;
    SpeciesDefinition leopard;
    ResourceDefinition grass;
    ResourceDefinition water;

    [SetUp]
    public void SetUp()
    {
        sheep = Create<SpeciesDefinition>("Blue Sheep");
        leopard = Create<SpeciesDefinition>("Snow Leopard");
        grass = Create<ResourceDefinition>("Grass");
        water = Create<ResourceDefinition>("Water");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(sheep);
        Object.DestroyImmediate(leopard);
        Object.DestroyImmediate(grass);
        Object.DestroyImmediate(water);
    }

    static T Create<T>(string name) where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        obj.name = name;
        return obj;
    }

    // Design doc worked example: Blue Sheep (eats: Grass, needs: Water), Snow Leopard (eats: Blue Sheep).
    [Test]
    public void DesignDocWorkedExample()
    {
        sheep.SetRelationship(RelationshipType.Eats, grass);
        sheep.SetRelationship(RelationshipType.Needs, water);
        leopard.SetRelationship(RelationshipType.Eats, sheep);

        Assert.IsTrue(sheep.HasRelationship(RelationshipType.Eats, grass));
        Assert.IsTrue(sheep.HasRelationship(RelationshipType.Needs, water));
        Assert.IsTrue(leopard.HasRelationship(RelationshipType.Eats, sheep));
        Assert.IsFalse(sheep.HasRelationship(RelationshipType.Eats, leopard));
        CollectionAssert.AreEqual(new[] { grass }, sheep.GetResourceTargets(RelationshipType.Eats));
        CollectionAssert.AreEqual(new[] { water }, sheep.GetResourceTargets(RelationshipType.Needs));
        CollectionAssert.AreEqual(new[] { sheep }, leopard.GetSpeciesTargets(RelationshipType.Eats));
        Assert.IsTrue(sheep.relationships.TrueForAll(r => r.IsValid));
        Assert.IsTrue(leopard.relationships.TrueForAll(r => r.IsValid));
    }

    [Test]
    public void Needs_MustTargetResource()
    {
        Assert.Throws<System.ArgumentException>(() => leopard.SetRelationship(RelationshipType.Needs, sheep));
        Assert.IsEmpty(leopard.relationships);

        // Changing an existing Eats-species link to Needs is also refused and leaves it as Eats.
        leopard.SetRelationship(RelationshipType.Eats, sheep);
        Assert.Throws<System.ArgumentException>(() => leopard.SetRelationship(RelationshipType.Needs, sheep));
        Assert.IsTrue(leopard.HasRelationship(RelationshipType.Eats, sheep));

        // Set directly (as the Inspector would), it's flagged invalid instead.
        var r = new Relationship { type = RelationshipType.Needs, targetSpecies = sheep };
        Assert.IsFalse(r.IsValid);
        StringAssert.Contains("Needs must target a resource", r.ValidationError);
    }

    [Test]
    public void Affects_AllowsSpeciesOrResource()
    {
        Assert.IsTrue(new Relationship(RelationshipType.Affects, sheep).IsValid);
        Assert.IsTrue(new Relationship(RelationshipType.Affects, grass).IsValid);
    }

    [Test]
    public void ChangingType_UpdatesExistingRelationship()
    {
        sheep.SetRelationship(RelationshipType.Eats, grass);
        sheep.SetRelationship(RelationshipType.Affects, grass);

        Assert.AreEqual(1, sheep.relationships.Count);
        Assert.IsFalse(sheep.HasRelationship(RelationshipType.Eats, grass));
        Assert.IsTrue(sheep.HasRelationship(RelationshipType.Affects, grass));
        Assert.IsEmpty(sheep.GetResourceTargets(RelationshipType.Eats));
    }

    [Test]
    public void ChangingTarget_InInspectorStyle_RetargetsRelationship()
    {
        // Same as a designer dragging a different asset into the field.
        sheep.SetRelationship(RelationshipType.Needs, grass);
        sheep.relationships[0].targetResource = water;

        Assert.IsTrue(sheep.HasRelationship(RelationshipType.Needs, water));
        Assert.IsFalse(sheep.HasRelationship(RelationshipType.Needs, grass));
    }

    [Test]
    public void RemoveRelationship_RemovesIt()
    {
        leopard.SetRelationship(RelationshipType.Eats, sheep);
        leopard.SetRelationship(RelationshipType.Needs, water);

        Assert.IsTrue(leopard.RemoveRelationship(sheep));
        Assert.IsFalse(leopard.HasRelationship(RelationshipType.Eats, sheep));
        Assert.IsTrue(leopard.HasRelationship(RelationshipType.Needs, water));
        Assert.IsFalse(leopard.RemoveRelationship(sheep));
    }

    [Test]
    public void IsValid_RequiresExactlyOneTarget()
    {
        Assert.IsFalse(new Relationship().IsValid);
        Assert.IsTrue(new Relationship(RelationshipType.Eats, grass).IsValid);
        Assert.IsTrue(new Relationship(RelationshipType.Eats, sheep).IsValid);
        Assert.IsFalse(new Relationship { targetSpecies = sheep, targetResource = grass }.IsValid);
    }

    [Test]
    public void NullTarget_IsNeverMatched()
    {
        sheep.relationships.Add(new Relationship());

        Assert.IsFalse(sheep.HasRelationship(RelationshipType.Eats, null));
        Assert.IsNull(sheep.FindRelationship(null));
    }
}
