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

    [Header("SeekResource only")]
    [Tooltip("Resource to look for (e.g. Grass, Water). Leave empty to use whatever this species Eats.")]
    public ResourceDefinition targetResource;

    [Tooltip("How far the animal can spot a patch. 0 = unlimited.")]
    public float searchRadius = 20f;

    [Tooltip("How much is taken from the patch each time the animal eats/drinks.")]
    public float resourceConsumedPerUse = 10f;
}
