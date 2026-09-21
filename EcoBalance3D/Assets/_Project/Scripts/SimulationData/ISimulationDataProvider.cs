using System.Collections.Generic;

/// <summary>
/// Read-only access to ecosystem-wide simulation aggregates for Displays 1–3.
/// Uses string ids — never <see cref="SpeciesDefinition"/>, <see cref="ResourceDefinition"/>, or GameObject pointers.
/// Distinct from <see cref="ISimulationQuery"/> (per-selection inspection).
/// </summary>
public interface ISimulationDataProvider
{
    /// <summary>
    /// Stable species ids present in the current data source (catalog or live sim).
    /// </summary>
    /// <returns>Read-only list of species id strings; never null.</returns>
    IReadOnlyList<string> GetSpeciesIds();

    /// <summary>
    /// Stable resource ids present in the current data source.
    /// </summary>
    /// <returns>Read-only list of resource id strings; never null.</returns>
    IReadOnlyList<string> GetResourceIds();

    /// <summary>
    /// Reads the current population count for a species id.
    /// </summary>
    /// <param name="speciesId">Species key (e.g. from <see cref="GetSpeciesIds"/>).</param>
    /// <param name="count">Population when found.</param>
    /// <returns>False when the id is unknown or the provider is not ready.</returns>
    bool TryGetPopulationCount(string speciesId, out float count);

    /// <summary>
    /// Reads the current resource level for a resource id.
    /// </summary>
    /// <param name="resourceId">Resource key (e.g. from <see cref="GetResourceIds"/>).</param>
    /// <param name="level">Level when found.</param>
    /// <returns>False when the id is unknown or the provider is not ready.</returns>
    bool TryGetResourceLevel(string resourceId, out float level);

    /// <summary>
    /// Human-readable label for a species or resource id.
    /// </summary>
    /// <param name="id">Species or resource id.</param>
    /// <returns>Display name, or <paramref name="id"/> when no name is configured.</returns>
    string GetDisplayName(string id);
}
