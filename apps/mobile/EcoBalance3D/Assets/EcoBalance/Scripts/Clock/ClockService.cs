using System;
using UnityEngine;

/// <summary>
/// Owns the only mutable day-night time. Ticks with scaled sim delta time.
/// HUD and lighting read this through <see cref="IClockProvider"/> only.
/// Personal prototype — not Feature Plan / T04 work until approved.
/// </summary>
public class ClockService : MonoBehaviour, IClockProvider
{
    [Tooltip("Clock hub. Required.")]
    [SerializeField]
    ClockConfig config;

    [Tooltip("Optional. Play/pause and speed. When empty, ticks at 1× while fallbackRunning is true.")]
    [SerializeField]
    SimulationSpeedController speedController;

    [Tooltip("Optional. When assigned and ShouldPause is true, the clock freezes.")]
    [SerializeField]
    MonoBehaviour pauseGateBehaviour;

    [Tooltip("Used only when speedController is empty.")]
    [SerializeField]
    bool fallbackRunning = true;

    ISimulationPauseGate _pauseGate;
    float _timeOfDay;
    int _day;
    bool _loggedMissingConfig;

    /// <inheritdoc />
    public int Day => _day;

    /// <inheritdoc />
    public float TimeOfDay01 => _timeOfDay;

    /// <inheritdoc />
    public bool IsDaytime
    {
        get
        {
            if (config == null)
                return true;

            var hour = _timeOfDay * 24f;
            return hour >= config.sunriseHour && hour < config.sunsetHour;
        }
    }

    /// <inheritdoc />
    public event Action<int> OnDayChanged;

    /// <summary>Real seconds in one game day at 1x (ClockConfig.dayLengthSeconds); animal timings scale with it.</summary>
    public static float DaySeconds { get; private set; } = 60f;

    void Awake()
    {
        _pauseGate = pauseGateBehaviour as ISimulationPauseGate;
        if (config != null)
        {
            config.EnsureDefaults();
            DaySeconds = config.dayLengthSeconds;
        }
        ApplyStartState();
    }

    void Update()
    {
        if (config == null)
        {
            if (!_loggedMissingConfig)
            {
                Debug.LogWarning($"{nameof(ClockService)} needs a {nameof(ClockConfig)}.", this);
                _loggedMissingConfig = true;
            }

            return;
        }

        if (!IsEffectivelyRunning())
            return;

        var speed = speedController != null ? speedController.CurrentSpeed : 1f;
        var dt = Time.deltaTime * speed;
        if (dt <= 0f)
            return;

        _timeOfDay += dt / config.dayLengthSeconds;
        while (_timeOfDay >= 1f)
        {
            _timeOfDay -= 1f;
            _day++;
            OnDayChanged?.Invoke(_day);
        }
    }

    /// <summary>
    /// Returns to config starting day and start time of day. Does not change sim speed.
    /// </summary>
    public void Reset()
    {
        ApplyStartState();
    }

    /// <summary>SCRUM-375: jump to a saved day and time of day (Guided Continue).</summary>
    public void Restore(int day, float timeOfDay01)
    {
        _day = Mathf.Max(1, day);
        _timeOfDay = Mathf.Clamp01(timeOfDay01);
        OnDayChanged?.Invoke(_day);
    }

    void ApplyStartState()
    {
        if (config == null)
        {
            _day = 1;
            _timeOfDay = 0f;
            return;
        }

        _day = config.startingDay;
        _timeOfDay = Mathf.Clamp01(config.startTimeOfDay);
    }

    bool IsEffectivelyRunning()
    {
        if (_pauseGate != null && _pauseGate.ShouldPause)
            return false;

        if (speedController != null)
            return speedController.IsRunning;

        return fallbackRunning;
    }
}
