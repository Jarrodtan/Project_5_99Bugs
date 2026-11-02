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
}
