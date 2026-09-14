using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public float secondsPerTick = 2f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= secondsPerTick)
        {
            timer = 0f;
            simulator.Tick();
            foreach (var kvp in simulator.populations)
            {
                Debug.Log($"{kvp.Key.speciesName}: {kvp.Value:F1}");
            }
        }
    }
}
