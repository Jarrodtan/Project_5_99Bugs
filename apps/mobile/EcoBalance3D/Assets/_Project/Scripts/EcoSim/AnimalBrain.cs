// Animal Core AI logic

using UnityEngine;
using UnityEngine.AI;

public class AnimalBrain : MonoBehaviour
{
    public SpeciesDefinition species_data;
    public float curr_hunger = 100f;

    private NavMeshAgent agent;
    private float wander_timer = 0f;
    public AudioSource audio;
    private float footstep_timer = 0f;

    private SimulationSpeedController simulationController;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    [Header("Predator / prey")]
    [Tooltip("Predators notice prey within this distance.")]
    public float huntRadius = 12f;
    [Tooltip("Prey notices predators within this distance.")]
    public float fleeRadius = 7f;
    [Tooltip("Distance at which a predator catches its prey.")]
    public float catchDistance = 1.5f;
    [Tooltip("Seconds a predator stops to eat after a catch.")]
    public float eatDuration = 2f;
    [Tooltip("Speed multiplier while chasing or fleeing.")]
    public float chaseSpeedMultiplier = 1.4f;
    public float fleeSpeedMultiplier = 1.25f;

    [Tooltip("Plant-eaters look for grass / water within this distance.")]
    public float forageRadius = 15f;
    [Tooltip("Chance (0-1) that a roaming animal heads to food or water instead of a random spot.")]
    [Range(0f, 1f)] public float forageChance = 0.6f;
    [Tooltip("Amount taken from a patch per visit.")]
    public float biteSize = 20f;

    private float base_speed;
    private float eat_timer = 0f;
    private ResourcePatch forage_target;
    private readonly System.Collections.Generic.List<ResourceDefinition> wanted_resources = new();
    // [AI-GENERATED END]

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        base_speed = agent.speed;
        audio = GetComponent<AudioSource>();

        simulationController = FindFirstObjectByType<SimulationSpeedController>();

        // Find collider to determine where is the floor
        Collider col = GetComponentInChildren<Collider>();

        if (col != null)
        {
            float dist = col.bounds.min.y - transform.position.y;
            agent.baseOffset = -dist;
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): fall back to the species the spawner assigned.
        if (species_data == null && TryGetComponent(out EcoEntity entity))
            species_data = entity.Definition as SpeciesDefinition;

        if (species_data != null)
        {
            gameObject.name = species_data.speciesName;
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): resources this animal eats (Grass) or needs (Water).
            wanted_resources.AddRange(species_data.GetResourceTargets(RelationshipType.Eats));
            wanted_resources.AddRange(species_data.GetResourceTargets(RelationshipType.Needs));
        }
    }

    void Update()
    {
        // Stop animal AI when simulation is paused
        if (simulationController != null && !simulationController.IsRunning)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        float speed = 1f;

        if (simulationController != null)
        {
            speed = simulationController.CurrentSpeed;
        }

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Priority: eat (after a catch / bite) > flee from predator > hunt prey > go to grass/water > roam.
        if (eat_timer > 0f)
        {
            eat_timer -= Time.deltaTime * speed;
            agent.isStopped = true;
        }
        else if (!Flee(speed) && !Hunt(speed) && !Forage(speed))
        {
            agent.speed = base_speed * speed;

            // Simulation speed affects wandering timer
            wander_timer -= Time.deltaTime * speed;

            if (wander_timer <= 0f)
            {
                if (Random.value < forageChance)
                    forage_target = ResourcePatch.FindNearest(transform.position, wanted_resources, forageRadius);
                if (forage_target == null)
                    Roam();
                wander_timer = Random.Range(3f, 6f);
            }
        }
        // [AI-GENERATED END]

        if (agent.velocity.magnitude > 0.1f)
        {
            footstep_timer -= Time.deltaTime;
            if (footstep_timer <= 0f) {
                if (audio != null && audio.clip != null && AudioManager.Instance != null) {
                    AudioManager.Instance.Play3DSound(audio, audio.clip);
                }
                footstep_timer = Random.Range(0.4f, 0.6f);
            }
        }
     
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Run directly away from the nearest predator. Returns false when none is close.
    bool Flee(float speed)
    {
        if (species_data == null) return false;
        Transform predator = EcoRegistry.NearestPredator(species_data, transform.position, fleeRadius);
        if (predator == null) return false;

        Vector3 away = transform.position - predator.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
        Vector3 target = transform.position + away.normalized * 6f;

        // If the straight-away point is off the island, try sideways escapes instead of hugging the edge.
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            Vector3 side = Vector3.Cross(Vector3.up, away.normalized) * (Random.value < 0.5f ? 6f : -6f);
            if (!NavMesh.SamplePosition(transform.position + side, out hit, 3f, NavMesh.AllAreas))
                return false;
        }

        agent.speed = base_speed * fleeSpeedMultiplier * speed;
        agent.SetDestination(hit.position);
        wander_timer = 0.5f;
        return true;
    }

    // Walk to the chosen grass/water patch and take a bite. Returns false when there is no target.
    bool Forage(float speed)
    {
        if (forage_target == null) return false;
        if (!forage_target.IsEdible) { forage_target = null; return false; }

        Vector3 to = forage_target.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude <= catchDistance)
        {
            forage_target.Consume(biteSize);
            forage_target = null;
            eat_timer = eatDuration;
            agent.ResetPath();
            return true;
        }

        agent.speed = base_speed * speed;
        agent.SetDestination(forage_target.transform.position);
        return true;
    }

    // Chase the nearest prey; stop to eat when caught. Returns false when no prey is in range.
    bool Hunt(float speed)
    {
        if (species_data == null) return false;
        Transform prey = EcoRegistry.NearestPrey(species_data, transform.position, huntRadius);
        if (prey == null) return false;

        if (Vector3.Distance(transform.position, prey.position) <= catchDistance)
        {
            // Visual only: population numbers come from EcosystemSimulator, so the sheep is not destroyed here.
            eat_timer = eatDuration;
            agent.ResetPath();
            return true;
        }

        agent.speed = base_speed * chaseSpeedMultiplier * speed;
        agent.SetDestination(prey.position);
        wander_timer = 0.5f;
        return true;
    }
    // [AI-GENERATED END]

    void Roam()
    {
        Vector3 rand_dir = Random.insideUnitSphere * 10f;
        rand_dir += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(rand_dir, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
