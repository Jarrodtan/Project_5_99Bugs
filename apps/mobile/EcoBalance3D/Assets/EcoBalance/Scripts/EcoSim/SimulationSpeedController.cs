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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>The speeds the speed buttons step through.</summary>
    public static readonly float[] Steps = { 1f, 2f, 4f, 8f };

    /// <summary>1x -> 2x -> 4x -> 8x -> 1x. Shared by the HUD and the Field Journal graph, so both stay in step.</summary>
    public float CycleSpeed()
    {
        int i = System.Array.IndexOf(Steps, CurrentSpeed);
        SetSpeed(Steps[(i + 1) % Steps.Length]);
        return CurrentSpeed;
    }
    // [AI-GENERATED END]

    public void SetSpeed(float speed)
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
