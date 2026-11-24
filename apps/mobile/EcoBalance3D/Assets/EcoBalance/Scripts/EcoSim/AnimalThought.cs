// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using UnityEngine;

/// <summary>
/// Display-only thought for an animal: one icon kind and one child-friendly line.
/// Reads AnimalBrain state; never writes it. Caches strings so callers allocate only on change.
/// </summary>
public static class AnimalThought
{
    public const float NeedThreshold = 40f;

    public enum Kind
    {
        None,
        Scared,
        Water,
        Grass,
        Meat,
        Heart
    }

    // Fixed lines — no per-call string building except the hunting-with-target case.
    const string LineScared = "I'm scared and running away!";
    const string LineThirsty = "I'm thirsty, heading to water.";
    const string LineDrinking = "I'm drinking water now.";
    const string LineHungryGrass = "I'm hungry, looking for grass.";
    const string LineEating = "I'm eating grass.";
    const string LineHungryMeat = "I'm hungry, hunting for meat.";
    const string LineFamily = "I want to start a family.";
    const string LineRoam = "I'm just walking around.";

    // Per-animal line cache so Update can assign by reference without rebuilding.
    static AnimalBrain lineBrain;
    static AnimalBrain.AnimalAction lineAction;
    static bool lineThirsty;
    static bool lineHungry;
    static bool linePredator;
    static string lineTarget = "";
    static Kind lineKind;
    static string lineText = LineRoam;
    static string huntLineTarget = "";
    static string huntLineText = LineHungryMeat;

    public static Kind GetKind(AnimalBrain brain)
    {
        if (brain == null || brain.IsCaught) return Kind.None;

        var action = brain.CurrentAction;
        if (action == AnimalBrain.AnimalAction.Fleeing) return Kind.Scared;

        if ((brain.HasThirst && brain.curr_thirst < NeedThreshold) ||
            action == AnimalBrain.AnimalAction.DrinkingWater)
            return Kind.Water;

        if (brain.curr_hunger < NeedThreshold ||
            action == AnimalBrain.AnimalAction.EatingGrass ||
            action == AnimalBrain.AnimalAction.Hunting)
            return brain.IsPredatorSpecies ? Kind.Meat : Kind.Grass;

        if (action == AnimalBrain.AnimalAction.Reproducing) return Kind.Heart;

        return Kind.None;
    }

    /// <summary>
    /// Plain-English line for the animal card. Rebuilds only when action / needs / target change.
    /// </summary>
    public static string Line(AnimalBrain brain)
    {
        if (brain == null) return LineRoam;

        var action = brain.CurrentAction;
        bool thirsty = brain.HasThirst && brain.curr_thirst < NeedThreshold;
        bool hungry = brain.curr_hunger < NeedThreshold;
        bool predator = brain.IsPredatorSpecies;
        string target = brain.CurrentTargetName ?? "";
        Kind kind = GetKind(brain);

        if (lineBrain == brain &&
            lineAction == action &&
            lineThirsty == thirsty &&
            lineHungry == hungry &&
            linePredator == predator &&
            lineKind == kind &&
            lineTarget == target)
            return lineText;

        lineBrain = brain;
        lineAction = action;
        lineThirsty = thirsty;
        lineHungry = hungry;
        linePredator = predator;
        lineKind = kind;
        lineTarget = target;
        lineText = BuildLine(brain, action, thirsty, hungry, predator, target, kind);
        return lineText;
    }

    static string BuildLine(
        AnimalBrain brain,
        AnimalBrain.AnimalAction action,
        bool thirsty,
        bool hungry,
        bool predator,
        string target,
        Kind kind)
    {
        if (kind == Kind.Scared || action == AnimalBrain.AnimalAction.Fleeing)
            return LineScared;

        if (kind == Kind.Water || action == AnimalBrain.AnimalAction.DrinkingWater || thirsty)
        {
            if (action == AnimalBrain.AnimalAction.DrinkingWater && !thirsty)
                return LineDrinking;
            return LineThirsty;
        }

        if (kind == Kind.Grass || kind == Kind.Meat ||
            action == AnimalBrain.AnimalAction.EatingGrass ||
            action == AnimalBrain.AnimalAction.Hunting ||
            hungry)
        {
            if (action == AnimalBrain.AnimalAction.EatingGrass)
                return LineEating;
            if (predator || action == AnimalBrain.AnimalAction.Hunting)
                return HuntLine(target);
            return LineHungryGrass;
        }

        if (kind == Kind.Heart || action == AnimalBrain.AnimalAction.Reproducing)
            return LineFamily;

        return LineRoam;
    }

    static string HuntLine(string target)
    {
        if (string.IsNullOrEmpty(target)) return LineHungryMeat;
        if (huntLineTarget == target && huntLineText != null) return huntLineText;
        huntLineTarget = target;
        // "Blue Sheep" -> "sheep"; keep it short for kids.
        string shortName = target;
        int space = target.LastIndexOf(' ');
        if (space >= 0 && space < target.Length - 1)
            shortName = target.Substring(space + 1).ToLowerInvariant();
        else
            shortName = target.ToLowerInvariant();
        huntLineText = "I'm hungry, hunting a " + shortName + ".";
        return huntLineText;
    }
}
