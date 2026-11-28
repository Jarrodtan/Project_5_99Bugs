// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// The Field Journal's Food & Water view: famine / thirst levels, grass eaten and water drunk, the recorder
// hook that stores them each day, the notes they add, and the graph drawing them.
public class FoodWaterGraphTests
{
    readonly List<Object> made = new();

    T Make<T>(string name) where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); o.name = name; made.Add(o); return o; }
    GameObject Go(string name, params System.Type[] c) { var g = new GameObject(name, c); made.Add(g); return g; }

    [TearDown]
    public void Clean()
    {
        foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
        made.Clear();
        ResourcePatch.All.Clear();
        typeof(FieldJournalObservations).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }

    static void Call(object o, string method) =>
        o.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, null);

    // ---------- Rules ----------

    [Test]
    public void NeedLevel_IsHundredMinusTheAverage()
    {
        Assert.AreEqual(0f, FoodWater.NeedLevel(new[] { 100f, 100f }));
        Assert.AreEqual(75f, FoodWater.NeedLevel(new[] { 50f, 0f }));
        Assert.AreEqual(0f, FoodWater.NeedLevel(new float[0]), "no animals: no famine");
        Assert.AreEqual(100f, FoodWater.NeedLevel(new[] { -20f }), "values are kept inside 0-100");
    }

    [TestCase(0f, "Well fed")]
    [TestCase(24.9f, "Well fed")]
    [TestCase(25f, "A bit hungry")]
    [TestCase(50f, "Hungry")]
    [TestCase(80f, "Famine!")]
    [TestCase(100f, "Famine!")]
    public void FamineBand_MatchesTheDocsFullnessLevels(float level, string label)
    {
        Assert.AreEqual(label, FoodWater.FamineBand(level).label);
    }

    [Test]
    public void ThirstBand_UsesTheSameLevels()
    {
        Assert.AreEqual("Not thirsty", FoodWater.ThirstBand(10f).label);
        Assert.AreEqual("Thirsty", FoodWater.ThirstBand(60f).label);
        Assert.AreEqual("Very thirsty!", FoodWater.ThirstBand(90f).label);
    }

    [Test]
    public void InLastDay_IsTheTotalNowMinusTheTotalADayAgo()
    {
        var log = new List<(float, float)> { (1.0f, 0f), (1.5f, 2f), (2.2f, 5f) };
        Assert.AreEqual(5f, FoodWater.InLastDay(log, 2.6f, 7f), 1e-4f);   // a day ago (1.6) the total was 2
        Assert.AreEqual(3f, FoodWater.InLastDay(log, 1.4f, 3f), 1e-4f, "less than a day in: counted from the start");
        Assert.AreEqual(0f, FoodWater.InLastDay(null, 5f, 0f));
    }

    [TestCase(3f, "3")]
    [TestCase(2.5f, "2.5")]
    [TestCase(12.4f, "12")]
    public void Amount_UsesADecimalOnlyWhenItHelps(float v, string text) => Assert.AreEqual(text, FoodWater.Amount(v));

    [Test]
    public void BuildSeries_HasEveryFoodAndWaterLine_AndLinksTheGrazers()
    {
        var sheep = new PopulationSeries { Key = "BlueSheep", Label = "Blue Sheep", Eats = new List<string> { "Grass" } };
        var leopard = new PopulationSeries { Key = "SnowLeopard", Label = "Snow Leopard", Eats = new List<string> { "BlueSheep" } };
        var food = FoodWater.BuildSeries(new[] { sheep, leopard });
        CollectionAssert.AreEqual(new[] { FoodWater.GrassLeft, FoodWater.GrassEaten, FoodWater.WaterLeft, FoodWater.WaterDrunk, FoodWater.Famine, FoodWater.Thirst },
            food.ConvertAll(s => s.Key));
        CollectionAssert.Contains(sheep.Eats, FoodWater.GrassLeft, "the sheep line is linked to the real grass");
        CollectionAssert.DoesNotContain(leopard.Eats, FoodWater.GrassLeft);
        Assert.IsTrue(food.Find(s => s.Key == FoodWater.GrassEaten).Quiet, "eaten per day wobbles: no notes");
        Assert.IsFalse(food.Find(s => s.Key == FoodWater.Thirst).Visible, "thirst starts hidden (six lines are a lot)");
    }

    [Test]
    public void Thin_KeepsFirstAndLast_AndAtMostMax()
    {
        var t = PopulationGraphMath.Thin(1000, PopulationGraph.MaxPoints);
        Assert.LessOrEqual(t.Count, PopulationGraph.MaxPoints);
        Assert.AreEqual(0, t[0]);
        Assert.AreEqual(999, t[t.Count - 1]);
        for (int i = 1; i < t.Count; i++) Assert.Greater(t[i], t[i - 1]);
        Assert.AreEqual(5, PopulationGraphMath.Thin(5, 240).Count);
        Assert.AreEqual(0, PopulationGraphMath.Thin(0, 240).Count);
    }

    [Test]
    public void Smooth_GoesThroughEveryPoint_WithoutBumpsPastThem()
    {
        var pts = new List<Vector2> { new(0, 0), new(10, 0), new(20, 100), new(30, 0), new(40, 0) };
        var curve = PopulationGraphMath.Smooth(pts, 4, 0f, 100f);
        foreach (var p in pts) Assert.IsTrue(curve.Exists(c => (c - p).sqrMagnitude < 1e-4f), $"passes through {p}");
        foreach (var c in curve) Assert.That(c.y, Is.InRange(0f, 100f), "never dips below zero or over the top");
        for (int i = 0; i < curve.Count; i++)
            if (curve[i].x <= 10f) Assert.AreEqual(0f, curve[i].y, 1e-4f, "flat before the spike stays flat");
    }

    // ---------- Recording ----------

    [Test]
    public void Recorder_LetsListenersAddValuesToTheSameDay()
    {
        var (sim, rec) = MakeSim();
        rec.Recording += v => v["Extra"] = 42f;
        sim.Tick();
        Assert.AreEqual(42f, rec.History.Latest.Get("Extra"));
        Assert.AreEqual(10f, rec.History.Latest.Get("BlueSheep"), "the normal values are still there");
    }

    [Test]
    public void Patch_ReportsWhatWasEaten()
    {
        var patch = MakePatch("Grass", 100f);
        var got = new List<float>();
        void On(ResourcePatch p, float a) { if (p == patch) got.Add(a); }
        ResourcePatch.Consumed += On;
        try
        {
            patch.Consume(30f);
            patch.Consume(100f);   // only 70 left
            patch.Consume(5f);     // empty: nothing taken, nothing reported
        }
        finally { ResourcePatch.Consumed -= On; }
        CollectionAssert.AreEqual(new[] { 30f, 70f }, got);
        Assert.IsFalse(patch.IsWaterPatch);
        Assert.AreEqual(100f, patch.Capacity);
    }

    [Test]
    public void Patch_BurnedByFire_EmptiesButIsNotCountedAsEaten()
    {
        var patch = MakePatch("Grass", 100f);
        int reports = 0;
        void On(ResourcePatch p, float a) { if (p == patch) reports++; }
        ResourcePatch.Consumed += On;
        try
        {
            Assert.AreEqual(40f, patch.Remove(40f));   // a grass fire / the demo key
            Assert.AreEqual(0f, patch.Remove(-10f), "never adds grass");
        }
        finally { ResourcePatch.Consumed -= On; }
        Assert.AreEqual(60f, patch.Amount, 0.001f, "the grass is gone (Grass left drops)");
        Assert.AreEqual(0, reports, "but no animal ate it (Grass eaten stays the same)");
    }

    [Test]
    public void Meter_RecordsGrassAndWaterLeft_EatenAndDrunk_EveryDay()
    {
        var (sim, rec) = MakeSim();
        var grass1 = MakePatch("Grass", 100f);
        MakePatch("Grass", 100f);
        var water = MakePatch("Water", 100f);
        var meter = rec.gameObject.AddComponent<ResourceUseMeter>();
        meter.recorder = rec;
        Call(meter, "OnEnable");   // edit mode: Unity doesn't call it
        try
        {
            grass1.Consume(50f);   // half a patch of two
            water.Consume(25f);
            sim.Tick();
            var s = rec.History.Latest;
            Assert.AreEqual(75f, s.Get(FoodWater.GrassLeft), 1e-3f, "average fullness of the grass patches");
            Assert.AreEqual(75f, s.Get(FoodWater.WaterLeft), 1e-3f);
            Assert.AreEqual(25f, s.Get(FoodWater.GrassEaten), 1e-3f, "half a patch of the two = 25 % of the grass");
            Assert.AreEqual(25f, s.Get(FoodWater.WaterDrunk), 1e-3f);
            Assert.AreEqual(0.5f, s.Get(FoodWater.GrassEatenTotal), 1e-3f, "in full patches");
            Assert.AreEqual(0.5f, meter.GrassEatenTotal, 1e-3f);
            Assert.IsFalse(s.Has(FoodWater.Famine), "no animals: no famine level to show");

            // Reset (Free Exploration's Reset button): the totals start again with the graph.
            sim.ResetEcosystem();
            Assert.AreEqual(0f, meter.GrassEatenTotal);
            Assert.AreEqual(0f, rec.History.Latest.Get(FoodWater.GrassEatenTotal));
        }
        finally { Call(meter, "OnDisable"); }
    }

    // ---------- Notes ----------

    [Test]
    public void Notes_FamineAlert_PointsAtTheGrass_AndMissingFirstDaysAreSkipped()
    {
        var h = new PopulationHistory();
        var sheep = new PopulationSeries { Key = "BlueSheep", Label = "Blue Sheep", Eats = new List<string> { "Grass" }, Visible = true };
        var food = FoodWater.BuildSeries(new[] { sheep });
        // Day 0 has no Food & Water values (recorded before the meter was listening).
        h.Add(0, new Dictionary<string, float> { ["BlueSheep"] = 10 });
        float[] grass = { 100, 90, 60, 25, 20, 20 };
        float[] famine = { 10, 20, 40, 60, 85, 90 };
        for (int i = 0; i < grass.Length; i++)
            h.Add((i + 1) * 10, new Dictionary<string, float> { ["BlueSheep"] = 10, [FoodWater.GrassLeft] = grass[i], [FoodWater.Famine] = famine[i], [FoodWater.GrassEaten] = 30f + i * 7 % 20 });
        var all = new List<PopulationSeries> { sheep };
        all.AddRange(food);

        var notes = ObservationNotes.Find(h, all);
        var texts = notes.ConvertAll(n => n.Text);
        Assert.IsTrue(texts.Exists(t => t.StartsWith("The Grass is running out")), string.Join(" | ", texts));
        var alert = notes.Find(n => n.Text.StartsWith("Famine!"));
        Assert.IsNotNull(alert, string.Join(" | ", texts));
        StringAssert.Contains("Look at the Grass line", alert.Text, "the famine note links to the grass running out");
        Assert.AreEqual(5, alert.Index, "on the day it reached 80 %");
        Assert.AreEqual(50, alert.Tick);
        Assert.IsFalse(texts.Exists(t => t.Contains("grew back")), "the missing first day is not read as 0 %");
        Assert.IsFalse(texts.Exists(t => t.Contains("Grass eaten")), "quiet lines get no notes");
    }

    [Test]
    public void Track_WithTheMeter_SplitsAnimalsFromFoodAndWater()
    {
        var (sim, rec) = MakeSim(withGrassResource: true);
        rec.History.Add(1, new Dictionary<string, float> { ["BlueSheep"] = 9, ["Grass"] = 50, [FoodWater.GrassLeft] = 50, [FoodWater.Famine] = 30 });
        FieldJournalObservations.Track(rec, null, sim, foodAndWater: true);
        CollectionAssert.AreEqual(new[] { "BlueSheep" }, FieldJournalObservations.Series.ConvertAll(s => s.Key),
            "the Animals view: no Food & Water keys and no duplicate Grass line");
        Assert.AreEqual(6, FieldJournalObservations.FoodSeries.Count);
        Assert.AreEqual(7, FieldJournalObservations.AllSeries.Count);

        FieldJournalObservations.Track(rec, null, sim);
        Assert.IsNull(FieldJournalObservations.FoodSeries, "no meter: no Food & Water view");
        Assert.IsFalse(FieldJournalObservations.Series.Exists(s => FoodWater.IsKey(s.Key)), "stray meter keys never become animal lines");
    }

    // ---------- Drawing ----------

    [Test]
    public void Graph_DrawsFoodAndWater_InPercent_WithABubblePerLine()
    {
        var canvas = Go("Canvas", typeof(Canvas));
        var box = Go("Box", typeof(RectTransform));
        box.transform.SetParent(canvas.transform, false);
        ((RectTransform)box.transform).sizeDelta = new Vector2(1200f, 800f);
        var h = new PopulationHistory();
        h.Add(0, new Dictionary<string, float> { ["BlueSheep"] = 10 });   // no food values on day 0
        for (int i = 1; i <= 300; i++)
            h.Add(i, new Dictionary<string, float> { [FoodWater.GrassLeft] = 100 - i * 0.2f, [FoodWater.WaterLeft] = 80, [FoodWater.GrassEaten] = 12,
                                                     [FoodWater.WaterDrunk] = 9, [FoodWater.Famine] = i * 0.2f, [FoodWater.Thirst] = 30 });
        var graph = PopulationGraph.Create(box.transform);
        graph.valueSuffix = "%";
        graph.minAxisMax = 100f;
        var food = FoodWater.BuildSeries();
        graph.Bind(h, food);
        Call(graph, "Redraw");

        int visible = food.FindAll(s => s.Visible).Count;
        int bubbles = 0;
        foreach (Transform t in graph.transform)
            if (t.name.StartsWith("Bubble") && t.gameObject.activeSelf)
            {
                bubbles++;
                StringAssert.EndsWith("%", t.GetComponentInChildren<TMP_Text>().text);
            }
        Assert.AreEqual(visible, bubbles, "one value bubble per line shown");
        var top = graph.transform.Find("Y5");
        Assert.IsNotNull(top);
        Assert.AreEqual("100%", top.GetComponent<TMP_Text>().text, "percent axis up to 100 %");
        Assert.AreEqual(6, graph.transform.Find("Legend").childCount, "a chip per line (two rows)");

        graph.SetVisible(FoodWater.Famine, false);
        Call(graph, "Redraw");
        int after = 0;
        foreach (Transform t in graph.transform) if (t.name.StartsWith("Bubble") && t.gameObject.activeSelf) after++;
        Assert.AreEqual(visible - 1, after, "hiding a line hides its bubble");
    }

    [Test]
    public void Graph_Bubble_ShowsWhereItsLineEnds_WhenTodayHasNoValue()
    {
        var canvas = Go("Canvas", typeof(Canvas));
        var box = Go("Box", typeof(RectTransform));
        box.transform.SetParent(canvas.transform, false);
        ((RectTransform)box.transform).sizeDelta = new Vector2(1200f, 800f);
        var h = new PopulationHistory();
        h.Add(0, new Dictionary<string, float> { [FoodWater.GrassLeft] = 90, [FoodWater.Famine] = 40 });
        h.Add(1, new Dictionary<string, float> { [FoodWater.GrassLeft] = 70, [FoodWater.Famine] = 65 });
        // Every animal is gone: no famine level today, so the famine line stops at 65 %.
        h.Add(2, new Dictionary<string, float> { [FoodWater.GrassLeft] = 80, [FoodWater.Animals] = 0 });
        var food = FoodWater.BuildSeries();
        food.RemoveAll(s => s.Key != FoodWater.GrassLeft && s.Key != FoodWater.Famine);
        var graph = PopulationGraph.Create(box.transform);
        graph.valueSuffix = "%";
        graph.Bind(h, food);
        Call(graph, "Redraw");

        var texts = new Dictionary<Color, string>();
        foreach (Transform t in graph.transform)
            if (t.name.StartsWith("Bubble") && t.gameObject.activeSelf)
                texts[t.GetComponent<UnityEngine.UI.Image>().color] = t.GetComponentInChildren<TMP_Text>().text;
        Assert.AreEqual("80%", texts[FoodWater.GrassColor]);
        Assert.AreEqual("65%", texts[FoodWater.FamineColor], "not 0 % for a value that wasn't recorded");
    }

    // ---------- Helpers ----------

    (EcosystemSimulator, PopulationHistoryRecorder) MakeSim(bool withGrassResource = false)
    {
        var saved = new Dictionary<string, float>(EcosystemSimulator.StartingPopulationOverrides);
        EcosystemSimulator.StartingPopulationOverrides.Clear();
        try
        {
            var sheep = Make<SpeciesDefinition>("BlueSheep"); sheep.startingPopulation = 10;
            var go = Go("Sim");
            var sim = go.AddComponent<EcosystemSimulator>();
            sim.mode = EcosystemSimulator.SimulationMode.Events;
            sim.species = new List<SpeciesDefinition> { sheep };
            sim.resources = new List<ResourceDefinition>();
            if (withGrassResource) sim.resources.Add(Make<ResourceDefinition>("Grass"));
            sim.ResetEcosystem();
            var rec = go.AddComponent<PopulationHistoryRecorder>();
            rec.Bind(sim);
            return (sim, rec);
        }
        finally
        {
            foreach (var kv in saved) EcosystemSimulator.StartingPopulationOverrides[kv.Key] = kv.Value;
        }
    }

    ResourcePatch MakePatch(string resourceName, float max)
    {
        var def = Make<ResourceDefinition>(resourceName);
        def.maxAmount = max;
        var patch = Go(resourceName + " Patch").AddComponent<ResourcePatch>();
        Call(patch, "Awake");   // edit mode: Unity doesn't call it
        patch.Configure(def);
        ResourcePatch.All.Add(patch);
        return patch;
    }
}
