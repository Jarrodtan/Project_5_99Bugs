// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
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
        sheep.reproductionRate = 0.1f;

        leopard = Make<SpeciesDefinition>();
        leopard.speciesName = "SnowLeopard";
        leopard.SetRelationship(RelationshipType.Eats, sheep);
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
    public void Grazing_ReducesGrass_And_WellFedSheepGrow()
    {
        sheep.SetRelationship(RelationshipType.Eats, grass);
        sheep.foodNeededPerCapita = 0.5f;
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 0f;

        sim.Tick();

        // grass regrows 50 -> 55, sheep eat 10 * 0.5 = 5 -> 50 ; fed -> 10 * 1.1 = 11
        Assert.AreEqual(50f, grass.currentAmount, 1e-4f);
        Assert.AreEqual(11f, sim.populations[sheep], 1e-4f);
    }

    [Test]
    public void Grazing_NotEnoughGrass_SheepStarve_AndGrassHitsZero()
    {
        sheep.SetRelationship(RelationshipType.Eats, grass);
        sheep.foodNeededPerCapita = 0.5f;
        grass.regenRate = 0f;
        grass.currentAmount = 2f;
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 0f;

        sim.Tick();

        // wanted 5, got 2 -> ratio 0.4, shortfall 0.6 -> 10 - 10 * 0.6 * 0.2 = 8.8
        Assert.AreEqual(0f, grass.currentAmount, 1e-4f);
        Assert.AreEqual(8.8f, sim.populations[sheep], 1e-4f);
    }

    [Test]
    public void Grazing_ScarceGrass_IsSharedFairlyBetweenGrazers()
    {
        var goat = Make<SpeciesDefinition>();
        goat.foodNeededPerCapita = 1f;
        goat.SetRelationship(RelationshipType.Eats, grass);
        sheep.foodNeededPerCapita = 1f;
        sheep.SetRelationship(RelationshipType.Eats, grass);
        sim.species.Add(goat);
        grass.regenRate = 0f;
        grass.currentAmount = 10f;
        sim.populations[sheep] = 10f;
        sim.populations[goat] = 10f;
        sim.populations[leopard] = 0f;

        sim.Tick();

        // demand 20, only 10 -> both get half -> identical outcome for identical species
        Assert.AreEqual(0f, grass.currentAmount, 1e-4f);
        Assert.AreEqual(sim.populations[sheep], sim.populations[goat], 1e-4f);
    }

    [Test]
    public void MissingConfiguration_DoesNotThrow()
    {
        sim.species.Add(null);
        sim.resources.Add(null);
        leopard.relationships.Add(null);
        sim.populations[sheep] = 10f;
        // leopard intentionally has no population entry

        Assert.DoesNotThrow(() => sim.Tick());
    }

    // ---- Events mode ----

    [Test]
    public void Events_Tick_OnlyRegrowsResources_PopulationsUnchanged()
    {
        sim.mode = EcosystemSimulator.SimulationMode.Events;
        sim.eventRegenPerTick = 3f;
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 2f;

        sim.Tick();

        Assert.AreEqual(10f, sim.populations[sheep]);
        Assert.AreEqual(2f, sim.populations[leopard]);
        Assert.AreEqual(53f, grass.currentAmount, 1e-4f);
    }

    [Test]
    public void Events_Predation_SheepMinusOne_NoLeopardSpawnByDefault()
    {
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 2f;

        sim.RecordPredation(leopard, sheep);

        Assert.AreEqual(9f, sim.populations[sheep]);
        Assert.AreEqual(2f, sim.populations[leopard]);
    }

    [Test]
    public void Events_Predation_WithOffspringOn_LeopardPlusOne()
    {
        sim.eatingAddsOffspring = true;
        sim.populations[sheep] = 10f;
        sim.populations[leopard] = 2f;

        sim.RecordPredation(leopard, sheep);

        Assert.AreEqual(9f, sim.populations[sheep]);
        Assert.AreEqual(3f, sim.populations[leopard]);
    }

    [Test]
    public void Events_Predation_WithNoPreyLeft_ChangesNothing()
    {
        sim.populations[sheep] = 0f;
        sim.populations[leopard] = 2f;

        sim.RecordPredation(leopard, sheep);

        Assert.AreEqual(0f, sim.populations[sheep]);
        Assert.AreEqual(2f, sim.populations[leopard]);
    }

    [Test]
    public void Events_Eat_UsesResource_NoSpawnByDefault()
    {
        sim.populations[sheep] = 10f;

        bool ate = sim.RecordEat(sheep, grass, 20f);

        Assert.IsTrue(ate);
        Assert.AreEqual(10f, sim.populations[sheep]);
        Assert.AreEqual(30f, grass.currentAmount, 1e-4f);
    }

    [Test]
    public void Events_Eat_WithOffspringOn_SpeciesPlusOne()
    {
        sim.eatingAddsOffspring = true;
        sim.populations[sheep] = 10f;

        bool ate = sim.RecordEat(sheep, grass, 20f);

        Assert.IsTrue(ate);
        Assert.AreEqual(11f, sim.populations[sheep]);
        Assert.AreEqual(30f, grass.currentAmount, 1e-4f);
    }

    [Test]
    public void Events_Eat_NotEnoughResource_ChangesNothing()
    {
        sim.populations[sheep] = 10f;
        grass.currentAmount = 5f;

        bool ate = sim.RecordEat(sheep, grass, 20f);

        Assert.IsFalse(ate);
        Assert.AreEqual(10f, sim.populations[sheep]);
        Assert.AreEqual(5f, grass.currentAmount, 1e-4f);
    }

    [Test]
    public void Events_Starvation_MinusOne_NeverBelowZero()
    {
        sim.populations[leopard] = 1f;

        sim.RecordStarvation(leopard);
        sim.RecordStarvation(leopard);

        Assert.AreEqual(0f, sim.populations[leopard]);
    }
}
// [AI-GENERATED END]
