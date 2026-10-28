// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// EditMode tests for Free Exploration investigation cases: each condition kind, combining
// conditions, the journal, the detector, and that example cases are reachable in the balance model.
public class CaseDetectorTests
{
    readonly List<Object> created = new();
    SpeciesDefinition sheep, leopard;
    ResourceDefinition grass;

    [SetUp]
    public void SetUp()
    {
        sheep = Make<SpeciesDefinition>("BlueSheep");
        leopard = Make<SpeciesDefinition>("SnowLeopard");
        grass = Make<ResourceDefinition>("Grass");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    T Make<T>(string assetName) where T : ScriptableObject
    {
        var o = ScriptableObject.CreateInstance<T>();
        o.name = assetName;
        created.Add(o);
        return o;
    }

    InvestigationCase Case(string id, params CaseCondition[] conds)
    {
        var c = Make<InvestigationCase>(id);
        c.caseId = id;
        c.earliestDay = 0;
        c.conditions = new List<CaseCondition>(conds);
        return c;
    }

    static CaseCondition Cond(CaseConditionKind kind, ScriptableObject subject, float value, int window = 10, ScriptableObject other = null)
        => new CaseCondition { kind = kind, subject = subject, value = value, windowDays = window, other = other };

    // Days 0..n with the given sheep / leopard / grass values.
    static PopulationHistory History(float[] sheepByDay, float[] leoByDay = null, float[] grassByDay = null)
    {
        var h = new PopulationHistory(100);
        for (int d = 0; d < sheepByDay.Length; d++)
            h.Add(d, new Dictionary<string, float>
            {
                ["BlueSheep"] = sheepByDay[d],
                ["SnowLeopard"] = leoByDay?[d] ?? 0f,
                ["Grass"] = grassByDay?[d] ?? 100f,
            });
        return h;
    }

    [Test]
    public void AtMost_AtLeast_ReachedZero()
    {
        var h = History(new[] { 10f, 4f }, new[] { 2f, 7f }, new[] { 100f, 0f });
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.AtMost, sheep, 5), h));
        Assert.IsFalse(CaseEvaluator.IsMet(Cond(CaseConditionKind.AtMost, sheep, 3), h));
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.AtLeast, leopard, 6), h));
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.ReachedZero, grass, 0), h));
        Assert.IsFalse(CaseEvaluator.IsMet(Cond(CaseConditionKind.ReachedZero, sheep, 0), h));
    }

    [Test]
    public void DroppedAndRose_CompareWithWindowDaysAgo()
    {
        var h = History(new[] { 20f, 18f, 15f, 9f });
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.DroppedByFraction, sheep, 0.5f, window: 3), h), "20 → 9 is a 55% drop");
        Assert.IsFalse(CaseEvaluator.IsMet(Cond(CaseConditionKind.DroppedByFraction, sheep, 0.5f, window: 1), h), "15 → 9 is only 40%");
        var up = History(new[] { 10f, 12f, 16f });
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.RoseByFraction, sheep, 0.5f, window: 2), up));
        Assert.IsFalse(CaseEvaluator.IsMet(Cond(CaseConditionKind.RoseByFraction, sheep, 0.5f, window: 1), up));
    }

    [Test]
    public void RatioAtLeast_DividesSubjectByOther()
    {
        var h = History(new[] { 10f }, new[] { 4f });
        Assert.IsTrue(CaseEvaluator.IsMet(Cond(CaseConditionKind.RatioAtLeast, leopard, 0.4f, other: sheep), h));
        Assert.IsFalse(CaseEvaluator.IsMet(Cond(CaseConditionKind.RatioAtLeast, leopard, 0.5f, other: sheep), h));
    }

    [Test]
    public void Case_NeedsAllConditionsAndEarliestDay()
    {
        var h = History(new[] { 20f, 8f }, new[] { 3f, 7f });
        var both = Case("predators", Cond(CaseConditionKind.AtMost, sheep, 10), Cond(CaseConditionKind.AtLeast, leopard, 6));
        Assert.IsTrue(CaseEvaluator.IsMet(both, h));
        var oneFails = Case("strict", Cond(CaseConditionKind.AtMost, sheep, 10), Cond(CaseConditionKind.AtLeast, leopard, 9));
        Assert.IsFalse(CaseEvaluator.IsMet(oneFails, h));
        both.earliestDay = 5;
        Assert.IsFalse(CaseEvaluator.IsMet(both, h), "day 1 is before earliestDay 5");
    }

    [Test]
    public void Case_WithNoConditionsNeverUnlocks()
    {
        Assert.IsFalse(CaseEvaluator.IsMet(Case("empty"), History(new[] { 1f })));
    }

    [Test]
    public void Journal_PersistsUnlocksThroughStore()
    {
        var store = new MemoryCaseJournalStore();
        var j = new CaseJournal(store);
        Assert.IsTrue(j.Unlock("a"));
        Assert.IsFalse(j.Unlock("a"), "second unlock is a no-op");
        Assert.IsTrue(new CaseJournal(store).IsUnlocked("a"), "a new journal reads the saved unlock");
        j.ResetAll();
        Assert.IsFalse(new CaseJournal(store).IsUnlocked("a"));
    }

    [Test]
    public void Detector_UnlocksOnNewDayAndRaisesEventOnce()
    {
        var go = new GameObject("Detector"); created.Add(go);
        var det = go.AddComponent<CaseDetector>();
        var store = new MemoryCaseJournalStore();
        det.UseStore(store);
        det.cases = new List<InvestigationCase> { Case("extinct", Cond(CaseConditionKind.ReachedZero, sheep, 0)) };
        var h = new PopulationHistory(10);
        det.Watch(h);
        int events = 0;
        det.CaseUnlocked += _ => events++;

        h.Add(0, new Dictionary<string, float> { ["BlueSheep"] = 3 });
        Assert.AreEqual(0, events);
        h.Add(1, new Dictionary<string, float> { ["BlueSheep"] = 0 });
        Assert.AreEqual(1, events);
        h.Add(2, new Dictionary<string, float> { ["BlueSheep"] = 0 });
        Assert.AreEqual(1, events, "already unlocked: no second event");
        Assert.IsTrue(new CaseJournal(store).IsUnlocked("extinct"));
    }

    // Design-doc values: sheep overgraze, then leopards wipe them out (see BalanceModelTests).
    // These example cases must be discoverable in at least one of 5 runs, or players could never find them.
    [Test]
    public void ExampleCases_AreReachableInBalanceModel()
    {
        var overgrazed = Case("overgrazed", Cond(CaseConditionKind.AtMost, grass, 10), Cond(CaseConditionKind.AtLeast, sheep, 20));
        var predators = Case("too-many-predators",
            Cond(CaseConditionKind.DroppedByFraction, sheep, 0.5f, window: 15), Cond(CaseConditionKind.AtLeast, leopard, 6));
        var extinct = Case("sheep-extinct", Cond(CaseConditionKind.ReachedZero, sheep, 0));

        foreach (var c in new[] { overgrazed, predators, extinct })
        {
            bool reached = false;
            for (int seed = 1; seed <= 5 && !reached; seed++)
            {
                var run = BalanceModel.Run(new BalanceParameters { seed = seed, days = 200 });
                var h = new PopulationHistory(400);
                var full = BalanceModel.ToHistory(run, "BlueSheep", "SnowLeopard", "Grass");
                for (int i = 0; i < full.Count && !reached; i++)
                {
                    var sample = full[i];
                    var values = new Dictionary<string, float>();
                    foreach (var k in sample.Keys) values[k] = sample.Get(k);
                    h.Add(sample.Tick, values);
                    reached = CaseEvaluator.IsMet(c, h);
                }
                if (reached) Debug.Log($"[Cases] '{c.Key}' unlocks on day {h.Latest.Tick} (seed {seed})");
            }
            Assert.IsTrue(reached, $"case '{c.Key}' never unlocked in the balance model");
        }
    }
}
// [AI-GENERATED END]
