// Drop-in replacement for AnimalBrain's decision-making. Keeps the same NavMeshAgent
// setup and footstep audio, but Roam() is now just what happens when the goal-oriented
// tree picks the Wander action - which action actually runs is decided by need urgency
// instead of being hardcoded.
using UnityEngine;
using UnityEngine.AI;

public class GoalOrientedAnimalBrain : MonoBehaviour
{
    public SpeciesDefinition species_data;
    public AudioSource audio;

    NavMeshAgent agent;
    GoalSelectorNode root;
    BTContext ctx;
    AnimalNeedsState needsState;

    [Tooltip("How often the tree re-evaluates, in seconds. Doesn't need to be every frame.")]
    public float tickInterval = 0.25f;
    float tickTimer;

    float footstep_timer;

    [Tooltip("Shows the current goal and each goal's live score above the animal in Scene/Game view. Testing aid only.")]
    public bool showDebugLabel = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audio = GetComponent<AudioSource>();

        var col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            float dist = col.bounds.min.y - transform.position.y;
            agent.baseOffset = -dist;
        }

        if (species_data == null)
        {
            Debug.LogWarning($"{name}: GoalOrientedAnimalBrain has no SpeciesDefinition assigned.", this);
            enabled = false;
            return;
        }

        gameObject.name = species_data.speciesName;

        needsState = new AnimalNeedsState(species_data.needs);
        root = new GoalSelectorNode(species_data);
        ctx = new BTContext
        {
            gameObject = gameObject,
            transform = transform,
            agent = agent,
            species = species_data,
            needs = needsState,
        };
    }

    void Update()
    {
        if (root == null) return;

        needsState.Tick(Time.deltaTime);

        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            ctx.deltaTime = tickInterval;
            root.Tick(ctx); // Success/Failure/Running all fine to ignore here - it just re-picks next tick
            tickTimer = tickInterval;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f)
        {
            footstep_timer -= Time.deltaTime;
            if (footstep_timer <= 0f)
            {
                if (audio != null && audio.clip != null && AudioManager.Instance != null)
                    AudioManager.Instance.Play3DSound(audio, audio.clip);
                footstep_timer = Random.Range(0.4f, 0.6f);
            }
        }
    }

    // Testing aid: draws the active goal and per-goal scores as a floating label
    // in the Scene view so you can watch decisions happen in real time. Remove or
    // set showDebugLabel = false once you've confirmed things work as expected.
    void OnDrawGizmos()
    {
        if (!showDebugLabel || root == null) return;

        var lines = $"Goal: {root.CurrentGoalName}\n";
        foreach (var kvp in root.LastScores)
            lines += $"{kvp.Key}: {kvp.Value:F2}\n";

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, lines);
#endif
    }
}
