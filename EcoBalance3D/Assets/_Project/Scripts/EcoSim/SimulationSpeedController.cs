using UnityEngine;

public class SimulationSpeedController : MonoBehaviour
{
    public SimulationClock[] clocks;

    public void SetSpeed1x()
    {
        SetSpeed(1f);
    }

    public void SetSpeed2x()
    {
        SetSpeed(2f);
    }

    public void SetSpeed4x()
    {
        SetSpeed(4f);
    }

    public void SetSpeed8x()
    {
        SetSpeed(8f);
    }

    private void SetSpeed(float speed)
    {
        foreach (SimulationClock clock in clocks)
        {
            if (clock != null)
            {
                clock.SetSpeed(speed);
            }
        }
    }
}
