using UnityEngine;

// This allows you to create Grass/Water assets from the Editor.
[CreateAssetMenu(menuName = "EcoSim/Resource")]

// Unity class of ScriptableObject
public class ResourceDefinition : ScriptableObject
{
    public string resourceName;
    public float regenRate = 1f;
    public float maxAmount = 100f;
    public float currentAmount = 100f;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    [Tooltip("Amount at the start of every simulation run. currentAmount is reset to this, so a run never continues from the last Play session's leftovers.")]
    [Min(0f)] public float startingAmount = 100f;
    // [AI-GENERATED END]
}
