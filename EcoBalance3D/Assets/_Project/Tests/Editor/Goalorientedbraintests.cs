using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// EditMode tests for the pieces of the goal-oriented tree that don't depend on
// NavMeshAgent/scene setup: need decay and goal scoring. Movement/execution
// (WanderExecutor etc.) needs an actual NavMesh and is tested manually in a scene
// instead - see the testing doc.
public class GoalOrientedBrainTests
{
    NeedDefinition hunger;
    GoalDefinition eatGoal;
    GoalDefinition wanderGoal;
    ActionDefinition idleAction;
    SpeciesDefinition sheep;

    [SetUp]
    public void SetUp()
    {
        hunger = Create<NeedDefinition>("Hunger");
        hunger.maxValue = 100f;
        hunger.decayRate = 10f; // per second, deliberately fast for quick tests

        idleAction = Create<ActionDefinition>("Idle");
        idleAction.type = ActionType.Idle;
        idleAction.duration = 0f; // succeeds immediately
        idleAction.needSatisfactionAmount = 50f;

        eatGoal = Create<GoalDefinition>("SatisfyHunger");
        eatGoal.relatedNeed = hunger;
        eatGoal.basePriority = 1f;
        eatGoal.possibleActions = new[] { idleAction };

        wanderGoal = Create<GoalDefinition>("Wander");
        wanderGoal.relatedNeed = null; // no need - constant low-priority filler
        wanderGoal.basePriority = 0.1f;
        wanderGoal.possibleActions = new[] { idleAction };

        sheep = Create<SpeciesDefinition>("Sheep");
        sheep.needs = new List<NeedDefinition> { hunger };
        sheep.goals = new List<GoalDefinition> { eatGoal, wanderGoal };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(hunger);
        Object.DestroyImmediate(eatGoal);
        Object.DestroyImmediate(wanderGoal);
        Object.DestroyImmediate(idleAction);
        Object.DestroyImmediate(sheep);
    }

    static T Create<T>(string name) where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        obj.name = name;
        return obj;
    }

    [Test]
    public void NeedsState_StartsFullyStatisfied()
    {
        var needs = new AnimalNeedsState(new[] { hunger });
        Assert.AreEqual(0f, needs.GetUrgency01(hunger), 1e-4f);
    }

    [Test]
    public void NeedsState_DecaysOverTime()
    {
        var needs = new AnimalNeedsState(new[] { hunger });
        needs.Tick(1f); // 10 points of decay

        Assert.AreEqual(0.1f, needs.GetUrgency01(hunger), 1e-4f);
    }

    [Test]
    public void NeedsState_ClampsAtEmpty()
    {
        var needs = new AnimalNeedsState(new[] { hunger });
        needs.Tick(100f); // way more decay than maxValue

        Assert.AreEqual(1f, needs.GetUrgency01(hunger), 1e-4f);
    }

    [Test]
    public void NeedsState_SatisfyRestoresValue()
    {
        var needs = new AnimalNeedsState(new[] { hunger });
        needs.Tick(5f); // 50 points of decay -> urgency 0.5
        needs.Satisfy(hunger, 50f);

        Assert.AreEqual(0f, needs.GetUrgency01(hunger), 1e-4f);
    }

    //[Test]
    //public void GoalSelector_PicksHighestScoringGoal_WhenNeedIsUrgent()
    //{
    //    var needs = new AnimalNeedsState(new[] { hunger });
    //    needs.Tick(9f); // 90 points decayed -> hunger urgency 0.9, easily beats wander's flat 0.1

    //    var root = new GoalSelectorNode(sheep);
    //    var ctx = new BTContext { needs = needs };

    //    root.Tick(ctx);

    //    // idleAction succeeds instantly, so CurrentGoalName resets to "(none)" right after
    //    // Tick returns - check LastScores instead to confirm hunger outscored wander this tick.
    //    Assert.Greater(root.LastScores["SatisfyHunger"], root.LastScores["Wander"]);
    //}

    //[Test]
    //public void GoalSelector_FallsBackToLowPriorityGoal_WhenNeedIsSatisfied()
    //{
    //    var needs = new AnimalNeedsState(new[] { hunger }); // fully satisfied, urgency 0

    //    var root = new GoalSelectorNode(sheep);
    //    var ctx = new BTContext { needs = needs };

    //    root.Tick(ctx);

    //    Assert.AreEqual(0f, root.LastScores["SatisfyHunger"], 1e-4f);
    //    Assert.Greater(root.LastScores["Wander"], root.LastScores["SatisfyHunger"]);
    //}
}
