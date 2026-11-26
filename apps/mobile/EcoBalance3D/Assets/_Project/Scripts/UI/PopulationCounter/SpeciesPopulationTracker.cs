using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Direction of the most recent change in a species' displayed count.
/// </summary>
public enum PopulationTrend
{
    None,
    Up,
    Down
}

/// <summary>
/// Whether a species counter has a value, and whether that value means extinction.
/// </summary>
public enum SpeciesCounterStatus
{
    Unavailable,
    Alive,
    Extinct
}

/// <summary>
/// Snapshot of one species row for Display 1. Produced by <see cref="SpeciesPopulationTracker"/>.
/// </summary>
public readonly struct SpeciesCounterState
{
    /// <summary>Stable species id from <see cref="ISimulationDataProvider"/>.</summary>
    public readonly string SpeciesId;

    /// <summary>Human-readable name from <see cref="ISimulationDataProvider.GetDisplayName"/>.</summary>
    public readonly string DisplayName;

    /// <summary>Whole-number population shown to the player. Zero when unavailable.</summary>
    public readonly int Count;

    /// <summary>Alive, extinct (count is zero), or unavailable (provider has no value).</summary>
    public readonly SpeciesCounterStatus Status;

    /// <summary>Direction of the most recent count change; persists until the next change.</summary>
    public readonly PopulationTrend Trend;

    /// <summary>True only on the refresh where the count changed. Drives the flash.</summary>
    public readonly bool Changed;

    public SpeciesCounterState(
        string speciesId,
        string displayName,
        int count,
        SpeciesCounterStatus status,
        PopulationTrend trend,
        bool changed)
    {
        SpeciesId = speciesId;
        DisplayName = displayName;
        Count = count;
        Status = status;
        Trend = trend;
        Changed = changed;
    }
}

/// <summary>
/// Reads per-species populations from an <see cref="ISimulationDataProvider"/> and tracks change direction
/// and extinction between reads. Plain C# so it can be tested without a scene.
/// </summary>
public class SpeciesPopulationTracker
{
    readonly List<SpeciesCounterState> states = new();
    readonly List<string> trackedIds = new();
    readonly List<string> incomingIds = new();
    readonly Dictionary<string, int> lastCounts = new();
    readonly Dictionary<string, PopulationTrend> lastTrends = new();

    /// <summary>
    /// Current row snapshots in provider order. Valid after the first <see cref="Refresh"/>.
    /// </summary>
    public IReadOnlyList<SpeciesCounterState> States => states;

    /// <summary>
    /// Converts a raw provider population to the whole number shown on screen.
    /// Negative values clamp to zero; the result is what extinction is judged on.
    /// </summary>
    /// <param name="rawCount">Population from <see cref="ISimulationDataProvider.TryGetPopulationCount"/>.</param>
    /// <returns>Rounded, non-negative count.</returns>
    public static int ToDisplayCount(float rawCount)
    {
        if (rawCount <= 0f)
            return 0;
        return Mathf.RoundToInt(rawCount);
    }

    /// <summary>
    /// Re-reads every species from the provider and updates <see cref="States"/>.
    /// </summary>
    /// <param name="provider">Data access layer to read from. Null clears all rows.</param>
    /// <returns>True when the set or order of species ids changed (rows must be rebuilt).</returns>
    public bool Refresh(ISimulationDataProvider provider)
    {
        var ids = provider != null ? provider.GetSpeciesIds() : null;
        var layoutChanged = SyncTrackedIds(ids);

        states.Clear();
        for (var i = 0; i < trackedIds.Count; i++)
            states.Add(ReadSpecies(provider, trackedIds[i]));

        return layoutChanged;
    }

    /// <summary>
    /// Forgets all previous counts so the next <see cref="Refresh"/> shows no trend or flash.
    /// </summary>
    public void Reset()
    {
        states.Clear();
        trackedIds.Clear();
        lastCounts.Clear();
        lastTrends.Clear();
    }

    SpeciesCounterState ReadSpecies(ISimulationDataProvider provider, string id)
    {
        var name = provider.GetDisplayName(id);
        if (string.IsNullOrEmpty(name))
            name = id;

        if (!provider.TryGetPopulationCount(id, out var raw) || float.IsNaN(raw))
        {
            // No value means "unknown", never "extinct"; drop history so a returning value doesn't flash.
            lastCounts.Remove(id);
            lastTrends.Remove(id);
            return new SpeciesCounterState(id, name, 0, SpeciesCounterStatus.Unavailable, PopulationTrend.None, false);
        }

        var count = ToDisplayCount(raw);
        var status = count == 0 ? SpeciesCounterStatus.Extinct : SpeciesCounterStatus.Alive;

        lastTrends.TryGetValue(id, out var trend);
        var changed = false;
        if (lastCounts.TryGetValue(id, out var previous) && previous != count)
        {
            trend = count > previous ? PopulationTrend.Up : PopulationTrend.Down;
            changed = true;
        }

        lastCounts[id] = count;
        lastTrends[id] = trend;
        return new SpeciesCounterState(id, name, count, status, trend, changed);
    }

    bool SyncTrackedIds(IReadOnlyList<string> ids)
    {
        incomingIds.Clear();
        if (ids != null)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]) && !incomingIds.Contains(ids[i]))
                    incomingIds.Add(ids[i]);
            }
        }

        var same = incomingIds.Count == trackedIds.Count;
        for (var i = 0; i < incomingIds.Count && same; i++)
            same = incomingIds[i] == trackedIds[i];

        if (same)
            return false;

        trackedIds.Clear();
        trackedIds.AddRange(incomingIds);

        PruneHistory(lastCounts);
        PruneHistory(lastTrends);
        return true;
    }

    void PruneHistory<T>(Dictionary<string, T> history)
    {
        if (history.Count == 0)
            return;

        var stale = new List<string>();
        foreach (var key in history.Keys)
        {
            if (!trackedIds.Contains(key))
                stale.Add(key);
        }

        for (var i = 0; i < stale.Count; i++)
            history.Remove(stale[i]);
    }
}
