using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mock <see cref="ISimulationDataProvider"/> for Displays 1–3 before a real T02 adapter exists.
/// Reads the catalog only from <see cref="MockSimulationDataConfig"/>; re-rolls values on an interval.
/// </summary>
public class MockSimulationDataProvider : MonoBehaviour, ISimulationDataProvider
{
    [Tooltip("Shared mock catalog (ids, names, ranges, refresh interval).")]
    [SerializeField]
    MockSimulationDataConfig config;

    readonly List<string> speciesIds = new();
    readonly List<string> resourceIds = new();
    readonly Dictionary<string, string> displayNames = new();
    readonly Dictionary<string, float> populations = new();
    readonly Dictionary<string, float> resourceLevels = new();
    float nextRefreshTime;
    bool ready;

    /// <inheritdoc />
    public IReadOnlyList<string> GetSpeciesIds() => speciesIds;

    /// <inheritdoc />
    public IReadOnlyList<string> GetResourceIds() => resourceIds;

    /// <inheritdoc />
    public bool TryGetPopulationCount(string speciesId, out float count)
    {
        count = 0f;
        if (!ready || string.IsNullOrEmpty(speciesId))
            return false;
        return populations.TryGetValue(speciesId, out count);
    }

    /// <inheritdoc />
    public bool TryGetResourceLevel(string resourceId, out float level)
    {
        level = 0f;
        if (!ready || string.IsNullOrEmpty(resourceId))
            return false;
        return resourceLevels.TryGetValue(resourceId, out level);
    }

    /// <inheritdoc />
    public string GetDisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;
        if (displayNames.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name))
            return name;
        return id;
    }

    void OnEnable()
    {
        if (!RebuildFromConfig())
            return;

        RerollAll();
        ScheduleNextRefresh();
    }

    void Update()
    {
        if (!ready || config == null || config.refreshIntervalSeconds <= 0f)
            return;

        if (Time.time < nextRefreshTime)
            return;

        RerollAll();
        ScheduleNextRefresh();
    }

    bool RebuildFromConfig()
    {
        ready = false;
        speciesIds.Clear();
        resourceIds.Clear();
        displayNames.Clear();
        populations.Clear();
        resourceLevels.Clear();

        if (config == null)
        {
            Debug.LogError($"{nameof(MockSimulationDataProvider)} requires a {nameof(MockSimulationDataConfig)}.", this);
            return false;
        }

        if (config.species != null)
        {
            for (var i = 0; i < config.species.Count; i++)
            {
                var entry = config.species[i];
                if (string.IsNullOrEmpty(entry.id))
                    continue;
                if (populations.ContainsKey(entry.id))
                    continue;

                speciesIds.Add(entry.id);
                displayNames[entry.id] = entry.displayName;
                populations[entry.id] = 0f;
            }
        }

        if (config.resources != null)
        {
            for (var i = 0; i < config.resources.Count; i++)
            {
                var entry = config.resources[i];
                if (string.IsNullOrEmpty(entry.id))
                    continue;
                if (resourceLevels.ContainsKey(entry.id))
                    continue;

                resourceIds.Add(entry.id);
                displayNames[entry.id] = entry.displayName;
                resourceLevels[entry.id] = 0f;
            }
        }

        ready = true;
        return true;
    }

    void RerollAll()
    {
        if (config == null)
            return;

        if (config.species != null)
        {
            for (var i = 0; i < config.species.Count; i++)
            {
                var entry = config.species[i];
                if (string.IsNullOrEmpty(entry.id) || !populations.ContainsKey(entry.id))
                    continue;

                var min = entry.minPopulation;
                var max = entry.maxPopulation;
                if (max < min)
                    (min, max) = (max, min);
                populations[entry.id] = Random.Range(min, max);
            }
        }

        if (config.resources != null)
        {
            for (var i = 0; i < config.resources.Count; i++)
            {
                var entry = config.resources[i];
                if (string.IsNullOrEmpty(entry.id) || !resourceLevels.ContainsKey(entry.id))
                    continue;

                var min = entry.minLevel;
                var max = entry.maxLevel;
                if (max < min)
                    (min, max) = (max, min);
                resourceLevels[entry.id] = Random.Range(min, max);
            }
        }
    }

    void ScheduleNextRefresh()
    {
        if (config == null || config.refreshIntervalSeconds <= 0f)
        {
            nextRefreshTime = float.PositiveInfinity;
            return;
        }

        nextRefreshTime = Time.time + config.refreshIntervalSeconds;
    }
}
