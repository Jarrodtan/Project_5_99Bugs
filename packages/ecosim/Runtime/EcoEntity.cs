// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Put this on anything in the world that the ecosystem should be able to find:
/// animal prefabs (definition = a SpeciesDefinition) or grass patches (a ResourceDefinition).
/// It registers itself in <see cref="EcoRegistry"/> while active.
/// </summary>
public class EcoEntity : MonoBehaviour
{
    [Tooltip("SpeciesDefinition for animals, ResourceDefinition for resource objects such as grass.")]
    [SerializeField] ScriptableObject definition;

    public ScriptableObject Definition => definition;

    /// <summary>Changes the definition at runtime (e.g. set by a spawner) and re-registers.</summary>
    public void SetDefinition(ScriptableObject value)
    {
        if (isActiveAndEnabled) EcoRegistry.Unregister(transform, definition);
        definition = value;
        if (isActiveAndEnabled) EcoRegistry.Register(transform, definition);
    }

    void OnEnable() => EcoRegistry.Register(transform, definition);
    void OnDisable() => EcoRegistry.Unregister(transform, definition);
}
// [AI-GENERATED END]
