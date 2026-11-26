using System.Collections.Generic;

/// <summary>
/// Live or static provider of display name and stats for one registered selectable handle.
/// </summary>
public interface IInspectableStatSource
{
    /// <summary>Display title for this object.</summary>
    string DisplayName { get; }

    /// <summary>Current stats (may change each call for live sources).</summary>
    IReadOnlyList<StatEntry> GetStats();
}
