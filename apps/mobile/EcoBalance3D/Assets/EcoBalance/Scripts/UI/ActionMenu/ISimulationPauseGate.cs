/// <summary>
/// Optional pause signal for <see cref="SimulationClock"/>. The clock does not reference the menu type.
/// </summary>
public interface ISimulationPauseGate
{
    /// <summary>True when the clock should skip ticks.</summary>
    bool ShouldPause { get; }
}
