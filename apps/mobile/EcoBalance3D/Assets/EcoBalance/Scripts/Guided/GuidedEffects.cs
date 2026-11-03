// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;

/// <summary>
/// Temporary buffs and debuffs from Guided Exploration decisions (Shumin's research, Event &amp; Decision
/// System). The simulation multiplies its own rates by these, so nothing in the ScriptableObjects is
/// changed and everything returns to normal when an effect expires or the challenge restarts.
/// Outside Guided mode every value is 1 (or 0 for the hunt bonus), so Free Exploration is unaffected.
/// </summary>
public static class GuidedEffects
{
    public enum Kind
    {
        SheepThirst, LeopardThirst,   // hydration depletion
        SheepHunger, LeopardHunger,   // fullness depletion
        PlantRegen,                   // vegetation regeneration
        Graze,                        // vegetation eaten per bite
        HuntSuccess,                  // added to the hunt success chance (0.15 = +15 points)
    }

    struct Effect { public Kind kind; public float value; public int untilDay; }
    static readonly List<Effect> active = new List<Effect>();
    static int today = 1;

    /// <summary>Adds an effect for a number of simulated days. Multipliers stack by multiplying; the hunt bonus adds.</summary>
    public static void Add(Kind kind, float value, int days) =>
        active.Add(new Effect { kind = kind, value = value, untilDay = today + days });

    /// <summary>Called by the challenge at each new day; drops expired effects.</summary>
    public static void NewDay(int day)
    {
        today = day;
        active.RemoveAll(e => day >= e.untilDay);
    }

    public static void Clear() { active.Clear(); today = 1; }

    static float Product(Kind k)
    {
        float m = 1f;
        foreach (var e in active) if (e.kind == k) m *= e.value;
        return m;
    }

    public static float Thirst(bool predator) => Product(predator ? Kind.LeopardThirst : Kind.SheepThirst);
    public static float Hunger(bool predator) => Product(predator ? Kind.LeopardHunger : Kind.SheepHunger);
    public static float PlantRegen => Product(Kind.PlantRegen);
    public static float Graze => Product(Kind.Graze);

    public static float HuntSuccessBonus
    {
        get
        {
            float b = 0f;
            foreach (var e in active) if (e.kind == Kind.HuntSuccess) b += e.value;
            return b;
        }
    }
}
