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
    bool wasStanding = true;
    Transform nearPredator, nearPrey;
    bool predatorNear => nearPredator != null;
    bool preyNear => nearPrey != null;
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
        var state = Pick();
        animator.SetState(state);
        // Alert prey watch the nearest predator; predators watch their prey (when hunting or stalking).
        // Not while fleeing: the predator is behind, past the head's turn limit.
        Transform watch = state == MotionState.Alert ? nearPredator
                        : state == MotionState.Stalk || state == MotionState.Run ? nearPrey : null;
        animator.SetLookTarget(watch != null ? watch.position : (Vector3?)null);
        animator.SetGroundNormal(GroundNormal(transform.position));
    }

    // Slope of the terrain under the animal (flat ground, or no terrain, = straight up).
    static Vector3 GroundNormal(Vector3 pos)
    {
        var t = Terrain.activeTerrain;   // one terrain in this game; activeTerrains would allocate every call
        // The terrain's data can already be gone while the scene closes (leaving the run, stopping Play),
        // with the animals still getting one last Update: treat that as flat ground too.
        var data = t != null ? t.terrainData : null;
        if (data == null) return Vector3.up;
        Vector3 local = pos - t.GetPosition();
        Vector3 size = data.size;
        if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) return Vector3.up;
        return data.GetInterpolatedNormal(local.x / size.x, local.z / size.z);
    }

    MotionState Pick()
    {
        if (brain.IsCaught) return MotionState.Die;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): two thresholds instead of one. An animal creeping at about
        // standingSpeed used to flip Eat/Walk every frame, which looked like shaking.
        if (wasStanding && animator.Speed > standingSpeed * 2.5f) wasStanding = false;
        else if (!wasStanding && animator.Speed < standingSpeed) wasStanding = true;
        bool standing = wasStanding;
        switch (brain.CurrentAction)
        {
            case AnimalBrain.AnimalAction.Fleeing:
                return MotionState.Flee;
            case AnimalBrain.AnimalAction.Hunting:
                // A predator stops on its catch to eat; creeps low while stalking; runs on the pounce.
                if (standing) return MotionState.Eat;
                return brain.IsStalking ? MotionState.Stalk : MotionState.Run;
            case AnimalBrain.AnimalAction.EatingGrass:
                return standing ? MotionState.Eat : MotionState.Walk;
            case AnimalBrain.AnimalAction.DrinkingWater:
                return standing ? MotionState.Drink : MotionState.Walk;
            case AnimalBrain.AnimalAction.Reproducing:
                return standing ? MotionState.Idle : MotionState.Walk;
            default: // Roaming
                if (predatorNear) return MotionState.Alert;
                if (preyNear) return MotionState.Stalk;
                return standing ? MotionState.Idle : MotionState.Walk;
        }
    }

    void UpdateAwareness()
    {
        var species = brain.species_data;
        if (species == null) { nearPredator = nearPrey = null; return; }
        Vector3 at = transform.position;
        nearPredator = EcoRegistry.NearestPredator(species, at, brain.fleeRadius * awarenessMultiplier);
        nearPrey = EcoRegistry.NearestPrey(species, at, brain.huntRadius * awarenessMultiplier);
    }
}
// [AI-GENERATED END]
