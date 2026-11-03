// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj

using System.Collections.Generic;

// Root node for one animal's tree. Unlike a plain Selector, its children aren't fixed
// in the Inspector - it re-scores every GoalDefinition on SpeciesDefinition.goals each
// tick (basePriority * need urgency) and runs whichever one currently wins.
//
// To "sticky" a goal so it doesn't thrash frame to frame, an in-progress goal keeps
// running unless another goal's score beats it by stickyMargin - so a Flee goal can
// still interrupt, but two similarly-urgent goals won't flicker back and forth.
public class GoalSelectorNode : BTNode
{
    readonly List<SpeciesGoalEntry> entries = new();
    GoalDefinition currentGoal;
    public float stickyMargin = 0.15f;

    // For debugging/testing only - lets you see which goal is currently active
    // and how every goal scored on the last Tick, without changing any behavior.
    public string CurrentGoalName => currentGoal != null ? currentGoal.goalName : "(none)";
    public readonly Dictionary<string, float> LastScores = new();

    class SpeciesGoalEntry
    {
        public GoalDefinition goal;
        public BTNode subtree; // built once, reused every tick this goal is chosen
    }

    public GoalSelectorNode(SpeciesDefinition species)
    {
        if (species?.goals == null) return;

        foreach (var goal in species.goals)
        {
            if (goal == null) continue;
            entries.Add(new SpeciesGoalEntry
            {
                goal = goal,
                subtree = BuildSubtreeFor(goal)
            });
        }
    }

    // A goal succeeds if ANY of its possibleActions succeeds - fallback options,
    // tried in the order the designer listed them on the GoalDefinition asset.
    static BTNode BuildSubtreeFor(GoalDefinition goal)
    {
        var children = new List<BTNode>();
        if (goal.possibleActions != null)
        {
            foreach (var action in goal.possibleActions)
            {
                if (action == null) continue;
                children.Add(new ActionNode(action, goal.relatedNeed));
            }
        }
        return new Selector(children);
    }

    static float UrgencyOf(NeedDefinition need, AnimalNeedsState needs)
    {
        if (need == null || needs == null) return 1f; // goals with no need (e.g. Flee) are always fully urgent
        return needs.GetUrgency01(need); // 0 = fully satisfied, 1 = empty
    }

    public override NodeStatus Tick(BTContext ctx)
    {
        if (entries.Count == 0) return NodeStatus.Failure;

        SpeciesGoalEntry best = null;
        float bestScore = float.NegativeInfinity;
        float currentScore = float.NegativeInfinity;

        LastScores.Clear();
        foreach (var entry in entries)
        {
            float score = entry.goal.basePriority * UrgencyOf(entry.goal.relatedNeed, ctx.needs);
            LastScores[entry.goal.goalName] = score;
            if (entry.goal == currentGoal) currentScore = score;
            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        // Keep the current goal unless something else clearly beats it.
        if (currentGoal != null && bestScore - currentScore < stickyMargin)
        {
            best = entries.Find(e => e.goal == currentGoal);
        }

        if (best == null) return NodeStatus.Failure;
        currentGoal = best.goal;

        var status = best.subtree.Tick(ctx);
        if (status != NodeStatus.Running) currentGoal = null; // free to re-pick next tick
        return status;
    }
}
