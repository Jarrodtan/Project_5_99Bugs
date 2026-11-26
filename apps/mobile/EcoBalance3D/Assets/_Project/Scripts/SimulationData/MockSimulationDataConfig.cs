using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One mock species row: id, display name, and population roll range.
/// </summary>
[Serializable]
public struct MockSpeciesEntry
{
    [Tooltip("Stable species id used by ISimulationDataProvider (e.g. blue_sheep).")]
    public string id;

    [Tooltip("Label shown in UI. When empty, GetDisplayName returns the id.")]
    public string displayName;

    [Tooltip("Inclusive minimum population when the mock re-rolls.")]
    public float minPopulation;

    [Tooltip("Inclusive maximum population when the mock re-rolls.")]
    public float maxPopulation;
}

/// <summary>
/// One mock resource row: id, display name, and level roll range.
/// </summary>
[Serializable]
public struct MockResourceEntry
{
    [Tooltip("Stable resource id used by ISimulationDataProvider (e.g. grass).")]
    public string id;

    [Tooltip("Label shown in UI. When empty, GetDisplayName returns the id.")]
    public string displayName;

    [Tooltip("Inclusive minimum level when the mock re-rolls.")]
    public float minLevel;

    [Tooltip("Inclusive maximum level when the mock re-rolls.")]
    public float maxLevel;
}

/// <summary>
/// Central catalog for <see cref="MockSimulationDataProvider"/>.
/// All mock ids, labels, ranges, and refresh timing live here — not in provider C#.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Mock Simulation Data Config", fileName = "MockSimulationDataConfig")]
public class MockSimulationDataConfig : ScriptableObject
{
    [Header("Catalog")]
    [Tooltip("Species ids and population ranges for the mock provider.")]
    public List<MockSpeciesEntry> species = new();

    [Tooltip("Resource ids and level ranges for the mock provider.")]
    public List<MockResourceEntry> resources = new();

    [Header("Behaviour")]
    [Tooltip("Seconds between re-rolls of mock values. Values at or below zero re-roll only on Start.")]
    public float refreshIntervalSeconds = 2f;
}
