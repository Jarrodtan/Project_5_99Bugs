using System;

/// <summary>
/// Read-only day-night clock. HUD and lighting depend on this interface, not on
/// <see cref="ClockService"/>, so a mock or future clock can replace the service.
/// Personal prototype — not Feature Plan / T04 work until approved.
/// </summary>
public interface IClockProvider
{
    /// <summary>Current day number (starts from config starting day).</summary>
    int Day { get; }

    /// <summary>Fraction of the current day, 0–1.</summary>
    float TimeOfDay01 { get; }

    /// <summary>True when the current hour is between sunrise and sunset on the config.</summary>
    bool IsDaytime { get; }

    /// <summary>Raised after midnight rollover with the new day number.</summary>
    event Action<int> OnDayChanged;
}
