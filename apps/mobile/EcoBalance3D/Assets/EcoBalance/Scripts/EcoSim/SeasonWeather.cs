// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Seasons and daily weather, and what they do to food and water:
///   Spring  – frequent rain, grass grows fast
///   Summer  – warm; sometimes a heatwave: grass browns, but the glaciers melt and the river runs full
///   Autumn  – grass slows down
///   Winter  – snow; grass hardly grows, water partly freezes
/// The river is glacier meltwater, as in the real Khumbu: warm days melt more ice and fill it,
/// freezing days melt almost none and it runs low. A dry spell only shrinks it when it is also cold.
/// Day 1 starts from the live Himalaya weather (WeatherManager), then each new day rolls weather
/// that fits the season. Changes are announced with a banner and shown in the status pill.
/// Grass colour follows the global shader value _EcoDryness (0 = lush green, 1 = dry brown).
/// </summary>
public class SeasonWeather : MonoBehaviour
{
    public enum Season { Spring, Summer, Autumn, Winter }
    public enum Weather { Clear, Rain, Snow, Drought }

    [Tooltip("Game days in each season (1 game day = 2 real minutes at 1x).")]
    public int daysPerSeason = 5;

    public static Season CurrentSeason { get; private set; }
    /// <summary>Set by Guided Exploration so every challenge starts the same way (Summer → Autumn → Winter → Spring). Null = follow the live weather.</summary>
    public static Season? ForcedStartSeason;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
    public static Weather CurrentWeather { get; private set; }

    /// <summary>Valley temperature today (°C).</summary>
    public static float Temperature { get; private set; }
    /// <summary>Heat makes animals thirsty faster (1 = normal). Read by AnimalBrain.</summary>
    public static float ThirstDrainMultiplier { get; private set; } = 1f;
    /// <summary>Hard cold makes animals hungry faster: they burn energy keeping warm (1 = normal).</summary>
    public static float HungerDrainMultiplier { get; private set; } = 1f;

    int seasonOffset;   // so day 1 starts in the season that fits the real temperature

    ClockService clock;
    int lastDay = -1;
    Season lastSeason;
    float dryness;   // eased towards the target so grass changes colour gradually
    static readonly int DrynessId = Shader.PropertyToID("_EcoDryness");

    EcosystemSimulator simulator;

    void Start()
    {
        clock = FindFirstObjectByType<ClockService>();
        simulator = FindFirstObjectByType<EcosystemSimulator>();
        if (simulator != null)
        {
            simulator.EatRecorded += OnEatRecorded;
            simulator.Ticked += SyncResourceTotals;
        }
    }

    void OnDestroy()
    {
        if (simulator == null) return;
        simulator.EatRecorded -= OnEatRecorded;
        simulator.Ticked -= SyncResourceTotals;
    }

    void OnEatRecorded(SpeciesDefinition eater, ResourceDefinition resource) => SyncResourceTotals();

    void Update()
    {
        int day = clock != null ? clock.Day : 1 + Mathf.FloorToInt(Time.time / 60f);
        if (day != lastDay) NewDay(day);

        float target = CurrentWeather == Weather.Drought ? 1f
                     : CurrentSeason == Season.Winter ? 0.75f
                     : CurrentSeason == Season.Autumn ? 0.45f
                     : CurrentWeather == Weather.Rain ? 0f : 0.15f;
        dryness = Mathf.MoveTowards(dryness, target, Time.deltaTime * 0.1f);
        Shader.SetGlobalFloat(DrynessId, dryness);
    }

    // Every frame, after animals have eaten and patches have regrown, so the stats never show a stale value.
    void LateUpdate() => SyncResourceTotals();

    readonly System.Collections.Generic.Dictionary<ResourceDefinition, (float sum, int count)> totals = new();

    // The simulator's Grass / Water numbers (HUD, graphs, investigation cases) used to be a separate pool
    // that every bite drained, so it ran dry even with a flowing river and full meadows. Now they show the
    // real average fullness of the patches on the map.
    // The simulator still changes its own pool in Events mode (+eventRegenPerTick each tick, -bite on every
    // RecordEat), so the stats dipped on each bite and snapped back on the next sync. Re-syncing right after
    // those (EatRecorded / Ticked) and every LateUpdate keeps the patches the only source of the number.
    void SyncResourceTotals()
    {
        totals.Clear();
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.Resource == null) continue;
            totals.TryGetValue(p.Resource, out var t);
            totals[p.Resource] = (t.sum + p.Amount, t.count + 1);
        }
        foreach (var kv in totals)
        {
            var r = kv.Key;
            float avgFill = kv.Value.count > 0 && r.maxAmount > 0f ? kv.Value.sum / kv.Value.count / r.maxAmount : 0f;
            r.currentAmount = avgFill * r.maxAmount;
        }
    }

    void NewDay(int day)
    {
        bool first = lastDay < 0;
        lastDay = day;

        // Day 1: the real valley temperature picks the starting season (a freezing real day starts in winter).
        if (first && WeatherManager.HasLiveData)
        {
            float t = WeatherManager.ValleyTemperature;
            seasonOffset = t >= 8f ? (int)Season.Summer : t >= 0f ? (int)Season.Spring : t >= -10f ? (int)Season.Autumn : (int)Season.Winter;
        }
        if (first && ForcedStartSeason.HasValue) seasonOffset = (int)ForcedStartSeason.Value;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): Guided always starts in summer
        CurrentSeason = (Season)((seasonOffset + (Mathf.Max(1, day) - 1) / Mathf.Max(1, daysPerSeason)) % 4);
        CurrentWeather = first ? FromLive(WeatherManager.LiveCondition) : Roll(CurrentSeason);
        Temperature = first && WeatherManager.HasLiveData ? WeatherManager.ValleyTemperature : RollTemperature(CurrentSeason, CurrentWeather);
        if (!first && CurrentSeason == Season.Summer && Temperature > 16f) CurrentWeather = Weather.Drought;   // heatwave dries the land
        ApplyEffects();

        string status = $"{CurrentSeason} · Day {day} · {Describe(CurrentWeather)} · {Temperature:0}°C";
        DramaDirector.SetStatus(status);

        if (first || CurrentSeason != lastSeason)
            DramaDirector.Announce(SeasonLine(CurrentSeason));
        else if (CurrentWeather == Weather.Drought || CurrentWeather == Weather.Rain || CurrentWeather == Weather.Snow)
            DramaDirector.Announce(WeatherLine(CurrentWeather));
        lastSeason = CurrentSeason;
    }

    void ApplyEffects()
    {
        float plants = CurrentSeason switch
        {
            Season.Spring => 1.5f, Season.Summer => 1.1f, Season.Autumn => 0.6f, _ => 0.15f,
        };
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): meltwater follows temperature: -10 °C or colder -> 0.2x, 0 °C -> 0.7x, 10 °C+ -> 1.2x+.
        float water = Mathf.Clamp(0.7f + Temperature * 0.05f, 0.2f, 1.6f);

        switch (CurrentWeather)
        {
            case Weather.Rain: plants *= 1.6f; water *= 1.5f; break;
            case Weather.Snow: plants *= 0.5f; water *= 0.7f; break;
            case Weather.Drought: plants *= 0.15f; if (Temperature < 5f) water *= 0.4f; break;   // a heatwave still melts ice
        }
        ResourcePatch.PlantGrowthMultiplier = plants;
        ResourcePatch.WaterGrowthMultiplier = water;

        // 20 °C → 1.5x thirst; -25 °C → 1.45x hunger. Mild days change nothing.
        ThirstDrainMultiplier = Temperature > 10f ? 1f + (Temperature - 10f) * 0.05f : 1f;
        HungerDrainMultiplier = Temperature < -10f ? 1f + (-10f - Temperature) * 0.03f : 1f;
    }

    static float RollTemperature(Season s, Weather w)
    {
        float t = s switch { Season.Spring => 4f, Season.Summer => 12f, Season.Autumn => 0f, _ => -15f };
        t += Random.Range(-4f, 4f);
        if (w == Weather.Drought) t += 6f;
        if (w == Weather.Snow) t -= 5f;
        return t;
    }

    static Weather Roll(Season s)
    {
        float r = Random.value;
        return s switch
        {
            Season.Spring => r < 0.45f ? Weather.Rain : Weather.Clear,
            Season.Summer => r < 0.3f ? Weather.Drought : r < 0.5f ? Weather.Rain : Weather.Clear,
            Season.Autumn => r < 0.2f ? Weather.Rain : r < 0.3f ? Weather.Snow : Weather.Clear,
            _ => r < 0.55f ? Weather.Snow : Weather.Clear,
        };
    }

    static Weather FromLive(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return Weather.Clear;
        string c = condition.ToLowerInvariant();
        if (c.Contains("snow")) return Weather.Snow;
        if (c.Contains("rain") || c.Contains("drizzle") || c.Contains("thunder")) return Weather.Rain;
        return Weather.Clear;
    }

    static string Describe(Weather w) => w switch
    {
        Weather.Rain => "Rain", Weather.Snow => "Snow", Weather.Drought => "Drought", _ => "Clear",
    };

    static string SeasonLine(Season s) => s switch
    {
        Season.Spring => "Spring is here! Rain helps the grass grow fast.",
        Season.Summer => "Summer! Watch out for dry days with no rain.",
        Season.Autumn => "Autumn. The grass is growing slower now.",
        _ => "Winter! Snow covers the grass. Can the herds find enough food?",
    };

    static string WeatherLine(Weather w) => w switch
    {
        Weather.Rain => "It's raining! Grass and rivers fill up.",
        Weather.Snow => "Snow is falling. Food is harder to find.",
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): glacier river - hot drought melts ice, cold drought freezes it
        _ => Temperature >= 5f
            ? "Heatwave! The grass is drying up, but melting glaciers keep the river flowing."
            : "Dry and freezing! Little ice melts, so the river is running low.",
    };
}
