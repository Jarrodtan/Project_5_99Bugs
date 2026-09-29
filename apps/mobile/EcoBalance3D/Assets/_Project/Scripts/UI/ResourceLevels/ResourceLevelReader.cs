using System;
using System.Collections.Generic;

/// <summary>
/// Max level and critical threshold a resource is judged against.
/// </summary>
public readonly struct ResourceGaugeLimits
{
    /// <summary>Level that fills the bar completely.</summary>
    public readonly float MaxLevel;

    /// <summary>Critical when level / <see cref="MaxLevel"/> is at or below this fraction.</summary>
    public readonly float CriticalFraction;

    public ResourceGaugeLimits(float maxLevel, float criticalFraction)
    {
        MaxLevel = maxLevel;
        CriticalFraction = criticalFraction;
    }
}

/// <summary>
/// Whether a resource gauge has a value, and whether that value is critically low.
/// </summary>
public enum ResourceLevelStatus
{
    Unavailable,
    Normal,
    Critical
}

/// <summary>
/// Snapshot of one resource row for Display 2. Produced by <see cref="ResourceLevelReader"/>.
/// </summary>
public readonly struct ResourceLevelState
{
    /// <summary>Stable resource id from <see cref="ISimulationDataProvider"/>.</summary>
    public readonly string ResourceId;

    /// <summary>Human-readable name from <see cref="ISimulationDataProvider.GetDisplayName"/>.</summary>
    public readonly string DisplayName;

    /// <summary>Current level, clamped to be non-negative. May exceed <see cref="MaxLevel"/>.</summary>
    public readonly float Level;

    /// <summary>Level that fills the bar.</summary>
    public readonly float MaxLevel;

    /// <summary>Bar fill in 0–1.</summary>
    public readonly float Fraction;

    /// <summary>Normal, critical (at or below threshold), or unavailable (provider has no value).</summary>
    public readonly ResourceLevelStatus Status;

    public ResourceLevelState(
        string resourceId,
        string displayName,
        float level,
        float maxLevel,
        float fraction,
        ResourceLevelStatus status)
    {
        ResourceId = resourceId;
        DisplayName = displayName;
        Level = level;
        MaxLevel = maxLevel;
        Fraction = fraction;
        Status = status;
    }
}

/// <summary>
/// Reads every resource from an <see cref="ISimulationDataProvider"/> and evaluates bar fill and
/// critical state against per-resource limits. Plain C# so it can be tested without a scene.
/// </summary>
public class ResourceLevelReader
{
    readonly List<ResourceLevelState> states = new();
    readonly HashSet<string> seenIds = new();

    /// <summary>
    /// Current row snapshots in provider order. Valid after the first <see cref="Refresh"/>.
    /// </summary>
    public IReadOnlyList<ResourceLevelState> States => states;

    /// <summary>
    /// Re-reads every resource from the provider and updates <see cref="States"/>.
    /// </summary>
    /// <param name="provider">Data access layer to read from. Null clears all rows.</param>
    /// <param name="limitsFor">Returns the gauge limits for a resource id.</param>
    public void Refresh(ISimulationDataProvider provider, Func<string, ResourceGaugeLimits> limitsFor)
    {
        states.Clear();
        seenIds.Clear();
        if (provider == null || limitsFor == null)
            return;

        var ids = provider.GetResourceIds();
        if (ids == null)
            return;

        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (string.IsNullOrEmpty(id) || !seenIds.Add(id))
                continue;
            states.Add(Read(provider, id, limitsFor(id)));
        }
    }

    /// <summary>
    /// Evaluates one resource against its limits.
    /// </summary>
    /// <param name="provider">Data access layer to read from.</param>
    /// <param name="resourceId">Resource id to read.</param>
    /// <param name="limits">Max level and critical threshold for this resource.</param>
    /// <returns>Row snapshot; <see cref="ResourceLevelStatus.Unavailable"/> when the provider has no value.</returns>
    public static ResourceLevelState Read(ISimulationDataProvider provider, string resourceId, ResourceGaugeLimits limits)
    {
        var name = provider.GetDisplayName(resourceId);
        if (string.IsNullOrEmpty(name))
            name = resourceId;

        var max = limits.MaxLevel;
        if (!provider.TryGetResourceLevel(resourceId, out var raw) || float.IsNaN(raw))
            return new ResourceLevelState(resourceId, name, 0f, max, 0f, ResourceLevelStatus.Unavailable);

        var level = Math.Max(0f, raw);
        var fraction = ToFraction(level, max);
        var status = fraction <= limits.CriticalFraction ? ResourceLevelStatus.Critical : ResourceLevelStatus.Normal;
        return new ResourceLevelState(resourceId, name, level, max, fraction, status);
    }

    /// <summary>
    /// Bar fill for a level. A non-positive max shows a full bar for any stock and an empty bar at zero.
    /// </summary>
    /// <param name="level">Non-negative level.</param>
    /// <param name="maxLevel">Level that fills the bar.</param>
    /// <returns>Fill in 0–1.</returns>
    public static float ToFraction(float level, float maxLevel)
    {
        if (maxLevel <= 0f)
            return level > 0f ? 1f : 0f;
        return Math.Min(1f, Math.Max(0f, level / maxLevel));
    }
}
