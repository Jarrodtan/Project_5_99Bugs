// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Seasons and daily weather, and what they do to food and water:
///   Spring  – frequent rain, grass grows fast
///   Summer  – warm; sometimes drought: grass browns and barely grows, the river runs low
///   Autumn  – grass slows down
///   Winter  – snow; grass hardly grows, water partly freezes
/// The river is fed by the mountain, so it refills quickly except in drought and winter.
/// Day 1 starts from the live Himalaya weather (WeatherManager), then each new day rolls weather
/// that fits the season. Changes are announced with a banner and shown in the status pill.
/// Grass colour follows the global shader value _EcoDryness (0 = lush green, 1 = dry brown).
/// </summary>
public class SeasonWeather : MonoBehaviour
{
    public enum Season { Spring, Summer, Autumn, Winter }
    public enum Weather { Clear, Rain, Snow, Drought }

    [Tooltip("Game days in each season (1 game day = 1 real minute at 1x).")]
    public int daysPerSeason = 6;

    public static Season CurrentSeason { get; private set; }
    public static Weather CurrentWeather { get; private set; }

    ClockService clock;
    int lastDay = -1;
    Season lastSeason;
    float dryness;   // eased towards the target so grass changes colour gradually
    static readonly int DrynessId = Shader.PropertyToID("_EcoDryness");

    void Start()
    {
        clock = FindFirstObjectByType<ClockService>();
    }

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

        if (Time.time >= nextSync) { nextSync = Time.time + 0.5f; SyncResourceTotals(); }
    }

    float nextSync;
    readonly System.Collections.Generic.Dictionary<ResourceDefinition, (float sum, int count)> totals = new();

    // The simulator's Grass / Water numbers (HUD, graphs, investigation cases) used to be a separate pool
    // that every bite drained, so it ran dry even with a flowing river and full meadows. Now they show the
    // real average fullness of the patches on the map.
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
        CurrentSeason = (Season)(((Mathf.Max(1, day) - 1) / Mathf.Max(1, daysPerSeason)) % 4);
        CurrentWeather = first ? FromLive(WeatherManager.LiveCondition) : Roll(CurrentSeason);
        ApplyEffects();

        string status = $"{CurrentSeason} · Day {day} · {Describe(CurrentWeather)}";
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
        float water = CurrentSeason == Season.Winter ? 0.4f : 1f;

        switch (CurrentWeather)
        {
            case Weather.Rain: plants *= 1.6f; water *= 2f; break;
            case Weather.Snow: plants *= 0.5f; water *= 0.7f; break;
            case Weather.Drought: plants *= 0.15f; water *= 0.2f; break;
        }
        ResourcePatch.PlantGrowthMultiplier = plants;
        ResourcePatch.WaterGrowthMultiplier = water;
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
        _ => "Drought! No rain, the grass is drying up and the river is low.",
    };
}
