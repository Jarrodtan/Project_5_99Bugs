using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// EditMode tests for EcosystemSimulator.Tick(). Known starting conditions must
// always produce the same, hand-checkable populations (Technical Deliverable #5).
public class EcosystemSimulatorTests
{
    private readonly List<Object> created = new();

    private GameObject simObject;
    private EcosystemSimulator sim;
    private ResourceDefinition grass;
    private SpeciesDefinition sheep;
    private SpeciesDefinition leopard;

    [SetUp]
    public void SetUp()
    {
        grass = Make<ResourceDefinition>();
        grass.resourceName = "Grass";
        grass.regenRate = 5f;
        grass.maxAmount = 100f;
        grass.currentAmount = 50f;

        sheep = Make<SpeciesDefinition>();
        sheep.speciesName = "BlueSheep";
        sheep.eats = new List<SpeciesDefinition>();
        sheep.reproductionRate = 0.1f;

        leopard = Make<SpeciesDefinition>();
        leopard.speciesName = "SnowLeopard";
        leopard.eats = new List<SpeciesDefinition> { sheep };
        leopard.reproductionRate = 0.05f;
        leopard.predationRate = 0.01f;
        leopard.foodNeededPerCapita = 0.5f;
        leopard.starvationThreshold = 0.2f;

        sim = CreateSimulator();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    private T Make<T>() where T : ScriptableObject
    {
        var o = ScriptableObject.CreateInstance<T>();
        created.Add(o);
        return o;
    }

    private EcosystemSimulator CreateSimulator()
    {
        simObject = new GameObject("Sim");
        created.Add(simObject);
        var s = simObject.AddComponent<EcosystemSimulator>();
        s.species = new List<SpeciesDefinition> { sheep, leopard };
        s.resources = new List<ResourceDefinition> { grass };
        return s;
    }

    [Test]
    public void Resource_Regenerates_And_IsCappedAtMax()
    {
        sim.populations[sheep] = 0f;
        sim.populations[leopard] = 0f;

        sim.Tick();
        Assert.AreEqual(55f, grass.currentAmount, 1e-4f);

        for (int i = 0; i < 20; i++) sim.Tick();
        Assert.AreEqual(100f, grass.currentAmount, 1e-4f);
    }

    [Test]
    public void Prey_WithoutPredators_GrowsByReproductionRate()
    {
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 0f;

        sim.Tick();

        Assert.AreEqual(11f, sim.populations[sheep], 1e-4f);
    }

    [Test]
    public void Predation_ReducesPrey_And_WellFedPredatorsGrow()
    {
        sim.populations[sheep] = 100f;
        sim.populations[leopard] = 2f;

        sim.Tick();

        // eaten = 0.01 * 2 * 100 = 2 ; sheep = (100 - 2) * 1.1 = 107.8
        // leopard needs 2 * 0.5 = 1, got 2 -> well fed -> 2 * 1.05 = 2.1
        Assert.AreEqual(107.8f, sim.populations[sheep], 1e-3f);
        Assert.AreEqual(2.1f, sim.populations[leopard], 1e-4f);
    }

    [Test]
    public void Predators_WithNoPrey_Starve()
    {
        sim.populations[sheep] = 0f;
        sim.populations[leopard] = 10f;

        sim.Tick();

        // ratio 0 -> shortfall 1 -> 10 - 10 * 1 * 0.2 = 8
        Assert.AreEqual(8f, sim.populations[leopard], 1e-4f);
    }

    [Test]
    public void SameStartingConditions_ProduceIdenticalResults()
    {
        float[] RunOnce()
        {
            grass.currentAmount = 50f;
            var s = CreateSimulator();
            s.populations[sheep] = 50f;
            s.populations[leopard] = 3f;
            for (int i = 0; i < 50; i++) s.Tick();
            return new[] { s.populations[sheep], s.populations[leopard], grass.currentAmount };
        }

        CollectionAssert.AreEqual(RunOnce(), RunOnce());
    }

    [Test]
    public void Populations_NeverGoNegative()
    {
        leopard.predationRate = 5f;
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 10f;

        for (int i = 0; i < 100; i++)
        {
            sim.Tick();
            Assert.GreaterOrEqual(sim.populations[sheep], 0f);
            Assert.GreaterOrEqual(sim.populations[leopard], 0f);
        }
    }

    [Test]
    public void MissingConfiguration_DoesNotThrow()
    {
        sim.species.Add(null);
        sim.resources.Add(null);
        leopard.eats.Add(null);
        sim.populations[sheep] = 10f;
        // leopard intentionally has no population entry

        Assert.DoesNotThrow(() => sim.Tick());
    }
}
