using System.Collections.Generic;

/// <summary>
/// Read-only access to recent ecosystem behaviour events for Display 3 (event feed).
/// Separate from <see cref="ISimulationDataProvider"/> (population/resource aggregates).
/// Blocked until T02 exposes behaviour events; see docs/t02-simulation-data-sync.md.
/// </summary>
public interface ISimulationEventProvider
{
    /// <summary>
    /// Recent simulation events in chronological or feed order.
    /// </summary>
    /// <returns>Read-only list of events; never null. Empty until a live provider is filled.</returns>
    IReadOnlyList<SimulationEvent> GetRecentEvents();
}
