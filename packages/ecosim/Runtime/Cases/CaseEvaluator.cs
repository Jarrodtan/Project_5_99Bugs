// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
/// <summary>
/// Pure checks of <see cref="InvestigationCase"/> conditions against a <see cref="PopulationHistory"/>.
/// No Unity scene needed, so it is unit-tested directly and reused by the balance-model reachability check.
/// </summary>
public static class CaseEvaluator
{
    /// <summary>True when every condition holds on the latest recorded day and that day is late enough.</summary>
    public static bool IsMet(InvestigationCase c, PopulationHistory history)
    {
        if (c == null || history == null || history.Count == 0) return false;
        if (c.conditions == null || c.conditions.Count == 0) return false;
        if (history.Latest.Tick < c.earliestDay) return false;
        foreach (var cond in c.conditions)
            if (!IsMet(cond, history)) return false;
        return true;
    }

    public static bool IsMet(CaseCondition cond, PopulationHistory history)
    {
        if (cond == null || history == null || history.Count == 0) return false;
        string key = cond.SubjectKey;
        if (key == null) return false;
        var today = history.Latest;
        if (!today.Has(key)) return false;
        float now = today.Get(key);

        switch (cond.kind)
        {
            case CaseConditionKind.AtMost: return now <= cond.value;
            case CaseConditionKind.AtLeast: return now >= cond.value;
            case CaseConditionKind.ReachedZero: return now <= 0f;
            case CaseConditionKind.DroppedByFraction:
            {
                float before = history.Back(cond.windowDays).Get(key);
                return before > 0f && (before - now) / before >= cond.value;
            }
            case CaseConditionKind.RoseByFraction:
            {
                float before = history.Back(cond.windowDays).Get(key);
                return before > 0f && (now - before) / before >= cond.value;
            }
            case CaseConditionKind.RatioAtLeast:
            {
                string otherKey = cond.OtherKey;
                if (otherKey == null || !today.Has(otherKey)) return false;
                float other = today.Get(otherKey);
                return other > 0f ? now / other >= cond.value : now > 0f;
            }
            default: return false;
        }
    }
}
// [AI-GENERATED END]
