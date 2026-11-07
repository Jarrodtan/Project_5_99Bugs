using System.Collections.Generic;

/// <summary>
/// Read-only simulation/inspection query API. All UI and inspectable adapters should use this — not EcoSimulator fields directly.
/// </summary>
public interface ISimulationQuery
{
    /// <summary>
    /// Resolves the display name for a registered inspectable handle.
    /// </summary>
    /// <param name="handle">Selection handle.</param>
    /// <param name="displayName">Display name when found.</param>
    /// <returns>True when a source is registered.</returns>
    bool TryGetDisplayName(SelectionHandle handle, out string displayName);

    /// <summary>
    /// Resolves current stats for a registered inspectable handle.
    /// </summary>
    /// <param name="handle">Selection handle.</param>
    /// <param name="stats">Stat rows when found.</param>
    /// <returns>True when a source is registered.</returns>
    bool TryGetStats(SelectionHandle handle, out IReadOnlyList<StatEntry> stats);

    /// <summary>
    /// Reads population for a species definition asset.
    /// </summary>
    /// <param name="species">Species asset.</param>
    /// <param name="population">Population value.</param>
    /// <returns>True when the simulator has an entry.</returns>
    bool TryGetPopulation(SpeciesDefinition species, out float population);

    /// <summary>
    /// Reads current amount for a resource definition asset.
    /// </summary>
    /// <param name="resource">Resource asset.</param>
    /// <param name="amount">Current amount.</param>
    /// <returns>True when the resource ref is valid.</returns>
    bool TryGetResourceAmount(ResourceDefinition resource, out float amount);
}
