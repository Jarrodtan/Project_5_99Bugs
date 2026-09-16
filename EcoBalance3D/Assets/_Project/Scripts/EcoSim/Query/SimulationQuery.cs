using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sole <see cref="ISimulationQuery"/> implementation: inspectable stats via <see cref="InspectableStatStore"/>,
/// aggregates via <see cref="EcosystemSimulator"/>.
/// </summary>
public class SimulationQuery : MonoBehaviour, ISimulationQuery
{
    [Tooltip("Simulation owner for aggregate population/resource reads.")]
    [SerializeField]
    EcosystemSimulator simulator;

    [Tooltip("Optional. Used only if future query helpers need shared inspection knobs.")]
    [SerializeField]
    InspectionConfig inspectionConfig;

    /// <summary>Assigned inspection config (may be null).</summary>
    public InspectionConfig InspectionConfig => inspectionConfig;

    /// <summary>
    /// Assigns dependencies (e.g. from a bootstrapper). Prefer Inspector wiring in the scene.
    /// </summary>
    /// <param name="ecosystemSimulator">Simulator reference.</param>
    /// <param name="config">Inspection config.</param>
    public void Configure(EcosystemSimulator ecosystemSimulator, InspectionConfig config)
    {
        simulator = ecosystemSimulator;
        inspectionConfig = config;
    }

    /// <inheritdoc />
    public bool TryGetDisplayName(SelectionHandle handle, out string displayName)
    {
        displayName = string.Empty;
        if (!InspectableStatStore.TryGet(handle, out var source))
            return false;

        displayName = source.DisplayName ?? string.Empty;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetStats(SelectionHandle handle, out IReadOnlyList<StatEntry> stats)
    {
        stats = null;
        if (!InspectableStatStore.TryGet(handle, out var source))
            return false;

        stats = source.GetStats();
        return stats != null;
    }

    /// <inheritdoc />
    public bool TryGetPopulation(SpeciesDefinition species, out float population)
    {
        population = 0f;
        if (simulator == null || species == null || simulator.populations == null)
            return false;

        return simulator.populations.TryGetValue(species, out population);
    }

    /// <inheritdoc />
    public bool TryGetResourceAmount(ResourceDefinition resource, out float amount)
    {
        amount = 0f;
        if (resource == null)
            return false;

        amount = resource.currentAmount;
        return true;
    }
}
