// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NUnit.Framework;

// Performance tests (rubric: "performance tests with a historical representation"). Times the
// simulation-core hot paths with fixed inputs. Every CI run prints the numbers in the log; the
// nightly run also passes -perfOut <file>, and CI appends that row to the history kept on Gitea,
// so slowdowns show up as a jump in the history. Budgets are generous: they only fail on a
// real regression (e.g. an accidental O(n^2) loop), not on a slow CI machine.
public class PerformanceTests
{
    static readonly List<(string name, double ms)> Results = new();

    static double Time(Action a, int repeats)
    {
        a(); // warm-up (JIT)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < repeats; i++) a();
        return sw.Elapsed.TotalMilliseconds / repeats;
    }

    static void Record(string name, double ms, double budgetMs)
    {
        Results.Add((name, ms));
        UnityEngine.Debug.Log($"[Perf] {name}: {ms:0.000} ms (budget {budgetMs} ms)");
        Assert.Less(ms, budgetMs, $"{name} took {ms:0.0} ms, budget {budgetMs} ms");
    }

    [Test]
    public void Perf_BalanceModel_400Days()
    {
        var p = new BalanceParameters { days = 400, seed = 1 };
        Record("balance_model_400_days", Time(() => BalanceModel.Run(p), 5), budgetMs: 2000);
    }

    [Test]
    public void Perf_PopulationHistory_Add10k()
    {
        Record("history_add_10k", Time(() =>
        {
            var h = new PopulationHistory(1000);
            for (int i = 0; i < 10000; i++)
                h.Add(i, new Dictionary<string, float> { ["BlueSheep"] = i, ["SnowLeopard"] = i, ["Grass"] = i });
        }, 5), budgetMs: 500);
    }

    [Test]
    public void Perf_CaseEvaluation_1000Days()
    {
        var h = new PopulationHistory(1000);
        for (int i = 0; i < 1000; i++)
            h.Add(i, new Dictionary<string, float> { ["BlueSheep"] = 1000 - i, ["SnowLeopard"] = i % 9, ["Grass"] = i % 100 });
        var sheep = UnityEngine.ScriptableObject.CreateInstance<SpeciesDefinition>();
        sheep.name = "BlueSheep";
        var cond = new CaseCondition { kind = CaseConditionKind.DroppedByFraction, subject = sheep, value = 0.5f, windowDays = 15 };
        try
        {
            Record("case_eval_10k_checks", Time(() =>
            {
                for (int i = 0; i < 10000; i++) CaseEvaluator.IsMet(cond, h);
            }, 3), budgetMs: 500);
        }
        finally { UnityEngine.Object.DestroyImmediate(sheep); }
    }

    // SCRUM-266: one frame of every animal asking for its nearest prey/predator, 20 vs 500
    // animals. With the spatial grid 500 should stay far below the O(n^2) plain-scan cost.
    [TestCase(20)]
    [TestCase(500)]
    public void Perf_EcoRegistry_NearestPerFrame(int animals)
    {
        EcoRegistry.Clear();
        var sheep = UnityEngine.ScriptableObject.CreateInstance<SpeciesDefinition>();
        var objects = new List<UnityEngine.GameObject>();
        var rng = new Random(1);
        try
        {
            for (int i = 0; i < animals; i++)
            {
                var go = new UnityEngine.GameObject();
                go.transform.position = new UnityEngine.Vector3((float)rng.NextDouble() * 200f - 100f, 0f, (float)rng.NextDouble() * 200f - 100f);
                objects.Add(go);
                EcoRegistry.Register(go.transform, sheep);
            }
            Record($"registry_nearest_{animals}_animals", Time(() =>
            {
                EcoRegistry.InvalidateGrids(); // new frame: animals have moved
                foreach (var go in objects)
                    EcoRegistry.Nearest(go.transform.position, sheep, 12f, go.transform);
            }, 10), budgetMs: 50);
        }
        finally
        {
            EcoRegistry.Clear();
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(sheep);
        }
    }

    // Writes the metrics (metric=ms,...) when Unity was started with -perfOut <path>;
    // CI prefixes the date and commit and appends the row to the history on Gitea.
    [OneTimeTearDown]
    public void WriteHistoryRow()
    {
        string path = ArgValue("-perfOut");
        if (path == null || Results.Count == 0) return;
        var cols = new List<string>();
        foreach (var (name, ms) in Results) cols.Add($"{name}={ms.ToString("0.000", CultureInfo.InvariantCulture)}");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, string.Join(",", cols) + "\n");
    }

    static string ArgValue(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
// [AI-GENERATED END]
