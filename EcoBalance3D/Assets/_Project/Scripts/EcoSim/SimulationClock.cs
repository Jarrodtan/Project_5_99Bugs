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

    ISimulationPauseGate _pauseGate;
    float timer;

    void Awake()
    {
        _pauseGate = pauseGateBehaviour as ISimulationPauseGate;
    }

    void Update()
    {
        if (_pauseGate != null && _pauseGate.ShouldPause)
            return;

        if (simulator == null)
            return;

        timer += Time.deltaTime;
        if (timer >= secondsPerTick)
        {
            timer = 0f;
            simulator.Tick();
            if (display != null)
                display.Refresh();
            if (spawner != null)
                spawner.RefreshVisuals();
        }
    }
}
