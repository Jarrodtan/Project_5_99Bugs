// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Headless, agent-based balance model: every sheep and leopard has its own fullness, health and
/// cooldowns, following the same rules the 3D animals use. Runs hundreds of simulated days in
/// milliseconds so designers can check whether populations cycle, crash or explode before
/// tuning the real scene. Deterministic: the same parameters and seed give the same history.
/// </summary>
public static class BalanceModel
{
    /// <summary>One simulated day's totals.</summary>
    public readonly struct DaySample
    {
        public readonly int Day, Sheep, Leopards;
        public readonly float GrassAverage;
        public DaySample(int day, int sheep, int leopards, float grass)
        { Day = day; Sheep = sheep; Leopards = leopards; GrassAverage = grass; }
    }

    sealed class Animal
    {
        public float Fullness = 100f, Health = 100f;
        public int BreedCooldown, HuntCooldown;
        public Animal(int breedCooldown) { BreedCooldown = breedCooldown; }
    }

    public static List<DaySample> Run(BalanceParameters p)
    {
        var rng = new Random(p.seed);
        var sheep = new List<Animal>();
        var leopards = new List<Animal>();
        for (int i = 0; i < p.sheepStart; i++) sheep.Add(new Animal(0));
        for (int i = 0; i < p.leopardStart; i++) leopards.Add(new Animal(0));
        var grass = new float[p.grassPatches];
        for (int i = 0; i < grass.Length; i++) grass[i] = 100f;

        var history = new List<DaySample>(p.days);
        var grazeable = new List<int>(grass.Length);

        for (int day = 1; day <= p.days; day++)
        {
            for (int i = 0; i < grass.Length; i++)
                grass[i] = Math.Min(100f, grass[i] + p.regrowPerDay);

            // Sheep graze one random patch that can still be bitten.
            foreach (var a in sheep)
            {
                a.Fullness = Math.Max(0f, a.Fullness - p.sheepHungerPerDay);
                if (a.Fullness > p.seekFoodAt) continue;
                grazeable.Clear();
                for (int i = 0; i < grass.Length; i++)
                    if (grass[i] - p.bite >= p.minGraze - 1e-4f) grazeable.Add(i);
                if (grazeable.Count == 0) continue;
                grass[grazeable[rng.Next(grazeable.Count)]] -= p.bite;
                a.Fullness = Math.Min(100f, a.Fullness + p.sheepMeal);
            }

            // Hungry leopards off cooldown may find a sheep, then catch it with huntSuccess.
            foreach (var a in leopards)
            {
                a.Fullness = Math.Max(0f, a.Fullness - p.leopardHungerPerDay);
                a.HuntCooldown--;
                if (a.Fullness > p.seekFoodAt || a.HuntCooldown > 0 || sheep.Count == 0) continue;
                double found = 1.0 - Math.Exp(-sheep.Count / p.encounterScale);
                if (rng.NextDouble() >= found) continue;
                a.HuntCooldown = p.huntCooldownDays;
                if (rng.NextDouble() >= p.huntSuccess) continue;
                sheep.RemoveAt(rng.Next(sheep.Count));
                a.Fullness = Math.Min(100f, a.Fullness + p.leopardMeal);
            }

            ApplyHealth(sheep, p);
            ApplyHealth(leopards, p);
            Breed(sheep, p.sheepBreedEveryDays, p);
            Breed(leopards, p.leopardBreedEveryDays, p);

            float total = 0f;
            foreach (var g in grass) total += g;
            history.Add(new DaySample(day, sheep.Count, leopards.Count, total / grass.Length));
        }
        return history;
    }

    static void ApplyHealth(List<Animal> pop, BalanceParameters p)
    {
        foreach (var a in pop)
            a.Health = a.Fullness <= 0f
                ? a.Health - p.starvationDamagePerDay
                : Math.Min(100f, a.Health + p.healthRegenPerDay);
        pop.RemoveAll(a => a.Health <= 0f);
    }

    // Every two well-fed adults off cooldown make one baby; newborns wait a full interval to breed.
    static void Breed(List<Animal> pop, int everyDays, BalanceParameters p)
    {
        Animal waiting = null;
        int existing = pop.Count;
        for (int i = 0; i < existing; i++)
        {
            var a = pop[i];
            a.BreedCooldown--;
            if (a.Fullness < p.breedFullness || a.BreedCooldown > 0) continue;
            if (waiting == null) { waiting = a; continue; }
            if (pop.Count >= p.maxPopulationPerSpecies) return;
            waiting.BreedCooldown = a.BreedCooldown = everyDays;
            pop.Add(new Animal(everyDays));
            waiting = null;
        }
    }

    /// <summary>Headline numbers for a run: ranges, extinction days and number of sheep booms.</summary>
    public readonly struct Summary
    {
        public readonly int SheepMin, SheepMax, LeopardMin, LeopardMax, SheepPeaks;
        /// <summary>First day the species hit 0, or -1 if it survived.</summary>
        public readonly int SheepExtinctDay, LeopardExtinctDay;
        public bool BothSurvived => SheepExtinctDay < 0 && LeopardExtinctDay < 0;

        public Summary(IReadOnlyList<DaySample> h)
        {
            SheepMin = LeopardMin = int.MaxValue; SheepMax = LeopardMax = 0;
            SheepExtinctDay = LeopardExtinctDay = -1;
            foreach (var s in h)
            {
                SheepMin = Math.Min(SheepMin, s.Sheep); SheepMax = Math.Max(SheepMax, s.Sheep);
                LeopardMin = Math.Min(LeopardMin, s.Leopards); LeopardMax = Math.Max(LeopardMax, s.Leopards);
                if (s.Sheep == 0 && SheepExtinctDay < 0) SheepExtinctDay = s.Day;
                if (s.Leopards == 0 && LeopardExtinctDay < 0) LeopardExtinctDay = s.Day;
            }
            SheepPeaks = CountPeaks(h);
        }

        // A peak = a rise followed by a fall of at least 20% (and 3 animals), so noise isn't counted.
        static int CountPeaks(IReadOnlyList<DaySample> h)
        {
            if (h.Count == 0) return 0;
            int peaks = 0, high = h[0].Sheep, low = h[0].Sheep;
            bool rising = true;
            foreach (var s in h)
            {
                int v = s.Sheep;
                if (rising)
                {
                    if (v > high) high = v;
                    else if (v < high * 0.8f && high - v >= 3) { peaks++; rising = false; low = v; }
                }
                else
                {
                    if (v < low) low = v;
                    else if (v > low * 1.25f && v - low >= 3) { rising = true; high = v; }
                }
            }
            return peaks;
        }

        public override string ToString() =>
            $"sheep {SheepMin}-{SheepMax} (extinct day {SheepExtinctDay}), " +
            $"leopards {LeopardMin}-{LeopardMax} (extinct day {LeopardExtinctDay}), sheep peaks {SheepPeaks}";
    }

    /// <summary>History as CSV (day,sheep,leopards,grass) for spreadsheets or the population graph.</summary>
    public static string ToCsv(IReadOnlyList<DaySample> h)
    {
        var sb = new StringBuilder("day,sheep,leopards,grass\n");
        foreach (var s in h)
            sb.Append(s.Day).Append(',').Append(s.Sheep).Append(',').Append(s.Leopards).Append(',')
              .Append(s.GrassAverage.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }
}
// [AI-GENERATED END]
