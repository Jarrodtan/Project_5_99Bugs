// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PopulationHistoryTests
{
    static Dictionary<string, float> V(float sheep) => new() { ["BlueSheep"] = sheep };

    [Test]
    public void Add_KeepsOrderAndLatest()
    {
        var h = new PopulationHistory(10);
        h.Add(0, V(10)); h.Add(1, V(12)); h.Add(2, V(9));
        Assert.AreEqual(3, h.Count);
        Assert.AreEqual(0, h[0].Tick);
        Assert.AreEqual(9f, h.Latest.Get("BlueSheep"));
        CollectionAssert.AreEqual(new[] { 10f, 12f, 9f }, h.Series("BlueSheep"));
    }

    [Test]
    public void Full_DropsOldestFirst()
    {
        var h = new PopulationHistory(3);
        for (int t = 0; t < 5; t++) h.Add(t, V(t));
        Assert.AreEqual(3, h.Count);
        Assert.AreEqual(2, h[0].Tick);
        Assert.AreEqual(4, h.Latest.Tick);
    }

    [Test]
    public void Back_ClampsToOldest()
    {
        var h = new PopulationHistory(10);
        for (int t = 0; t < 4; t++) h.Add(t, V(t * 10));
        Assert.AreEqual(10f, h.Back(2).Get("BlueSheep"));
        Assert.AreEqual(0f, h.Back(99).Get("BlueSheep"));
    }

    [Test]
    public void UnknownKey_ReadsZero()
    {
        var h = new PopulationHistory(2);
        h.Add(0, V(5));
        Assert.AreEqual(0f, h.Latest.Get("SnowLeopard"));
        Assert.IsFalse(h.Latest.Has("SnowLeopard"));
    }

    [Test]
    public void Recorder_RecordsTicksAndRestartsOnReset()
    {
        var created = new List<Object>();
        var sheep = ScriptableObject.CreateInstance<SpeciesDefinition>(); sheep.name = "BlueSheep"; sheep.startingPopulation = 10; created.Add(sheep);
        var grass = ScriptableObject.CreateInstance<ResourceDefinition>(); grass.name = "Grass"; grass.startingAmount = 50; grass.maxAmount = 100; created.Add(grass);
        var go = new GameObject("Sim"); created.Add(go);
        var sim = go.AddComponent<EcosystemSimulator>();
        sim.mode = EcosystemSimulator.SimulationMode.Events;
        sim.eventRegenPerTick = 5f;
        sim.species = new List<SpeciesDefinition> { sheep };
        sim.resources = new List<ResourceDefinition> { grass };
        sim.ResetEcosystem();
        var rec = go.AddComponent<PopulationHistoryRecorder>();
        rec.Bind(sim);
        try
        {
            Assert.AreEqual(1, rec.History.Count, "starting state recorded as tick 0");

            sim.Tick(); sim.RecordStarvation(sheep); sim.Tick();
            Assert.AreEqual(3, rec.History.Count);
            Assert.AreEqual(2, rec.History.Latest.Tick);
            Assert.AreEqual(9f, rec.History.Latest.Get("BlueSheep"));
            Assert.AreEqual(60f, rec.History.Latest.Get("Grass"));

            sim.ResetEcosystem();
            Assert.AreEqual(1, rec.History.Count);
            Assert.AreEqual(0, rec.History.Latest.Tick);
            Assert.AreEqual(10f, rec.History.Latest.Get("BlueSheep"));
        }
        finally
        {
            foreach (var o in created) Object.DestroyImmediate(o);
        }
    }
}
// [AI-GENERATED END]
