using System.Collections.Generic;

/// <summary>
/// Snapshot of one Display 3 feed row. Produced by <see cref="EventFeedReader"/>.
/// </summary>
public readonly struct EventFeedEntry
{
    /// <summary>Stable event type key from <see cref="SimulationEvent.EventTypeId"/>.</summary>
    public readonly string EventTypeId;

    /// <summary>Feed text; falls back to the type key when the event has no description.</summary>
    public readonly string Text;

    /// <summary>When the event occurred, as reported by the provider.</summary>
    public readonly float Timestamp;

    /// <summary>
    /// Time the reader first saw this event (the <c>now</c> passed to <see cref="EventFeedReader.Refresh"/>).
    /// Negative infinity for events already present on the first refresh, so they don't highlight.
    /// </summary>
    public readonly float SeenAt;

    public EventFeedEntry(string eventTypeId, string text, float timestamp, float seenAt)
    {
        EventTypeId = eventTypeId;
        Text = text;
        Timestamp = timestamp;
        SeenAt = seenAt;
    }
}

/// <summary>
/// Turns an <see cref="ISimulationEventProvider"/> history into the visible feed: newest first,
/// capped to a fixed count, with the arrival time of each entry so the view can highlight new ones.
/// </summary>
public class EventFeedReader
{
    List<EventFeedEntry> entries = new();
    List<EventFeedEntry> previous = new();
    readonly List<(SimulationEvent evt, int index)> scratch = new();
    float newestSeenTimestamp = float.NegativeInfinity;
    bool primed;

    /// <summary>
    /// Visible entries, newest first. At most the <c>maxVisible</c> passed to the last refresh.
    /// </summary>
    public IReadOnlyList<EventFeedEntry> Entries => entries;

    /// <summary>
    /// Forgets what has been seen, so the next refresh shows the current history without highlights.
    /// </summary>
    public void Reset()
    {
        entries.Clear();
        previous.Clear();
        newestSeenTimestamp = float.NegativeInfinity;
        primed = false;
    }

    /// <summary>
    /// Reads the provider and rebuilds <see cref="Entries"/>.
    /// </summary>
    /// <param name="provider">Event source; null empties the feed.</param>
    /// <param name="maxVisible">Cap on visible entries; zero or less shows none.</param>
    /// <param name="now">Current time, stamped on entries that are new since the last refresh.</param>
    /// <returns>Number of entries that arrived since the last refresh.</returns>
    public int Refresh(ISimulationEventProvider provider, int maxVisible, float now)
    {
        scratch.Clear();
        var events = provider?.GetRecentEvents();
        if (events != null)
        {
            for (var i = 0; i < events.Count; i++)
                scratch.Add((events[i], i));
        }

        // Newest first; for equal timestamps the later list index is treated as newer.
        scratch.Sort((a, b) =>
        {
            var byTime = b.evt.Timestamp.CompareTo(a.evt.Timestamp);
            return byTime != 0 ? byTime : b.index.CompareTo(a.index);
        });

        var newCount = 0;
        if (primed)
        {
            while (newCount < scratch.Count && scratch[newCount].evt.Timestamp > newestSeenTimestamp)
                newCount++;
        }

        // Entries after the new ones are the previous feed shifted down; carry their arrival time over.
        (previous, entries) = (entries, previous);
        entries.Clear();
        var visible = maxVisible > 0 ? System.Math.Min(maxVisible, scratch.Count) : 0;
        for (var i = 0; i < visible; i++)
        {
            var evt = scratch[i].evt;
            float seenAt;
            if (i < newCount)
                seenAt = now;
            else if (i - newCount < previous.Count)
                seenAt = previous[i - newCount].SeenAt;
            else
                seenAt = float.NegativeInfinity;

            var text = string.IsNullOrEmpty(evt.Description) ? evt.EventTypeId ?? string.Empty : evt.Description;
            entries.Add(new EventFeedEntry(evt.EventTypeId, text, evt.Timestamp, seenAt));
        }

        if (scratch.Count > 0 && scratch[0].evt.Timestamp > newestSeenTimestamp)
            newestSeenTimestamp = scratch[0].evt.Timestamp;

        primed = true;
        return newCount;
    }
}
