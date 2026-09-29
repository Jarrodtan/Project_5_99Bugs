using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live-sim adapter that will implement <see cref="ISimulationDataProvider"/> against
/// <see cref="EcosystemSimulator"/>. Blocked until T02 agrees on string ids and mapping;
/// see docs/t02-simulation-data-sync.md. Empty behaviour only — do not wire to SampleScene yet.
/// </summary>
public class EcosystemSimulationDataProvider : MonoBehaviour, ISimulationDataProvider
{
    // TODO(T02): Wire after sync agreement — see docs/t02-simulation-data-sync.md
    [Tooltip("Live ecosystem simulator (optional until adapter is filled).")]
    [SerializeField]
    EcosystemSimulator simulator;

    // TODO(T02): Assign EcosystemSimulationDataConfig when that asset exists — see docs/t02-simulation-data-sync.md
    // [SerializeField] EcosystemSimulationDataConfig mappingConfig;

    /// <summary>
    /// Stable species ids from the live mapping. Empty until the adapter is filled.
    /// </summary>
    /// <returns>Read-only list of species ids; never null.</returns>
    public IReadOnlyList<string> GetSpeciesIds()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    /// Stable resource ids from the live mapping. Empty until the adapter is filled.
    /// </summary>
    /// <returns>Read-only list of resource ids; never null.</returns>
    public IReadOnlyList<string> GetResourceIds()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    /// Reads population for a species id from the live sim. Always fails until filled.
    /// </summary>
    /// <param name="speciesId">Species key.</param>
    /// <param name="count">Unused; set to 0.</param>
    /// <returns>False — adapter not implemented yet.</returns>
    public bool TryGetPopulationCount(string speciesId, out float count)
    {
        count = 0f;
        return false;
    }

    /// <summary>
    /// Reads resource level for a resource id from the live sim. Always fails until filled.
    /// </summary>
    /// <param name="resourceId">Resource key.</param>
    /// <param name="level">Unused; set to 0.</param>
    /// <returns>False — adapter not implemented yet.</returns>
    public bool TryGetResourceLevel(string resourceId, out float level)
    {
        level = 0f;
        return false;
    }

    /// <summary>
    /// Human-readable label for a species or resource id. Returns the id or empty until filled.
    /// </summary>
    /// <param name="id">Species or resource id.</param>
    /// <returns><paramref name="id"/> when non-empty; otherwise empty string.</returns>
    public string GetDisplayName(string id)
    {
        return string.IsNullOrEmpty(id) ? string.Empty : id;
    }
}
