using System.Collections.Generic;

/// <summary>
/// Fixed label/value rows copied from an inspectable schema (entity / stub kinds).
/// </summary>
public sealed class StaticStatSource : IInspectableStatSource
{
    readonly string displayName;
    readonly List<StatEntry> stats;

    /// <summary>
    /// Creates a static source from a schema's display name and static rows.
    /// </summary>
    /// <param name="schema">Schema providing display name and static stats.</param>
    public StaticStatSource(InspectableSchema schema)
    {
        displayName = schema != null ? schema.displayName : string.Empty;
        stats = new List<StatEntry>();

        if (schema?.staticStats == null)
            return;

        for (var i = 0; i < schema.staticStats.Count; i++)
        {
            var row = schema.staticStats[i];
            stats.Add(new StatEntry(row.label, row.value));
        }
    }

    /// <inheritdoc />
    public string DisplayName => displayName;

    /// <inheritdoc />
    public IReadOnlyList<StatEntry> GetStats() => stats;
}
