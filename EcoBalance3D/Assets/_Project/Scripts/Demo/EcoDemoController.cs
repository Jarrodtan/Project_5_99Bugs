// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the M1 demo scene: ticks the <see cref="EcosystemSimulator"/>, records population
/// history for the graph, and refreshes the visual layer (grass patches + animal agents).
/// Play / Pause / Reset / speed live here until the team's F02/F06 controls replace them.
/// Runs after the simulator so its Start sees initialised populations.
/// </summary>
[DefaultExecutionOrder(100)]
public class EcoDemoController : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public GrassField grass;
    public List<EcoPopulationView> views = new List<EcoPopulationView>();

    [Min(0.05f)] public float secondsPerTick = 1f;
    [Min(2)] public int historyLength = 150;

    /// <summary>Movement speed multiplier for agents: 0 while paused, otherwise the sim speed.</summary>
    public static float SimSpeed { get; private set; } = 1f;

    public bool Paused { get; private set; }
    public int Speed { get; private set; } = 1;
    public int TickCount { get; private set; }

    /// <summary>Recent values per species / resource, oldest first.</summary>
    public readonly Dictionary<ScriptableObject, List<float>> History = new Dictionary<ScriptableObject, List<float>>();

    float timer;

    void Start()
    {
        SimSpeed = 1f;
        RecordAndRefresh();
    }

    void OnDestroy() => SimSpeed = 1f;

    void Update()
    {
        if (Paused || simulator == null) return;

        timer += Time.deltaTime * Speed;
        while (timer >= secondsPerTick)
        {
            timer -= secondsPerTick;
            Step();
        }
    }

    /// <summary>Advances the simulation by one tick and updates visuals.</summary>
    public void Step()
    {
        if (simulator == null) return;
        simulator.Tick();
        TickCount++;
        RecordAndRefresh();
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        SimSpeed = paused ? 0f : Speed;
    }

    public void SetSpeed(int speed)
    {
        Speed = Mathf.Max(1, speed);
        if (!Paused) SimSpeed = Speed;
    }

    public void ResetSimulation()
    {
        if (simulator == null) return;
        simulator.ResetEcosystem();
        TickCount = 0;
        timer = 0f;
        foreach (var series in History.Values) series.Clear();
        foreach (var view in views)
            if (view != null) view.Clear();
        RecordAndRefresh();
    }

    void RecordAndRefresh()
    {
        if (simulator == null) return;

        if (simulator.species != null)
            foreach (var s in simulator.species)
                if (s != null && simulator.populations.TryGetValue(s, out float pop))
                    Record(s, pop);

        if (simulator.resources != null)
            foreach (var r in simulator.resources)
                if (r != null) Record(r, r.currentAmount);

        if (grass != null) grass.Refresh();
        foreach (var view in views)
            if (view != null) view.Refresh();
    }

    void Record(ScriptableObject key, float value)
    {
        if (!History.TryGetValue(key, out var series))
            History[key] = series = new List<float>();
        series.Add(value);
        if (series.Count > historyLength)
            series.RemoveRange(0, series.Count - historyLength);
    }
}
// [AI-GENERATED END]
