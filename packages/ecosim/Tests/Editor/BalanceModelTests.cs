// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using NUnit.Framework;
using UnityEngine;

// EditMode tests for the headless BalanceModel. They pin down behaviour that must hold for any
// tuning (determinism, starvation timing, caps), plus what design's M2 numbers currently do.
public class BalanceModelTests
{
    [Test]
    public void SameSeed_GivesIdenticalHistory()
    {
        var a = BalanceModel.Run(new BalanceParameters { seed = 7 });
        var b = BalanceModel.Run(new BalanceParameters { seed = 7 });
        Assert.AreEqual(BalanceModel.ToCsv(a), BalanceModel.ToCsv(b));
    }

    [Test]
    public void DifferentSeed_GivesDifferentHistory()
    {
        var a = BalanceModel.Run(new BalanceParameters { seed = 1 });
        var b = BalanceModel.Run(new BalanceParameters { seed = 2 });
        Assert.AreNotEqual(BalanceModel.ToCsv(a), BalanceModel.ToCsv(b));
    }

    [Test]
    public void LeopardsWithNoSheep_StarveOnSchedule()
    {
        // Fullness 100 -12/day hits 0 on day 9; then -20 HP/day from 100 HP: dead on day 13.
        // One leopard, so no pair can breed and add fresh, fully-fed cubs.
        var h = BalanceModel.Run(new BalanceParameters { sheepStart = 0, leopardStart = 1, days = 20 });
        Assert.AreEqual(1, h[11].Leopards, "still alive on day 12");
        Assert.AreEqual(0, h[12].Leopards, "all starved on day 13");
    }

    [Test]
    public void SheepWithNoLeopards_SurviveButAreLimitedByGrass()
    {
        var p = new BalanceParameters { leopardStart = 0, days = 300 };
        var s = new BalanceModel.Summary(BalanceModel.Run(p));
        Assert.Less(s.SheepExtinctDay, 0, "grass alone should keep some sheep alive");
        Assert.Less(s.SheepMax, p.maxPopulationPerSpecies, "grass, not the cap, should limit sheep");
    }

    [Test]
    public void Populations_NeverExceedCap()
    {
        var p = new BalanceParameters { leopardStart = 0, grassPatches = 500, maxPopulationPerSpecies = 30, days = 100 };
        foreach (var d in BalanceModel.Run(p))
            Assert.LessOrEqual(d.Sheep, 30);
    }

    [Test]
    public void Grass_StaysWithinBounds()
    {
        foreach (var d in BalanceModel.Run(new BalanceParameters { sheepStart = 40, leopardStart = 0 }))
            Assert.That(d.GrassAverage, Is.InRange(0f, 100f));
    }

    // Records the finding behind the M2 tuning task: with design's doc values the sheep boom,
    // overgraze, then leopards wipe them out and starve. Update when design's numbers change.
    [Test]
    public void DesignDocValues_CrashWithin200Days()
    {
        for (int seed = 1; seed <= 5; seed++)
        {
            var s = new BalanceModel.Summary(BalanceModel.Run(new BalanceParameters { seed = seed }));
            Debug.Log($"[Balance] doc values, seed {seed}: {s}");
            Assert.IsFalse(s.BothSurvived, $"seed {seed} unexpectedly stable: {s}");
        }
    }

    // Candidate tuning from the prototype sweep: slower leopard breeding, harder-to-find prey,
    // lower hunt success and slower leopard hunger. Logs results; asserts it outlasts the doc values.
    [Test]
    public void CandidateTuning_OutlastsDesignDocValues()
    {
        int docDays = 0, tunedDays = 0;
        for (int seed = 1; seed <= 5; seed++)
        {
            var doc = new BalanceModel.Summary(BalanceModel.Run(new BalanceParameters { seed = seed, days = 400 }));
            var tuned = new BalanceModel.Summary(BalanceModel.Run(new BalanceParameters
            {
                seed = seed, days = 400, leopardBreedEveryDays = 20, encounterScale = 40f,
                huntSuccess = 0.4f, leopardHungerPerDay = 8f,
            }));
            Debug.Log($"[Balance] tuned, seed {seed}: {tuned}");
            docDays += FirstExtinction(doc, 400);
            tunedDays += FirstExtinction(tuned, 400);
        }
        Assert.Greater(tunedDays, docDays);
    }

    static int FirstExtinction(BalanceModel.Summary s, int days)
    {
        int a = s.SheepExtinctDay < 0 ? days : s.SheepExtinctDay;
        int b = s.LeopardExtinctDay < 0 ? days : s.LeopardExtinctDay;
        return Mathf.Min(a, b);
    }
}
// [AI-GENERATED END]
