using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live <see cref="ISimulationEventProvider"/> for the Events tab: listens to what the animals report to
/// <see cref="EcosystemSimulator"/> (catches, eating, starving) and keeps the most recent ones as feed rows.
/// Only Events mode produces these; in Formula mode the feed stays empty.
/// </summary>
public class EcosystemSimulationEventProvider : MonoBehaviour, ISimulationEventProvider
{
    [Tooltip("Live ecosystem simulator to listen to.")]
    [SerializeField]
    EcosystemSimulator simulator;

    [Tooltip("How many recent events to keep. The feed shows its own maxVisibleEntries of these.")]
    [Min(1)]
    [SerializeField]
    int maxEvents = 20;

    [Header("Text")]
    [Tooltip("{0} = predator, {1} = prey.")]
    [SerializeField]
    string predationFormat = "{0} caught a {1}";

    [Tooltip("Eats a resource. {0} = species, {1} = resource.")]
    [SerializeField]
    string eatFormat = "{0} ate {1}";

    [Tooltip("Needs a resource (e.g. Water). {0} = species, {1} = resource.")]
    [SerializeField]
    string needFormat = "{0} drank {1}";

    [Tooltip("{0} = species.")]
    [SerializeField]
    string starvationFormat = "{0} starved";

    [Tooltip("Added when a species' population reaches zero. {0} = species.")]
    [SerializeField]
    string extinctionFormat = "{0} went extinct";

    readonly List<SimulationEvent> events = new();
    float lastTimestamp = float.NegativeInfinity;

    /// <summary>
    /// Recent events, oldest first.
    /// </summary>
    /// <returns>Read-only list of events; never null.</returns>
    public IReadOnlyList<SimulationEvent> GetRecentEvents() => events;

    void OnEnable()
    {
        if (simulator == null)
        {
            Debug.LogError($"{nameof(EcosystemSimulationEventProvider)} requires an {nameof(EcosystemSimulator)}.", this);
            return;
        }

        simulator.PredationRecorded += OnPredation;
        simulator.EatRecorded += OnEat;
        simulator.StarvationRecorded += OnStarvation;
    }

    void OnDisable()
    {
        if (simulator == null)
            return;

        simulator.PredationRecorded -= OnPredation;
        simulator.EatRecorded -= OnEat;
        simulator.StarvationRecorded -= OnStarvation;
    }

    void OnPredation(SpeciesDefinition predator, SpeciesDefinition prey)
    {
        Add("predation", string.Format(predationFormat, NameOf(predator), NameOf(prey)));
        CheckExtinct(prey);
    }

    void OnEat(SpeciesDefinition eater, ResourceDefinition resource)
    {
        // Uses the species' relationship to the resource (Eats Grass vs Needs Water) to pick the wording.
        var relationship = eater.FindRelationship(resource);
        var isNeed = relationship != null && relationship.type == RelationshipType.Needs;
        Add(isNeed ? "need" : "eat", string.Format(isNeed ? needFormat : eatFormat, NameOf(eater), NameOf(resource)));
    }

    void OnStarvation(SpeciesDefinition species)
    {
        Add("starvation", string.Format(starvationFormat, NameOf(species)));
        CheckExtinct(species);
    }

    void CheckExtinct(SpeciesDefinition species)
    {
        if (simulator.populations.TryGetValue(species, out var count) && count < 1f)
            Add("extinction", string.Format(extinctionFormat, NameOf(species)));
    }

    void Add(string eventTypeId, string description)
    {
        // The feed only treats strictly newer timestamps as new, so same-frame events get nudged apart.
        var timestamp = Mathf.Max(Time.time, lastTimestamp + 0.01f);
        lastTimestamp = timestamp;

        events.Add(new SimulationEvent { EventTypeId = eventTypeId, Timestamp = timestamp, Description = description });
        if (events.Count > maxEvents)
            events.RemoveRange(0, events.Count - maxEvents);
    }

    static string NameOf(SpeciesDefinition species) =>
        !string.IsNullOrEmpty(species.speciesName) ? species.speciesName : species.name;

    static string NameOf(ResourceDefinition resource) =>
        !string.IsNullOrEmpty(resource.resourceName) ? resource.resourceName : resource.name;
}
