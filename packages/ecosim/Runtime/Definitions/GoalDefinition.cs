// GoalDefinition.cs — what an organism can want, and roughly how urgently
using UnityEngine;

[CreateAssetMenu(menuName = "EcoSim/Goal")]
public class GoalDefinition : ScriptableObject
{
    public string goalName;              // "SatisfyHunger", "Flee", "Reproduce"
    public NeedDefinition relatedNeed;    // null for goals not tied to a decaying need (e.g. "Flee")
    public float basePriority = 1f;       // baseline weight before need urgency is factored in
    public ActionDefinition[] possibleActions;  // ways to achieve this goal
}
