using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mock <see cref="ISimulationEventProvider"/> for Display 3 before T02 emits behaviour events.
/// Emits weighted random events from <see cref="MockSimulationEventConfig"/> at random intervals and
/// keeps a capped history in chronological order (oldest first).
/// </summary>
public class MockSimulationEventProvider : MonoBehaviour, ISimulationEventProvider
{
    [Tooltip("Mock event catalog (types, copy, weights, timing).")]
    [SerializeField]
    MockSimulationEventConfig config;

    readonly List<SimulationEvent> history = new();
    float nextEmitTime;

    /// <inheritdoc />
    public IReadOnlyList<SimulationEvent> GetRecentEvents() => history;

    void OnEnable()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(MockSimulationEventProvider)} requires a {nameof(MockSimulationEventConfig)}.", this);
            enabled = false;
            return;
        }

        ScheduleNextEmit();
    }

    void Update()
    {
        if (Time.time < nextEmitTime)
            return;

        Emit();
        ScheduleNextEmit();
    }

    void Emit()
    {
        if (!TryPickTemplate(out var template))
            return;

        history.Add(new SimulationEvent
        {
            EventTypeId = template.eventTypeId,
            Timestamp = Time.time,
            Description = template.description
        });

        var capacity = Mathf.Max(1, config.historyCapacity);
        if (history.Count > capacity)
            history.RemoveRange(0, history.Count - capacity);
    }

    bool TryPickTemplate(out MockEventTemplate picked)
    {
        picked = default;
        if (config.templates == null)
            return false;

        var totalWeight = 0f;
        foreach (var template in config.templates)
        {
            if (template.weight > 0f)
                totalWeight += template.weight;
        }

        if (totalWeight <= 0f)
            return false;

        var roll = Random.value * totalWeight;
        foreach (var template in config.templates)
        {
            if (template.weight <= 0f)
                continue;

            picked = template;
            roll -= template.weight;
            if (roll < 0f)
                break;
        }

        return true;
    }

    void ScheduleNextEmit()
    {
        var min = config.minIntervalSeconds;
        var max = config.maxIntervalSeconds;
        if (max < min)
            (min, max) = (max, min);

        // Floor keeps a zero/negative config from emitting every frame.
        nextEmitTime = Time.time + Mathf.Max(0.05f, Random.Range(min, max));
    }
}
