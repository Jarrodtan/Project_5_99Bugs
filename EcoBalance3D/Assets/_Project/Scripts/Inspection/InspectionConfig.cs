using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds a <see cref="SelectableTypeDefinition"/> to an <see cref="InspectableSchema"/> in the central catalog.
/// </summary>
[Serializable]
public struct InspectableSchemaBinding
{
    [Tooltip("Selectable type this schema applies to (e.g. Entity.asset).")]
    public SelectableTypeDefinition type;

    [Tooltip("Schema describing display name and stat rows for that type.")]
    public InspectableSchema schema;
}

/// <summary>
/// Central inspection knobs: schema catalog by selectable type, value format, and debug logging.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Inspection Config", fileName = "InspectionConfig")]
public class InspectionConfig : ScriptableObject
{
    [Header("Schemas")]
    [Tooltip("Catalog of selectable type → inspectable schema. Add a row for each new kind.")]
    public List<InspectableSchemaBinding> schemas = new();

    [Header("Formatting")]
    [Tooltip("Numeric format for live resource values (e.g. F1). Single place for all inspectables.")]
    public string valueFormat = "F1";

    [Header("Debug")]
    [Tooltip("When true, InspectableDebugLogger prints stats on selection change.")]
    public bool logStatsOnSelection = true;

    /// <summary>
    /// Looks up a schema for the given selectable type asset.
    /// </summary>
    /// <param name="type">Type definition to match.</param>
    /// <param name="schema">Matched schema, or null.</param>
    /// <returns>True when a binding was found.</returns>
    public bool TryGetSchema(SelectableTypeDefinition type, out InspectableSchema schema)
    {
        schema = null;
        if (type == null || schemas == null)
            return false;

        for (var i = 0; i < schemas.Count; i++)
        {
            var binding = schemas[i];
            if (binding.type == type && binding.schema != null)
            {
                schema = binding.schema;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Looks up a schema by type id string (matches <see cref="SelectableTypeDefinition.id"/>).
    /// </summary>
    /// <param name="typeId">Type id from a selection handle.</param>
    /// <param name="schema">Matched schema, or null.</param>
    /// <returns>True when a binding was found.</returns>
    public bool TryGetSchema(string typeId, out InspectableSchema schema)
    {
        schema = null;
        if (string.IsNullOrEmpty(typeId) || schemas == null)
            return false;

        for (var i = 0; i < schemas.Count; i++)
        {
            var binding = schemas[i];
            if (binding.type != null &&
                string.Equals(binding.type.id, typeId, StringComparison.Ordinal) &&
                binding.schema != null)
            {
                schema = binding.schema;
                return true;
            }
        }

        return false;
    }
}
