using System.Collections.Generic;
using UnityEngine;

// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj

// One animal's live need values. NeedDefinition assets are shared, designer-edited data
// (decay rate, max, threshold) - this class is the per-instance state that decays against
// that data, the same split as SpeciesDefinition (data) vs EcosystemSimulator.populations
// (runtime state) already used elsewhere in the project.
public class AnimalNeedsState
{
    readonly Dictionary<NeedDefinition, float> current = new();

    public AnimalNeedsState(IEnumerable<NeedDefinition> needs)
    {
        if (needs == null) return;
        foreach (var need in needs)
        {
            if (need == null || current.ContainsKey(need)) continue;
            current[need] = need.maxValue; // start fully satisfied
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (var need in new List<NeedDefinition>(current.Keys))
        {
            var value = current[need] - need.decayRate * deltaTime;
            current[need] = Mathf.Clamp(value, 0f, need.maxValue);
        }
    }

    public void Satisfy(NeedDefinition need, float amount)
    {
        if (need == null) return;
        var value = current.TryGetValue(need, out var v) ? v : need.maxValue;
        current[need] = Mathf.Clamp(value + amount, 0f, need.maxValue);
    }

    // 0 = fully satisfied, 1 = empty. Goals score against this, so lower need = lower urgency.
    public float GetUrgency01(NeedDefinition need)
    {
        if (need == null || need.maxValue <= 0f) return 0f;
        if (!current.TryGetValue(need, out var value)) return 1f; // untracked need reads as urgent
        return 1f - Mathf.Clamp01(value / need.maxValue);
    }
}
