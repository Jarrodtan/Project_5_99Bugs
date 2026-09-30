using UnityEngine;

public class SimulationSpeedController : MonoBehaviour
{
    public SimulationClock[] clocks;
    public AudioSource[] audioSources;

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

        if (clocks != null)
        {
            foreach (SimulationClock clock in clocks)
            {
                if (clock != null)
                    clock.SetSpeed(speed);
            }
        }
    }

    public void Play()
    {
        IsRunning = true;

        // Resume simulation clocks
        if (clocks != null)
        {
            foreach (SimulationClock clock in clocks)
            {
                if (clock != null)
                    clock.Play();
            }
        }

        // Resume audio
        if (audioSources != null)
        {
            foreach (AudioSource audio in audioSources)
            {
                if (audio != null)
                    audio.UnPause();
            }
        }
    }

    public void Pause()
    {
        IsRunning = false;

        // Pause simulation clocks
        if (clocks != null)
        {
            foreach (SimulationClock clock in clocks)
            {
                if (clock != null)
                    clock.Pause();
            }
        }

        // Pause audio
        if (audioSources != null)
        {
            foreach (AudioSource audio in audioSources)
            {
                if (audio != null)
                    audio.Pause();
            }
        }
    }

    public void Reset()
    {
        IsRunning = true;
        CurrentSpeed = 1f;

        // Reset simulation clocks
        if (clocks != null)
        {
            foreach (SimulationClock clock in clocks)
            {
                if (clock != null)
                    clock.Reset();
            }
        }

        // Resume audio
        if (audioSources != null)
        {
            foreach (AudioSource audio in audioSources)
            {
                if (audio != null)
                    audio.UnPause();
            }
        }
    }
}
