// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places where an animal saw a predator, fading over time. AnimalBrain uses it so prey keep
/// away from a spot after the predator is out of sight, instead of walking straight back.
/// </summary>
public class DangerMemory
{
    public struct Spot
    {
        public Vector3 position;
        public float remaining;   // seconds (simulation time) until forgotten
    }

    readonly List<Spot> spots = new();

    public IReadOnlyList<Spot> Spots => spots;
    public int Count => spots.Count;

    /// <summary>
    /// Remembers a sighting for <paramref name="seconds"/>. A sighting within <paramref name="mergeDistance"/>
    /// of a known spot refreshes that spot (and moves it to the latest position) instead of adding one.
    /// When full, the oldest spot is replaced.
    /// </summary>
    /// <returns>True when this is a new spot rather than a refresh.</returns>
    public bool Remember(Vector3 position, float seconds, float mergeDistance, int maxSpots)
    {
        float mergeSqr = mergeDistance * mergeDistance;
        for (int i = 0; i < spots.Count; i++)
        {
            if ((spots[i].position - position).sqrMagnitude > mergeSqr) continue;
            spots[i] = new Spot { position = position, remaining = Mathf.Max(spots[i].remaining, seconds) };
            return false;
        }

        if (maxSpots <= 0) return false;
        if (spots.Count >= maxSpots)
        {
            int oldest = 0;
            for (int i = 1; i < spots.Count; i++)
                if (spots[i].remaining < spots[oldest].remaining) oldest = i;
            spots.RemoveAt(oldest);
        }
        spots.Add(new Spot { position = position, remaining = seconds });
        return true;
    }

    /// <summary>Ages every spot and forgets the expired ones.</summary>
    public void Tick(float dt)
    {
        for (int i = spots.Count - 1; i >= 0; i--)
        {
            var s = spots[i];
            s.remaining -= dt;
            if (s.remaining <= 0f) spots.RemoveAt(i);
            else spots[i] = s;
        }
    }

    /// <summary>
    /// How dangerous <paramref name="p"/> feels, 0..1: 1 right on a fresh sighting, falling to 0 at
    /// <paramref name="radius"/> away and as the memory fades over <paramref name="seconds"/>.
    /// </summary>
    public float DangerAt(Vector3 p, float radius, float seconds)
    {
        if (spots.Count == 0 || radius <= 0f) return 0f;
        float danger = 0f;
        foreach (var s in spots)
        {
            Vector3 d = s.position - p;
            d.y = 0f;
            float closeness = 1f - d.magnitude / radius;
            if (closeness <= 0f) continue;
            float freshness = seconds > 0f ? Mathf.Clamp01(s.remaining / seconds) : 1f;
            danger = Mathf.Max(danger, closeness * freshness);
        }
        return danger;
    }

    public void Clear() => spots.Clear();
}
