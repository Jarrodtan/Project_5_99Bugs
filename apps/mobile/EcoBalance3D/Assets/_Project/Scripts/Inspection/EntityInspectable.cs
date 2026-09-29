using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin entity adapter: registers a <see cref="StaticStatSource"/> from the inspection catalog and reads only via <see cref="ISimulationQuery"/>.
/// </summary>
[DisallowMultipleComponent]
public class EntityInspectable : MonoBehaviour, IInspectable
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

    SelectionHandle registeredHandle;
    bool registered;

    /// <summary>
    /// Wires shared config/query/type. Prefer calling while the GameObject is inactive.
    /// </summary>
    /// <param name="config">Inspection config with schema catalog.</param>
    /// <param name="query">Simulation query service.</param>
    /// <param name="type">Selectable type for catalog lookup.</param>
    public void Configure(InspectionConfig config, SimulationQuery query, SelectableTypeDefinition type)
    {
        if (registered)
            UnregisterSource();

        inspectionConfig = config;
        simulationQuery = query;
        typeDefinition = type;

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
            Debug.LogError($"{nameof(EntityInspectable)} on '{name}' failed to register (Selectable handle missing).", this);
    }

    void OnDisable() => UnregisterSource();

    void RegisterSource()
    {
        if (registered)
            return;

        if (inspectionConfig == null || simulationQuery == null || typeDefinition == null)
        {
            Debug.LogError($"{nameof(EntityInspectable)} on '{name}' needs InspectionConfig, SimulationQuery, and type.", this);
            return;
        }

        if (!inspectionConfig.TryGetSchema(typeDefinition, out var schema) || schema == null)
        {
            Debug.LogError($"{nameof(EntityInspectable)}: no schema in catalog for type '{typeDefinition.id}'.", this);
            return;
        }

        var selectable = GetComponent<Selectable>();
        if (selectable == null || !selectable.Handle.IsValid)
            return;

        registeredHandle = selectable.Handle;
        InspectableStatStore.Register(registeredHandle, new StaticStatSource(schema));
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
