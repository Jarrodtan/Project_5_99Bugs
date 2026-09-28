using System.Collections.Generic;

/// <summary>
/// Read-only inspection contract for any selectable object. Consumers must not cast to concrete inspectable types.
/// </summary>
public interface IInspectable
{
    /// <summary>Human-readable title for the inspector.</summary>
    string GetDisplayName();

    /// <summary>Current label/value rows to display.</summary>
    IReadOnlyList<StatEntry> GetStats();
}
