using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin resource adapter: registers a <see cref="ResourceDefinitionStatSource"/> from the catalog and reads only via <see cref="ISimulationQuery"/>.
/// </summary>
[DisallowMultipleComponent]
public class ResourceInspectable : MonoBehaviour, IInspectable
{
    [Tooltip("Shared inspection config (schema catalog + format).")]
    [SerializeField]
    InspectionConfig inspectionConfig;

    [Tooltip("Central simulation/inspection query.")]
    [SerializeField]
    SimulationQuery simulationQuery;

    [Tooltip("Selectable type used to resolve the schema from the catalog.")]
    [SerializeField]
    SelectableTypeDefinition typeDefinition;

    [Tooltip("Resource definition providing live values.")]
    [SerializeField]
    ResourceDefinition resourceDefinition;

    SelectionHandle registeredHandle;
    bool registered;

    /// <summary>
    /// Wires shared config/query/type/resource. Prefer calling while the GameObject is inactive.
    /// </summary>
    /// <param name="config">Inspection config with schema catalog.</param>
    /// <param name="query">Simulation query service.</param>
    /// <param name="type">Selectable type for catalog lookup.</param>
    /// <param name="resource">Resource definition for live stats.</param>
    public void Configure(
        InspectionConfig config,
        SimulationQuery query,
        SelectableTypeDefinition type,
        ResourceDefinition resource)
    {
        if (registered)
            UnregisterSource();

        inspectionConfig = config;
        simulationQuery = query;
        typeDefinition = type;
        resourceDefinition = resource;

        if (isActiveAndEnabled)
            RegisterSource();
    }

    /// <inheritdoc />
    public string GetDisplayName()
    {
        var handle = ResolveHandle();
        if (simulationQuery != null && simulationQuery.TryGetDisplayName(handle, out var name))
            return name;
        return string.Empty;
    }

    /// <inheritdoc />
    public IReadOnlyList<StatEntry> GetStats()
    {
        var handle = ResolveHandle();
        if (simulationQuery != null && simulationQuery.TryGetStats(handle, out var stats))
            return stats;
        return System.Array.Empty<StatEntry>();
    }

    void OnEnable() => RegisterSource();

    void Start()
    {
        RegisterSource();
        if (!registered)
            Debug.LogError($"{nameof(ResourceInspectable)} on '{name}' failed to register (Selectable handle missing).", this);
    }

    void OnDisable() => UnregisterSource();

    void RegisterSource()
    {
        if (registered)
            return;

        if (inspectionConfig == null || simulationQuery == null || typeDefinition == null || resourceDefinition == null)
        {
            Debug.LogError(
                $"{nameof(ResourceInspectable)} on '{name}' needs InspectionConfig, SimulationQuery, type, and ResourceDefinition.",
                this);
            return;
        }

        if (!inspectionConfig.TryGetSchema(typeDefinition, out var schema) || schema == null)
        {
            Debug.LogError($"{nameof(ResourceInspectable)}: no schema in catalog for type '{typeDefinition.id}'.", this);
            return;
        }

        var selectable = GetComponent<Selectable>();
        if (selectable == null || !selectable.Handle.IsValid)
            return;

        registeredHandle = selectable.Handle;
        var format = inspectionConfig.valueFormat;
        InspectableStatStore.Register(
            registeredHandle,
            new ResourceDefinitionStatSource(resourceDefinition, schema, format));
        registered = true;
    }

    void UnregisterSource()
    {
        if (!registered)
            return;

        InspectableStatStore.Unregister(registeredHandle);
        registeredHandle = SelectionHandle.None;
        registered = false;
    }

    SelectionHandle ResolveHandle()
    {
        if (registeredHandle.IsValid)
            return registeredHandle;

        var selectable = GetComponent<Selectable>();
        return selectable != null ? selectable.Handle : SelectionHandle.None;
    }
}
