using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public PopulationDisplay display;
    public CreatureSpawner spawner;

    public float secondsPerTick = 2f;

    [Tooltip("Optional. When assigned and ShouldPause is true, ticks are skipped.")]
    [SerializeField]
    MonoBehaviour pauseGateBehaviour;

    [Tooltip("Optional. When set, one ecosystem day passes each time this clock's day changes, instead of every secondsPerTick.")]
    [SerializeField]
    MonoBehaviour clockBehaviour;

    ISimulationPauseGate _pauseGate;
    IClockProvider _clock;
    float timer;
    private float simulationSpeed = 1f;
    private bool isRunning = true;

    void Awake()
    {
        _pauseGate = pauseGateBehaviour as ISimulationPauseGate;
    }

    void OnEnable()
    {
        _clock = clockBehaviour as IClockProvider;
        if (clockBehaviour != null && _clock == null)
            Debug.LogError($"{nameof(SimulationClock)} clock must implement {nameof(IClockProvider)}.", this);

        if (_clock != null)
            _clock.OnDayChanged += OnClockDay;
    }

    void OnDisable()
    {
        if (_clock != null)
            _clock.OnDayChanged -= OnClockDay;
    }

    void OnClockDay(int _) => Step();

    void Update()
    {
        if (_clock != null)
            return;

        if (!isRunning)
            return;

        if (_pauseGate != null && _pauseGate.ShouldPause)
            return;

        if (simulator == null)
            return;

        timer += Time.deltaTime * simulationSpeed;

        if (timer >= secondsPerTick)
        {
            timer -= secondsPerTick;
            Step();
        }
    }

    void Step()
    {
        if (!isRunning)
            return;

        if (_pauseGate != null && _pauseGate.ShouldPause)
            return;

        if (simulator == null)
            return;

        simulator.Tick();
        if (display != null)
            display.Refresh();
        if (spawner != null)
            spawner.RefreshVisuals();
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
