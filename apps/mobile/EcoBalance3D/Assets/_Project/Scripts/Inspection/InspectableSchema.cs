using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identifies which <see cref="ResourceDefinition"/> field a schema binding reads.
/// Used as a data key on assets — not as UI text.
/// </summary>
public enum ResourceStatField
{
    CurrentAmount = 0,
    RegenRate = 1,
    MaxAmount = 2
}

/// <summary>
/// One static label/value row authored on an inspectable schema (e.g. entity demo stats).
/// </summary>
[Serializable]
public struct StaticStatRow
{
    [Tooltip("Row label shown in the inspector (e.g. Hunger).")]
    public string label;

    [Tooltip("Starting / static value string for this row.")]
    public string value;
}

/// <summary>
/// Maps a display label to a live resource definition field.
/// </summary>
[Serializable]
public struct ResourceStatBinding
{
    [Tooltip("Row label shown in the inspector (e.g. Level).")]
    public string label;

    [Tooltip("Which ResourceDefinition field supplies the value.")]
    public ResourceStatField field;
}

/// <summary>
/// Data asset describing display name and which stats an inspectable kind exposes.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Inspectable Schema", fileName = "InspectableSchema")]
public class InspectableSchema : ScriptableObject
{
    [Tooltip("Display name override. When empty, adapters may fall back to a linked definition name.")]
    public string displayName;

    [Tooltip("Static label/value rows (entity / stub kinds).")]
    public List<StaticStatRow> staticStats = new();

    [Tooltip("Live bindings into a ResourceDefinition (resource kinds).")]
    public List<ResourceStatBinding> resourceBindings = new();
}
