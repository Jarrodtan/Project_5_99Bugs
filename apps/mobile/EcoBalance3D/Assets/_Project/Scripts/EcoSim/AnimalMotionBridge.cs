// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// The only link between the animal AI and the ProcAnim module: turns what <see cref="AnimalBrain"/>
/// is doing into a <see cref="MotionState"/> for <see cref="ProcAnimator"/>. ProcAnim knows nothing
/// about EcoSim or the AI, so it stays reusable.
/// Added automatically by AnimalBrain; the motion profile is loaded from Resources/Motion/&lt;species asset name&gt;
/// (falls back to Resources/Motion/Default, then to ProcAnim's built-in defaults).
/// </summary>
[RequireComponent(typeof(AnimalBrain))]
public class AnimalMotionBridge : MonoBehaviour
{
    [Tooltip("Below this ground speed (m/s) the animal counts as standing.")]
    public float standingSpeed = 0.15f;

    [Tooltip("Alert / stalk when a predator / prey is within this many times the flee / hunt radius.")]
    public float awarenessMultiplier = 1.5f;

    [Tooltip("Seconds between predator/prey proximity checks (EcoRegistry lookups).")]
    public float awarenessInterval = 0.25f;

    AnimalBrain brain;
    ProcAnimator animator;
    bool predatorNear, preyNear;
    float nextAwarenessCheck;

    public ProcAnimator Animator => animator;

    void Awake()
    {
        brain = GetComponent<AnimalBrain>();
        animator = GetComponent<ProcAnimator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<ProcAnimator>();
            animator.profile = LoadProfile(brain.species_data);
        }
    }

    static MotionProfile LoadProfile(SpeciesDefinition species)
    {
        MotionProfile found = species != null ? Resources.Load<MotionProfile>("Motion/" + species.name) : null;
        return found != null ? found : Resources.Load<MotionProfile>("Motion/Default");
    }

    void Update()
    {
        if (Time.time >= nextAwarenessCheck)
        {
            nextAwarenessCheck = Time.time + awarenessInterval;
            UpdateAwareness();
        }
        animator.SetState(Pick());
    }

    MotionState Pick()
    {
        if (brain.IsCaught) return MotionState.Die;

        bool standing = animator.Speed < standingSpeed;
        switch (brain.CurrentAction)
        {
            case AnimalBrain.AnimalAction.Fleeing:
                return MotionState.Flee;
            case AnimalBrain.AnimalAction.Hunting:
                // A predator stops on its catch to eat.
                return standing ? MotionState.Eat : MotionState.Run;
            case AnimalBrain.AnimalAction.EatingGrass:
                return standing ? MotionState.Eat : MotionState.Walk;
            case AnimalBrain.AnimalAction.DrinkingWater:
                return standing ? MotionState.Drink : MotionState.Walk;
            default: // Roaming
                if (predatorNear) return MotionState.Alert;
                if (preyNear) return MotionState.Stalk;
                return standing ? MotionState.Idle : MotionState.Walk;
        }
    }

    void UpdateAwareness()
    {
        var species = brain.species_data;
        if (species == null) { predatorNear = preyNear = false; return; }
        Vector3 at = transform.position;
        predatorNear = EcoRegistry.NearestPredator(species, at, brain.fleeRadius * awarenessMultiplier) != null;
        preyNear = EcoRegistry.NearestPrey(species, at, brain.huntRadius * awarenessMultiplier) != null;
    }
}
// [AI-GENERATED END]
