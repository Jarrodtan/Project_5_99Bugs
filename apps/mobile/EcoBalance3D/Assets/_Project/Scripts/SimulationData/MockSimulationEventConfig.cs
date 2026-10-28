using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One mock event the provider can emit: type key, feed text, and how often it is picked.
/// </summary>
[Serializable]
public struct MockEventTemplate
{
    [Tooltip("Stable event type key (e.g. predation, grazing). Carried into SimulationEvent.EventTypeId.")]
    public string eventTypeId;

    [Tooltip("Feed text for this event (e.g. Snow Leopard hunted a Blue Sheep).")]
    public string description;

    [Tooltip("Relative pick chance. Zero or negative never picks this template.")]
    public float weight;
}

/// <summary>
/// Central catalog for <see cref="MockSimulationEventProvider"/>.
/// All mock event types, copy, weights, and timing live here — not in provider C#.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Mock Simulation Event Config", fileName = "MockSimulationEventConfig")]
public class MockSimulationEventConfig : ScriptableObject
{
    [Header("Catalog")]
    [Tooltip("Events the mock picks from, weighted by MockEventTemplate.weight.")]
    public List<MockEventTemplate> templates = new();

    [Header("Behaviour")]
    [Tooltip("Shortest gap between emitted events (seconds, simulation time).")]
    public float minIntervalSeconds = 0.8f;

    [Tooltip("Longest gap between emitted events (seconds, simulation time).")]
    public float maxIntervalSeconds = 2.5f;

    [Tooltip("Events kept in history; the oldest are dropped beyond this.")]
    public int historyCapacity = 50;
}
