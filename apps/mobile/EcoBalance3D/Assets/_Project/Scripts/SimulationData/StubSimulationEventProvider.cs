using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Empty <see cref="ISimulationEventProvider"/> placeholder until T02 emits behaviour events.
/// Returns an empty list only. Do not wire to HimalayaSimulation yet.
/// See docs/t02-simulation-data-sync.md.
/// </summary>
public class StubSimulationEventProvider : MonoBehaviour, ISimulationEventProvider
{
    // TODO(T02): Replace with live event source after sync agreement — see docs/t02-simulation-data-sync.md

    /// <summary>
    /// Recent events. Always empty until this stub is replaced or filled.
    /// </summary>
    /// <returns>Empty read-only list; never null.</returns>
    public IReadOnlyList<SimulationEvent> GetRecentEvents()
    {
        return Array.Empty<SimulationEvent>();
    }
}
