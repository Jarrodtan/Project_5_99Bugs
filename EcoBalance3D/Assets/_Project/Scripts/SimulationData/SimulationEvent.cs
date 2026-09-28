/// <summary>
/// One row in the Display 3 behaviour-event feed.
/// Field shell only — values unused until T02 fills an event provider.
/// Do not seed sample messages; labels/types belong on a future event catalog config.
/// See docs/t02-simulation-data-sync.md.
/// </summary>
public struct SimulationEvent
{
    /// <summary>
    /// Stable event type key (from future event catalog config, not hardcoded in UI).
    /// </summary>
    public string EventTypeId;

    /// <summary>
    /// When the event occurred (sim or wall time — format agreed with T02 later).
    /// </summary>
    public float Timestamp;

    /// <summary>
    /// Human-readable description for the feed row. Leave default/empty until providers fill it;
    /// do not hardcode sample event copy in C#.
    /// </summary>
    public string Description;
}
