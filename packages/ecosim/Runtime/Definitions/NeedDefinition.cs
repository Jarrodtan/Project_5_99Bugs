// NeedDefinition.cs — a reusable "need" any species can reference
using UnityEngine;

[CreateAssetMenu(menuName = "EcoSim/Need")]
public class NeedDefinition : ScriptableObject
{
    public string needName;          // "Hunger", "Thirst", "Safety"
    public float maxValue = 100f;
    public float decayRate = 1f;     // per second/tick
    public float criticalThreshold = 20f;  // below this = urgent
}
