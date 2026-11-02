using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ticks an <see cref="EcosystemSimulator"/> every <see cref="secondsPerTick"/> (scaled by the sim speed) and
/// refreshes its spawner / display after each tick. Several clocks can share one simulator (AnimalCreator gives
/// every species spawner its own clock): only one of them ticks it, the others just refresh on its ticks.
/// </summary>
public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public PopulationDisplay display;
    public CreatureSpawner spawner;

    public float secondsPerTick = 2f;

    [Tooltip("Optional. When assigned and ShouldPause is true, ticks are skipped.")]
    [SerializeField]
    MonoBehaviour pauseGateBehaviour;

    ISimulationPauseGate _pauseGate;
    float timer;
    private float simulationSpeed = 1f;
    private bool isRunning = true;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // The clock that ticks each simulator. HimalayaSimulation has one clock per species spawner, and each
    // used to tick the simulator itself: two ticks every 2 s, so the history, the graph and the case
    // windows ran at double rate.
    static readonly Dictionary<EcosystemSimulator, SimulationClock> drivers = new();
    EcosystemSimulator listening;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDrivers() => drivers.Clear();   // domain reload is off in this project

    /// <summary>This clock is the one ticking its simulator (the first enabled clock for it).</summary>
    public bool DrivesSimulator
    {
        get
        {
            if (simulator == null || !isActiveAndEnabled) return false;
            if (!drivers.TryGetValue(simulator, out var d) || d == null || !d.isActiveAndEnabled)
                drivers[simulator] = d = this;
            return d == this;
        }
    }

    void OnEnable() => Listen();

    void OnDisable()
    {
        if (listening != null)
        {
            listening.Ticked -= OnTicked;
            if (drivers.TryGetValue(listening, out var d) && d == this) drivers.Remove(listening);
        }
        listening = null;
    }

    // Every clock on the simulator refreshes its own spawner / display after each tick, whoever ticked.
    void Listen()
    {
        if (listening == simulator) return;
        OnDisable();
        if (simulator == null) return;
        listening = simulator;
        listening.Ticked += OnTicked;
    }

    void OnTicked()
    {
        if (display != null)
            display.Refresh();
        if (spawner != null)
            spawner.RefreshVisuals();
    }
    // [AI-GENERATED END]

    void Awake()
    {
        _pauseGate = pauseGateBehaviour as ISimulationPauseGate;
    }

    void Update()
    {
        if (!isRunning)
            return;

        if (_pauseGate != null && _pauseGate.ShouldPause)
            return;

        if (simulator == null)
            return;

        Listen();   // in case the simulator was assigned after OnEnable
        if (!DrivesSimulator)
            return;

        timer += Time.deltaTime * simulationSpeed;

        if (timer >= secondsPerTick)
        {
            timer -= secondsPerTick;
            simulator.Tick();   // OnTicked refreshes the spawners and displays
        }
    }

    public void SetSpeed(float speed)
    {
        simulationSpeed = speed;
    }

    public void SetSpeed1x()
    {
        simulationSpeed = 1f;
    }

    public void SetSpeed2x()
    {
        simulationSpeed = 2f;
    }

    public void SetSpeed4x()
    {
        simulationSpeed = 4f;
    }

    public void SetSpeed8x()
    {
        simulationSpeed = 8f;
    }

    public void Play()
    {
        isRunning = true;
    }

    public void Pause()
    {
        isRunning = false;
    }

    public void Reset()
    {
        timer = 0f;
        simulationSpeed = 1f;
        isRunning = true;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): null-safe so Reset works with any slot left empty.
        if (simulator != null)
            simulator.ResetSimulation();
        if (display != null)
            display.Refresh();
        if (spawner != null)
            spawner.ResetVisuals();
    }
}
