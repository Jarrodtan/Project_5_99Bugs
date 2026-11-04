using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Live stats from a <see cref="ResourceDefinition"/> using schema bindings and a shared value format.
/// </summary>
public sealed class ResourceDefinitionStatSource : IInspectableStatSource
{
    readonly ResourceDefinition resource;
    readonly InspectableSchema schema;
    readonly string valueFormat;
    readonly string displayName;
    readonly List<StatEntry> buffer = new();

    /// <summary>
    /// Creates a live resource stat source.
    /// </summary>
    /// <param name="resource">Resource definition to read.</param>
    /// <param name="schema">Schema with resourceBindings and optional displayName.</param>
    /// <param name="valueFormat">Numeric format from InspectionConfig.</param>
    public ResourceDefinitionStatSource(
        ResourceDefinition resource,
        InspectableSchema schema,
        string valueFormat)
    {
        this.resource = resource;
        this.schema = schema;
        this.valueFormat = string.IsNullOrEmpty(valueFormat) ? "G" : valueFormat;

        if (schema != null && !string.IsNullOrEmpty(schema.displayName))
            displayName = schema.displayName;
        else if (resource != null)
            displayName = resource.resourceName;
        else
            displayName = string.Empty;
    }

    /// <inheritdoc />
    public string DisplayName => displayName;

    /// <inheritdoc />
    public IReadOnlyList<StatEntry> GetStats()
    {
        buffer.Clear();
        if (resource == null || schema?.resourceBindings == null)
            return buffer;

        for (var i = 0; i < schema.resourceBindings.Count; i++)
        {
            var binding = schema.resourceBindings[i];
            var value = FormatField(binding.field);
            buffer.Add(new StatEntry(binding.label, value));
        }

        return buffer;
    }

    string FormatField(ResourceStatField field)
    {
        float number;
        switch (field)
        {
            case ResourceStatField.CurrentAmount:
                number = resource.currentAmount;
                break;
            case ResourceStatField.RegenRate:
                number = resource.regenRate;
                break;
            case ResourceStatField.MaxAmount:
                number = resource.maxAmount;
                break;
            default:
                return string.Empty;
        }

        return number.ToString(valueFormat, CultureInfo.InvariantCulture);
    }
}
