using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public PopulationDisplay display;
    public float secondsPerTick = 2f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= secondsPerTick)
        {
            timer = 0f;
            simulator.Tick();
            display.Refresh();
        }
    }
}
