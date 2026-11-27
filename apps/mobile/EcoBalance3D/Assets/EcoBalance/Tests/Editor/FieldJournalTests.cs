using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Field Journal logic that doesn't need a scene: case selection, the NEW tag, graph lines and axis.
public class FieldJournalTests
{
    readonly List<Object> created = new();

    T Make<T>(string name) where T : ScriptableObject
    {
        var o = ScriptableObject.CreateInstance<T>();
        o.name = name;
        created.Add(o);
        return o;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
        foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        objects.Clear();
    }

    InvestigationCase Case(string id)
    {
        var c = Make<InvestigationCase>("Case_" + id);
        c.caseId = id;
        return c;
    }

    [TestCase(0f, 1f, 5)]
    [TestCase(1f, 1f, 1)]
    [TestCase(3f, 1f, 3)]
    [TestCase(10f, 2f, 5)]
    [TestCase(17f, 5f, 4)]
    [TestCase(200f, 50f, 4)]
    public void Scale_UsesWholeNumberStepsThatReachTheMax(float max, float step, int divisions)
    {
        var (s, d) = PopulationGraphMath.Scale(max);
        Assert.AreEqual(step, s, 1e-4f);
        Assert.AreEqual(divisions, d);
        Assert.GreaterOrEqual(s * d, max);
    }

    [TestCase("BlueSheep", "Blue Sheep")]
    [TestCase("Snow_Leopard", "Snow Leopard")]
    [TestCase("Grass", "Grass")]
    public void Pretty_SplitsAssetNames(string raw, string expected) =>
        Assert.AreEqual(expected, PopulationGraphMath.Pretty(raw));

    [Test]
    public void BuildSeries_SpeciesShownFirst_ResourcesHidden_UnknownKeysKept()
    {
        var sheep = Make<SpeciesDefinition>("BlueSheep"); sheep.speciesName = "BlueSheep";
        var leopard = Make<SpeciesDefinition>("SnowLeopard"); leopard.speciesName = "";
        var grass = Make<ResourceDefinition>("Grass"); grass.resourceName = "Grass";

        var series = PopulationGraphMath.BuildSeries(new[] { "Grass", "Mystery", "SnowLeopard", "BlueSheep" },
            new[] { sheep, leopard }, new[] { grass });

        CollectionAssert.AreEqual(new[] { "BlueSheep", "SnowLeopard", "Grass", "Mystery" }, series.ConvertAll(s => s.Key));
        Assert.AreEqual("Blue Sheep", series[0].Label);
        Assert.AreEqual("Snow Leopard", series[1].Label, "falls back to the asset name");
        Assert.IsTrue(series[0].Visible && series[1].Visible);
        Assert.IsTrue(series[2].IsResource);
        Assert.IsFalse(series[2].Visible, "resources start hidden");
        Assert.AreNotEqual(series[0].Color, series[1].Color);
    }

    [Test]
    public void SeenTracker_NewUntilRead_SurvivesReload_AndResetsWithJournal()
    {
        var journal = new CaseJournal(new MemoryCaseJournalStore());
        var store = new MemoryCaseJournalStore();
        var seen = new CaseSeenTracker(store);

        Assert.IsFalse(seen.IsNew(journal, "a"), "locked cases are never NEW");
        journal.Unlock("a");
        Assert.IsTrue(seen.IsNew(journal, "a"));
        seen.MarkSeen("a");
        Assert.IsFalse(seen.IsNew(journal, "a"));
        Assert.IsFalse(new CaseSeenTracker(store).IsNew(journal, "a"), "saved");

        journal.ResetAll();
        seen.Prune(journal);
        journal.Unlock("a");
        Assert.IsTrue(new CaseSeenTracker(store).IsNew(journal, "a"), "unlocking again after a reset is NEW again");
    }

    [Test]
    public void PickInitial_PrefersRequested_ThenNew_ThenUnlocked_ThenFirst()
    {
        var journal = new CaseJournal(new MemoryCaseJournalStore());
        var seen = new CaseSeenTracker(new MemoryCaseJournalStore());
        var cases = new FieldJournalCases(new[] { Case("one"), Case("two"), Case("three") }, journal);

        Assert.AreEqual(0, cases.PickInitial(seen), "nothing unlocked: first case");
        journal.Unlock("two");
        seen.MarkSeen("two");
        Assert.AreEqual(1, cases.PickInitial(seen), "first unlocked");
        journal.Unlock("three");
        Assert.AreEqual(2, cases.PickInitial(seen), "unread beats read");
        Assert.AreEqual(0, cases.PickInitial(seen, "one"), "requested case wins");
        Assert.AreEqual(2, cases.PickInitial(seen, "missing"), "unknown request is ignored");
        Assert.AreEqual(2, cases.UnlockedCount);
        Assert.AreEqual(-1, new FieldJournalCases(null, journal).PickInitial(seen));
    }

    readonly List<GameObject> objects = new();

    GameObject Go(string name, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        objects.Add(go);
        return go;
    }

    [Test]
    public void GraphLines_GetACanvasRenderer_SoTheyAreDrawn()
    {
        // uGUI 2 stopped adding a CanvasRenderer for custom Graphics: the graph was empty without one.
        var lines = Go("Plot", typeof(RectTransform)).AddComponent<PopulationGraphLines>();
        Assert.IsNotNull(lines.GetComponent<CanvasRenderer>());
    }

    [Test]
    public void GraphHistory_FollowsCountsGoingUpAndDown()
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
            sim.ResetEcosystem();
            var rec = go.AddComponent<PopulationHistoryRecorder>();
            rec.Bind(sim);

            Assert.AreEqual(3, sim.RecordBirth(sheep, 3));
            sim.Tick();
            Assert.AreEqual(13f, rec.History.Latest.Get("BlueSheep"), "births show on the next day");
            for (int i = 0; i < 5; i++) sim.RecordStarvation(sheep);
            sim.Tick();
            Assert.AreEqual(8f, rec.History.Latest.Get("BlueSheep"), "deaths show on the next day");
            CollectionAssert.AreEqual(new[] { 10f, 13f, 8f }, rec.History.Series("BlueSheep"));
        }
        finally
        {
            foreach (var kv in saved) EcosystemSimulator.StartingPopulationOverrides[kv.Key] = kv.Value;
        }
    }

    [Test]
    public void SpeedButton_CyclesOneTwoFourEight()
    {
        // The HUD and the Field Journal graph share this, so the graph runs at the speed the HUD shows.
        var speed = Go("Speed").AddComponent<SimulationSpeedController>();
        Assert.AreEqual(2f, speed.CycleSpeed());
        Assert.AreEqual(4f, speed.CycleSpeed());
        Assert.AreEqual(8f, speed.CycleSpeed());
        Assert.AreEqual(1f, speed.CycleSpeed());
        speed.SetSpeed(3f);
        Assert.AreEqual(1f, speed.CycleSpeed(), "an odd speed goes back to 1x");
    }

    [Test]
    public void ButtonCaption_LabelsBlankButtonsOnly()
    {
        var blank = Go("BackBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image)).AddComponent<UnityEngine.UI.Button>();
        var label = ButtonCaption.Ensure(blank, "‹ Back");
        Assert.IsNotNull(label);
        Assert.AreEqual("‹ Back", label.text);
        Assert.AreSame(label, ButtonCaption.Ensure(blank, "‹ Back"), "a second call reuses the label");

        var labelled = Go("StartBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image)).AddComponent<UnityEngine.UI.Button>();
        Go("Text", typeof(RectTransform)).transform.SetParent(labelled.transform, false);
        labelled.transform.GetChild(0).gameObject.AddComponent<TMPro.TextMeshProUGUI>().text = "Start";
        Assert.IsNull(ButtonCaption.Ensure(labelled, "x"), "buttons with their own text are left alone");
    }

    // ---------- Observation notes ----------

    static PopulationHistory History(params (float sheep, float leopards, float grass)[] days)
    {
        var h = new PopulationHistory(100);
        for (int i = 0; i < days.Length; i++)
            h.Add(i, new Dictionary<string, float> { { "BlueSheep", days[i].sheep }, { "SnowLeopard", days[i].leopards }, { "Grass", days[i].grass } });
        return h;
    }

    static List<PopulationSeries> FoodChain() => new()
    {
        new PopulationSeries { Key = "BlueSheep", Label = "Blue Sheep", Visible = true, Eats = new List<string> { "Grass" } },
        new PopulationSeries { Key = "SnowLeopard", Label = "Snow Leopard", Visible = true, Eats = new List<string> { "BlueSheep" } },
        new PopulationSeries { Key = "Grass", Label = "Grass", IsResource = true },
    };

    [Test]
    public void Notes_SmallWobblesAreIgnored()
    {
        var h = History((10, 2, 100), (11, 2, 95), (9, 2, 90), (10, 2, 92));
        CollectionAssert.IsEmpty(ObservationNotes.Find(h, FoodChain()));
    }

    [Test]
    public void Notes_BiggestRiseAndFall_InTheOrderTheyHappened()
    {
        var h = History((10, 2, 100), (16, 2, 100), (20, 2, 100), (12, 2, 100), (4, 2, 100), (6, 2, 100));
        var notes = ObservationNotes.Find(h, FoodChain());
        Assert.AreEqual(2, notes.Count);
        Assert.AreEqual(2, notes[0].Index);
        StringAssert.StartsWith("Blue Sheep reached 20", notes[0].Text);
        Assert.AreEqual(4, notes[1].Index);
        StringAssert.StartsWith("Blue Sheep fell to 4", notes[1].Text);
    }

    [Test]
    public void Notes_DyingOut_AndResourceRunningOutThenGrowingBack()
    {
        var h = History((10, 2, 100), (10, 1, 50), (10, 0, 20), (10, 0, 40), (10, 0, 70));
        var notes = ObservationNotes.Find(h, FoodChain()).ConvertAll(n => n.Text);
        Assert.IsTrue(notes.Exists(t => t.StartsWith("The last Snow Leopard is gone.")), string.Join("\n", notes));
        Assert.IsTrue(notes.Exists(t => t.StartsWith("The Grass is running out.")), string.Join("\n", notes));
        Assert.IsTrue(notes.Exists(t => t.StartsWith("The Grass grew back.")), string.Join("\n", notes));
    }

    [Test]
    public void Notes_PointAtTheFoodChainLine_InsteadOfGivingTheAnswer()
    {
        // Grass runs out first, then the sheep fall: the sheep note asks the player to look at the grass.
        var h = History((10, 2, 100), (10, 2, 25), (4, 2, 20));
        var sheepFell = ObservationNotes.Find(h, FoodChain()).Find(n => n.Text.StartsWith("Blue Sheep fell"));
        Assert.IsNotNull(sheepFell);
        StringAssert.Contains("Look at the Grass line", sheepFell.Text);

        // Many leopards first, then the sheep fall: it points at the hunters.
        h = History((10, 2, 100), (10, 6, 100), (4, 6, 100));
        sheepFell = ObservationNotes.Find(h, FoodChain()).Find(n => n.Text.StartsWith("Blue Sheep fell"));
        StringAssert.Contains("Look at the Snow Leopard line", sheepFell.Text);

        // Nothing related changed before: no prompt.
        h = History((10, 2, 100), (4, 2, 100));
        sheepFell = ObservationNotes.Find(h, FoodChain()).Find(n => n.Text.StartsWith("Blue Sheep fell"));
        StringAssert.DoesNotContain("Look at", sheepFell.Text);
    }

    [Test]
    public void Notes_CasesFoundArePinnedToTheirDay_OldOnesSkipped()
    {
        var h = new PopulationHistory(3);
        for (int t = 0; t < 5; t++) h.Add(t * 10, new Dictionary<string, float> { { "BlueSheep", 10 } });   // keeps ticks 20, 30, 40
        var notes = ObservationNotes.Find(h, FoodChain(), new[] { (30, "Too Many Sheep"), (0, "Dropped") });
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(1, notes[0].Index);
        Assert.AreEqual(30, notes[0].Tick);
        Assert.AreEqual("Case found: Too Many Sheep!", notes[0].Text);
    }

    [Test]
    public void BuildSeries_RemembersWhatEachSpeciesEats()
    {
        var grass = Make<ResourceDefinition>("Grass");
        var sheep = Make<SpeciesDefinition>("BlueSheep");
        var leopard = Make<SpeciesDefinition>("SnowLeopard");
        sheep.SetRelationship(RelationshipType.Eats, grass);
        leopard.SetRelationship(RelationshipType.Eats, sheep);

        var series = PopulationGraphMath.BuildSeries(new[] { "BlueSheep", "SnowLeopard", "Grass" }, new[] { sheep, leopard }, new[] { grass });
        CollectionAssert.AreEqual(new[] { "Grass" }, series[0].Eats);
        CollectionAssert.AreEqual(new[] { "BlueSheep" }, series[1].Eats);
        CollectionAssert.IsEmpty(series[2].Eats);
    }
}
