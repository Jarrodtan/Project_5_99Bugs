using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public PopulationDisplay display;
    public CreatureSpawner spawner;

    public float secondsPerTick = 2f;

    private float timer;
    private float simulationSpeed = 1f;

    void Update()
    {
        timer += Time.deltaTime * simulationSpeed;

        if (timer >= secondsPerTick)
        {
            timer -= secondsPerTick;

            simulator.Tick();
            display.Refresh();
            spawner.RefreshVisuals();
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
}
