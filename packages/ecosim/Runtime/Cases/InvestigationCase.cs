// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Field Journal investigation case (Free Exploration mode). It unlocks the first time ALL of
/// its <see cref="conditions"/> are true on the same simulated day, based on what actually
/// happened in the run (read from <see cref="PopulationHistory"/>), not on what the player intended.
/// Designers create these as assets: Create > EcoSim > Investigation Case.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Investigation Case", fileName = "Case_")]
public class InvestigationCase : ScriptableObject
{
    [Tooltip("Stable id saved in the journal. Never change it after release, or players lose the unlock.")]
    public string caseId;

    public string title;

    [Tooltip("Shown while the case is still locked. Points the player toward it without giving it away.")]
    [TextArea(2, 4)] public string hint;

    [Tooltip("Shown once unlocked: what happened and why.")]
    [TextArea(3, 8)] public string explanation;

    [Tooltip("The case can't unlock before this simulated day (avoids unlocking on the starting state).")]
    [Min(0)] public int earliestDay = 1;

    [Tooltip("All of these must be true on the same day.")]
    public List<CaseCondition> conditions = new();

    /// <summary>Journal key: <see cref="caseId"/>, or the asset name if no id was set.</summary>
    public string Key => string.IsNullOrWhiteSpace(caseId) ? name : caseId;
}

/// <summary>What a <see cref="CaseCondition"/> checks.</summary>
public enum CaseConditionKind
{
    /// <summary>Subject's value today is at most <c>value</c>.</summary>
    AtMost,
    /// <summary>Subject's value today is at least <c>value</c>.</summary>
    AtLeast,
    /// <summary>Subject reached 0 (extinct / used up).</summary>
    ReachedZero,
    /// <summary>Subject fell by at least <c>value</c> (0.5 = 50%) compared with <c>windowDays</c> ago.</summary>
    DroppedByFraction,
    /// <summary>Subject grew by at least <c>value</c> (0.5 = 50%) compared with <c>windowDays</c> ago.</summary>
    RoseByFraction,
    /// <summary>Subject ÷ other is at least <c>value</c> today (e.g. leopards ÷ sheep ≥ 0.3).</summary>
    RatioAtLeast,
}

/// <summary>One rule inside an <see cref="InvestigationCase"/>.</summary>
[System.Serializable]
public class CaseCondition
{
    public CaseConditionKind kind;

    [Tooltip("SpeciesDefinition or ResourceDefinition asset this rule reads.")]
    public ScriptableObject subject;

    [Tooltip("Only for RatioAtLeast: the asset to divide by.")]
    public ScriptableObject other;

    [Tooltip("Threshold. Counts for AtMost/AtLeast; a fraction (0.5 = 50%) for Dropped/Rose; a ratio for RatioAtLeast.")]
    public float value;

    [Tooltip("Only for Dropped/Rose: how many days back to compare with.")]
    [Min(1)] public int windowDays = 10;

    /// <summary>History key of <see cref="subject"/> (its asset name, as recorded by the recorder).</summary>
    public string SubjectKey => subject != null ? subject.name : null;
    public string OtherKey => other != null ? other.name : null;
}
// [AI-GENERATED END]
