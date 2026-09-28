using UnityEngine;

public class SimulationSpeedController : MonoBehaviour
{
    public SimulationClock[] clocks;

    public bool IsRunning { get; private set; } = true;
    public float CurrentSpeed { get; private set; } = 1f;

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
        CurrentSpeed = speed;

        foreach (SimulationClock clock in clocks)
        {
            if (clock != null)
                clock.SetSpeed(speed);
        }
    }

    public void Play()
    {
        IsRunning = true;

        foreach (SimulationClock clock in clocks)
        {
            if (clock != null)
                clock.Play();
        }
    }

    public void Pause()
    {
        IsRunning = false;

        foreach (SimulationClock clock in clocks)
        {
            if (clock != null)
                clock.Pause();
        }
    }

    public void Reset()
    {
        CurrentSpeed = 1f;
        IsRunning = true;

        foreach (SimulationClock clock in clocks)
        {
            if (clock != null)
                clock.Reset();
        }
    }
}
