// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;

/// <summary>
/// Day-by-day record of every species population and resource amount, keyed by asset name.
/// Keeps the most recent <see cref="Capacity"/> samples (oldest dropped first) so a long run
/// can't grow memory forever. Read by the population graph and the case detector.
/// </summary>
public class PopulationHistory
{
    /// <summary>Values at the end of one tick (tick 0 = the starting state).</summary>
    public sealed class Sample
    {
        public readonly int Tick;
        readonly Dictionary<string, float> values;

        public Sample(int tick, Dictionary<string, float> values)
        {
            Tick = tick;
            this.values = values;
        }

        /// <summary>The value for a species or resource asset name, or 0 if it wasn't recorded.</summary>
        public float Get(string key) => key != null && values.TryGetValue(key, out var v) ? v : 0f;
        public bool Has(string key) => key != null && values.ContainsKey(key);
        public IEnumerable<string> Keys => values.Keys;
    }

    readonly Sample[] buffer;
    int start, count;

    /// <summary>Raised after a sample is added.</summary>
    public event Action<Sample> SampleAdded;
    /// <summary>Raised after <see cref="Clear"/>.</summary>
    public event Action Cleared;

    public PopulationHistory(int capacity = 1000)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        buffer = new Sample[capacity];
    }

    public int Capacity => buffer.Length;
    public int Count => count;

    /// <summary>Oldest retained sample is index 0; the newest is Count - 1.</summary>
    public Sample this[int index]
    {
        get
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return buffer[(start + index) % buffer.Length];
        }
    }

    public Sample Latest => count == 0 ? null : this[count - 1];

    public void Add(int tick, Dictionary<string, float> values)
    {
        var sample = new Sample(tick, values ?? new Dictionary<string, float>());
        if (count < buffer.Length)
        {
            buffer[(start + count) % buffer.Length] = sample;
            count++;
        }
        else
        {
            buffer[start] = sample;
            start = (start + 1) % buffer.Length;
        }
        SampleAdded?.Invoke(sample);
    }

    public void Clear()
    {
        Array.Clear(buffer, 0, buffer.Length);
        start = count = 0;
        Cleared?.Invoke();
    }

    /// <summary>One key's values, oldest first (for graph lines).</summary>
    public List<float> Series(string key)
    {
        var list = new List<float>(count);
        for (int i = 0; i < count; i++) list.Add(this[i].Get(key));
        return list;
    }

    /// <summary>
    /// The sample from <paramref name="ticksAgo"/> ticks before the latest, or the oldest retained
    /// one if history is shorter. Used for "dropped by half over the last N days" checks.
    /// </summary>
    public Sample Back(int ticksAgo)
    {
        if (count == 0) return null;
        int i = count - 1 - Math.Max(0, ticksAgo);
        return this[Math.Max(0, i)];
    }
}
// [AI-GENERATED END]
