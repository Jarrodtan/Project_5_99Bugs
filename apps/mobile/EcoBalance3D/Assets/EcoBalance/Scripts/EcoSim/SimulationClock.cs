using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public PopulationDisplay display;
    public CreatureSpawner spawner;
    public DataLogging dataLogging;

    public float secondsPerTick = 2f;

    [Tooltip("Optional. When assigned and ShouldPause is true, ticks are skipped.")]
    [SerializeField]
    MonoBehaviour pauseGateBehaviour;

    ISimulationPauseGate _pauseGate;
    float timer;
    private float simulationSpeed = 1f;
    private bool isRunning = true;
    private int check = 0;

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

        timer += Time.deltaTime * simulationSpeed;

        if (timer >= secondsPerTick)
        {
            timer -= secondsPerTick;
            check++;

            simulator.Tick();
            if (display != null)
                display.Refresh();
            if (spawner != null)
                spawner.RefreshVisuals();
            if (dataLogging != null)
            {
                int sheepCount = 0;
                int leopardCount = 0;
                int treeCount = 0;

                if (Terrain.activeTerrain != null && Terrain.activeTerrain.terrainData != null)
                {
                    treeCount += Terrain.activeTerrain.terrainData.treeInstanceCount;
                }

                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("BlueSheep")) sheepCount++;
                    else if (obj.name.Contains("SnowLeopard")) leopardCount++;
                    else if (obj.name.Contains("Tree") || obj.name.Contains("Pinus")) treeCount++;
                }

                dataLogging.RecordTick(check, sheepCount, leopardCount, treeCount);
            }
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
        check = 0;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): null-safe so Reset works with any slot left empty.
        if (simulator != null)
            simulator.ResetSimulation();
        if (display != null)
            display.Refresh();
        if (spawner != null)
            spawner.ResetVisuals();
    }
}
