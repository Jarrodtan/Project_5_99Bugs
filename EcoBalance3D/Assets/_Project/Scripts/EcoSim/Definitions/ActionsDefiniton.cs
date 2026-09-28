// ActionDefinition.cs — concrete behavior an organism executes to pursue a goal
using UnityEngine;

public enum ActionType { SeekResource, HuntTarget, Flee, Reproduce, Wander, Idle }

[CreateAssetMenu(menuName = "EcoSim/Action")]
public class ActionDefinition : ScriptableObject
{
    public string actionName;
    public ActionType type;
    public float duration = 1f;
    public float needSatisfactionAmount = 50f;
}
