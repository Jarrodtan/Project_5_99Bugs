// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EcoRegistryTests
{
    SpeciesDefinition sheep, leopard;
    ResourceDefinition grass;
    readonly List<Object> created = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        EcoRegistry.Clear();
        sheep = Make<SpeciesDefinition>();
        leopard = Make<SpeciesDefinition>();
        grass = Make<ResourceDefinition>();
        sheep.SetRelationship(RelationshipType.Eats, grass);
        leopard.SetRelationship(RelationshipType.Eats, sheep);
    }

    [TearDown]
    public void TearDown()
    {
        EcoRegistry.Clear();
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    T Make<T>() where T : ScriptableObject
    {
        var so = ScriptableObject.CreateInstance<T>();
        created.Add(so);
        return so;
    }

    Transform Spawn(ScriptableObject def, float x)
    {
        var go = new GameObject();
        go.transform.position = new Vector3(x, 0f, 0f);
        created.Add(go);
        EcoRegistry.Register(go.transform, def);
        return go.transform;
    }

    [Test]
    public void Nearest_ReturnsClosestInstance()
    {
        Spawn(grass, 10f);
        var near = Spawn(grass, 2f);
        Assert.AreSame(near, EcoRegistry.Nearest(Vector3.zero, grass));
    }

    [Test]
    public void Nearest_RespectsMaxDistance()
    {
        Spawn(grass, 10f);
        Assert.IsNull(EcoRegistry.Nearest(Vector3.zero, grass, 5f));
    }

    [Test]
    public void NearestFood_UsesEatsRelationship()
    {
        var patch = Spawn(grass, 3f);
        Spawn(sheep, 1f); // closer, but sheep don't eat sheep
        Assert.AreSame(patch, EcoRegistry.NearestFood(sheep, Vector3.zero));
    }

    [Test]
    public void NearestPrey_FindsWhatThePredatorEats()
    {
        var prey = Spawn(sheep, 4f);
        Spawn(grass, 1f);
        Assert.AreSame(prey, EcoRegistry.NearestPrey(leopard, Vector3.zero));
    }

    [Test]
    public void NearestPredator_FindsSpeciesThatEatsMe()
    {
        var hunter = Spawn(leopard, 6f);
        Assert.AreSame(hunter, EcoRegistry.NearestPredator(sheep, Vector3.zero));
        Assert.IsNull(EcoRegistry.NearestPredator(leopard, Vector3.zero));
    }

    [Test]
    public void Unregister_And_DestroyedObjects_AreIgnored()
    {
        var a = Spawn(grass, 1f);
        var b = Spawn(grass, 2f);
        EcoRegistry.Unregister(a, grass);
        Object.DestroyImmediate(b.gameObject);
        Assert.IsNull(EcoRegistry.Nearest(Vector3.zero, grass));
        Assert.AreEqual(0, EcoRegistry.Count(grass));
    }

    [Test]
    public void NullInputs_DoNotThrow()
    {
        EcoRegistry.Register(null, grass);
        EcoRegistry.Register(Spawn(grass, 1f), null);
        Assert.IsNull(EcoRegistry.NearestPrey(null, Vector3.zero));
        Assert.IsNull(EcoRegistry.NearestPredator(null, Vector3.zero));
        Assert.AreEqual(0, EcoRegistry.Count(null));
    }
}
// [AI-GENERATED END]
