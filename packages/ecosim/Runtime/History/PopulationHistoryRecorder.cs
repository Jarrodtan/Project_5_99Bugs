// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Records the simulator's populations and resources into a <see cref="PopulationHistory"/>
/// after every tick, and restarts the history (with the starting state as tick 0) on reset.
/// Put it next to the EcosystemSimulator; the graph and case detector read <see cref="History"/>.
/// </summary>
public class PopulationHistoryRecorder : MonoBehaviour
{
    [Tooltip("Simulator to record. Defaults to one on this GameObject.")]
    public EcosystemSimulator simulator;

    [Tooltip("How many days are kept; older days are dropped first.")]
    [Min(1)] public int capacity = 1000;

    PopulationHistory history;
    EcosystemSimulator subscribed;

    public PopulationHistory History => history ??= new PopulationHistory(capacity);

    void OnEnable()
    {
        if (simulator == null) simulator = GetComponent<EcosystemSimulator>();
        if (simulator == null)
        {
            Debug.LogWarning($"[{name}] PopulationHistoryRecorder has no EcosystemSimulator to record.", this);
            return;
        }
        Bind(simulator);
    }

    void OnDisable() => Unbind();

    /// <summary>Starts recording <paramref name="sim"/> (also used by tests).</summary>
    public void Bind(EcosystemSimulator sim)
    {
        Unbind();
        subscribed = sim;
        sim.Ticked += RecordNow;
        sim.EcosystemReset += Restart;
        // Start() may already have reset the simulator before we subscribed.
        if (History.Count == 0 && sim.populations.Count > 0) RecordNow();
    }

    void Unbind()
    {
        if (subscribed == null) return;
        subscribed.Ticked -= RecordNow;
        subscribed.EcosystemReset -= Restart;
        subscribed = null;
    }

    void Restart()
    {
        History.Clear();
        RecordNow();
    }

    void RecordNow()
    {
        var sim = subscribed;
        var values = new Dictionary<string, float>();
        foreach (var kv in sim.populations)
            if (kv.Key != null) values[kv.Key.name] = kv.Value;
        if (sim.resources != null)
            foreach (var r in sim.resources)
                if (r != null) values[r.name] = r.currentAmount;
        History.Add(sim.TickCount, values);
    }
}
// [AI-GENERATED END]
