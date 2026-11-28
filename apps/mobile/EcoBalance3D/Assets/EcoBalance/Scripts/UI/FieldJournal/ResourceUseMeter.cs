// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Field Journal's "Food &amp; Water" numbers, recorded into the population history on every day sample:
///   - grass and water left (average fullness of the grass / water patches, 0-100 %);
///   - grass eaten and water drunk in the last day (% of all the grass / water there is);
///   - famine level and thirst level (0 = every animal full, 100 = every animal starving / parched), from the
///     animals' Fullness and Hydration (research doc, "Animal needs");
///   - running totals and how many animals are starving / dehydrated right now, for the summary card.
/// Pure rules live here so they are unit-tested; <see cref="ResourceUseMeter"/> measures the running world.
/// </summary>
public static class FoodWater
{
    public const string Prefix = "FoodWater.";
    public const string GrassLeft = Prefix + "GrassLeft", WaterLeft = Prefix + "WaterLeft";
    public const string GrassEaten = Prefix + "GrassEaten", WaterDrunk = Prefix + "WaterDrunk";
    public const string Famine = Prefix + "Famine", Thirst = Prefix + "Thirst";
    public const string GrassEatenTotal = Prefix + "GrassEatenTotal", WaterDrunkTotal = Prefix + "WaterDrunkTotal";
    public const string Starving = Prefix + "Starving", Dehydrated = Prefix + "Dehydrated", Animals = Prefix + "Animals";

    /// <summary>Keys this meter writes (they are not animals, so the Animals view leaves them out).</summary>
    public static bool IsKey(string key) => key != null && key.StartsWith(Prefix);

    // Levels (100 - average Fullness / Hydration). The doc: animals look for food at 50 % Fullness and
    // start losing health at 0 %, so 50 = "hungry" and 80 = most animals close to starving.
    public const float HungryAt = 50f, FamineAt = 80f, BitHungryAt = 25f;

    public static readonly Color GrassColor = new Color32(0x2E, 0x9E, 0x4F, 0xFF);
    public static readonly Color GrassEatenColor = new Color32(0x9B, 0xC9, 0x3A, 0xFF);
    public static readonly Color WaterColor = new Color32(0x1F, 0x8F, 0xE0, 0xFF);
    public static readonly Color WaterDrunkColor = new Color32(0x6F, 0xD0, 0xF2, 0xFF);
    public static readonly Color FamineColor = new Color32(0xE8, 0x4A, 0x3B, 0xFF);
    public static readonly Color ThirstColor = new Color32(0xF2, 0x9A, 0x1F, 0xFF);

    static readonly Color Calm = new Color32(0x2E, 0x9E, 0x4F, 0xFF), Mild = new Color32(0x9B, 0xB0, 0x1E, 0xFF),
                          Warn = new Color32(0xF2, 0x9A, 0x1F, 0xFF), Danger = new Color32(0xE8, 0x4A, 0x3B, 0xFF);

    /// <summary>100 - the average of <paramref name="values"/> (Fullness or Hydration, 0-100). No animals = 0.</summary>
    public static float NeedLevel(IEnumerable<float> values)
    {
        float sum = 0f; int n = 0;
        if (values != null) foreach (var v in values) { sum += Mathf.Clamp(v, 0f, 100f); n++; }
        return n == 0 ? 0f : 100f - sum / n;
    }

    /// <summary>What a famine level means, in words a child reads at a glance, and its colour.</summary>
    public static (string label, Color color) FamineBand(float level) =>
        level >= FamineAt ? ("Famine!", Danger) : level >= HungryAt ? ("Hungry", Warn) : level >= BitHungryAt ? ("A bit hungry", Mild) : ("Well fed", Calm);

    public static (string label, Color color) ThirstBand(float level) =>
        level >= FamineAt ? ("Very thirsty!", Danger) : level >= HungryAt ? ("Thirsty", Warn) : level >= BitHungryAt ? ("A bit thirsty", Mild) : ("Not thirsty", Calm);

    /// <summary>
    /// How much was eaten in the last day: the running total now minus the running total one day ago.
    /// <paramref name="log"/> holds (day, running total), oldest first; before a whole day has passed the
    /// baseline is the start of the run (0).
    /// </summary>
    public static float InLastDay(IReadOnlyList<(float day, float total)> log, float nowDay, float nowTotal)
    {
        float baseline = 0f;
        if (log != null)
            for (int i = log.Count - 1; i >= 0; i--)
                if (log[i].day <= nowDay - 1f) { baseline = log[i].total; break; }
        return Mathf.Max(0f, nowTotal - baseline);
    }

    /// <summary>"3", "2.5", "0.4": patches eaten, one decimal only when it helps.</summary>
    public static string Amount(float patches) =>
        patches >= 10f || Mathf.Abs(patches - Mathf.Round(patches)) < 0.05f ? patches.ToString("0") : patches.ToString("0.0");

    /// <summary>
    /// The Food &amp; Water lines. Each animal line also "eats" the matching food line (Blue Sheep: grass), so the
    /// journal's notes can link "the grass ran out" with the animals and the famine level.
    /// </summary>
    public static List<PopulationSeries> BuildSeries(IEnumerable<PopulationSeries> animals = null)
    {
        if (animals != null)
            foreach (var a in animals)
            {
                if (a == null || a.IsResource) continue;
                bool grass = false, water = false;
                foreach (var e in a.Eats)
                {
                    var k = e.ToLowerInvariant();
                    grass |= k.Contains("grass") || k.Contains("vegetation") || k.Contains("plant");
                    water |= k.Contains("water");
                }
                if (grass && !a.Eats.Contains(GrassLeft)) a.Eats.Add(GrassLeft);
                if (water && !a.Eats.Contains(WaterLeft)) a.Eats.Add(WaterLeft);
            }

        return new List<PopulationSeries>
        {
            new() { Key = GrassLeft, Label = "Grass", Color = GrassColor, IsResource = true, Visible = true },
            new() { Key = GrassEaten, Label = "Grass eaten", Color = GrassEatenColor, Quiet = true, Visible = true },
            new() { Key = WaterLeft, Label = "Water", Color = WaterColor, IsResource = true, Visible = true },
            new() { Key = WaterDrunk, Label = "Water drunk", Color = WaterDrunkColor, Quiet = true, Visible = true },
            new() { Key = Famine, Label = "Famine level", Color = FamineColor, Visible = true, AlertAbove = FamineAt,
                    AlertText = "Famine! Most animals are close to starving.", Eats = new List<string> { GrassLeft } },
            new() { Key = Thirst, Label = "Thirst level", Color = ThirstColor, Visible = false, AlertAbove = FamineAt,
                    AlertText = "The animals are very thirsty!", Eats = new List<string> { WaterLeft } },
        };
    }
}

/// <summary>
/// Measures the running world for <see cref="FoodWater"/>: counts every bite and drink (through
/// <see cref="ResourcePatch.Consumed"/>), and adds the Food &amp; Water values to each population history sample.
/// Added next to the <see cref="PopulationHistoryRecorder"/> by <see cref="FieldJournalInstaller"/>.
/// </summary>
public class ResourceUseMeter : MonoBehaviour
{
    public PopulationHistoryRecorder recorder;

    // Running totals in "full patches": 1 = as much as one full grass patch / water pool holds.
    float grassTotal, waterTotal;
    readonly List<(float day, float total)> grassLog = new(), waterLog = new();
    PopulationHistoryRecorder hooked;
    PopulationHistory hookedHistory;
    ClockService clock;

    public float GrassEatenTotal => grassTotal;
    public float WaterDrunkTotal => waterTotal;

    void OnEnable()
    {
        if (recorder == null) recorder = GetComponent<PopulationHistoryRecorder>();
        clock = FindAnyObjectByType<ClockService>();
        ResourcePatch.Consumed -= OnConsumed;
        ResourcePatch.Consumed += OnConsumed;
        if (recorder != null)
        {
            hooked = recorder;
            hooked.Recording += Record;
            hookedHistory = recorder.History;
            hookedHistory.Cleared += Restart;
        }
    }

    void OnDisable()
    {
        ResourcePatch.Consumed -= OnConsumed;
        if (hooked != null) hooked.Recording -= Record;
        if (hookedHistory != null) hookedHistory.Cleared -= Restart;
        hooked = null; hookedHistory = null;
    }

    // The run was reset (Free Exploration's Reset): its graph and totals start again.
    void Restart()
    {
        grassTotal = waterTotal = 0f;
        grassLog.Clear(); waterLog.Clear();
    }

    void OnConsumed(ResourcePatch patch, float amount)
    {
        if (patch == null || patch.Capacity <= 0f) return;
        if (patch.IsWaterPatch) waterTotal += amount / patch.Capacity;
        else grassTotal += amount / patch.Capacity;
    }

    // The sim clock (follows the speed buttons); looked up again if it didn't exist yet when the meter started.
    float NowDay
    {
        get
        {
            if (clock == null) clock = FindAnyObjectByType<ClockService>();
            return clock != null ? clock.Day + clock.TimeOfDay01 : Time.time / Mathf.Max(1f, ClockService.DaySeconds);
        }
    }

    void Record(Dictionary<string, float> values)
    {
        float day = NowDay;

        int grassPatches = 0, waterPatches = 0;
        float grassFill = 0f, waterFill = 0f;
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.Resource == null || p.Capacity <= 0f) continue;
            float fill = Mathf.Clamp01(p.Amount / p.Capacity);
            if (p.IsWaterPatch) { waterPatches++; waterFill += fill; }
            else { grassPatches++; grassFill += fill; }
        }
        if (grassPatches > 0)
        {
            values[FoodWater.GrassLeft] = grassFill / grassPatches * 100f;
            values[FoodWater.GrassEaten] = FoodWater.InLastDay(grassLog, day, grassTotal) / grassPatches * 100f;
            values[FoodWater.GrassEatenTotal] = grassTotal;
        }
        if (waterPatches > 0)
        {
            values[FoodWater.WaterLeft] = waterFill / waterPatches * 100f;
            values[FoodWater.WaterDrunk] = FoodWater.InLastDay(waterLog, day, waterTotal) / waterPatches * 100f;
            values[FoodWater.WaterDrunkTotal] = waterTotal;
        }
        Remember(grassLog, day, grassTotal);
        Remember(waterLog, day, waterTotal);

        var fullness = new List<float>();
        var hydration = new List<float>();
        int starving = 0, dehydrated = 0;
        foreach (var a in AnimalBrain.All)
        {
            if (a == null || a.IsCaught) continue;
            fullness.Add(a.curr_hunger);
            hydration.Add(a.curr_thirst);
            if (a.curr_hunger <= 0.01f) starving++;
            if (a.curr_thirst <= 0.01f) dehydrated++;
        }
        if (fullness.Count > 0)
        {
            values[FoodWater.Famine] = FoodWater.NeedLevel(fullness);
            values[FoodWater.Thirst] = FoodWater.NeedLevel(hydration);
        }
        values[FoodWater.Starving] = starving;
        values[FoodWater.Dehydrated] = dehydrated;
        values[FoodWater.Animals] = fullness.Count;
    }

    // Keeps a little more than one day of running totals (enough for "in the last day").
    static void Remember(List<(float day, float total)> log, float day, float total)
    {
        log.Add((day, total));
        int drop = 0;
        while (drop + 1 < log.Count && log[drop + 1].day <= day - 1f) drop++;
        if (drop > 0) log.RemoveRange(0, drop);
    }
}
