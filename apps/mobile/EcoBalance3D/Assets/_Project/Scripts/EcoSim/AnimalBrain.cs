// Animal Core AI logic

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AnimalBrain : MonoBehaviour
{
    public SpeciesDefinition species_data;

    [Header("Animal Status")]
    [Range(0f, 100f)]
    public float curr_hunger = 100f;

    public enum AnimalAction
    {
        Roaming,
        EatingGrass,
        Hunting,
        Fleeing,
        DrinkingWater
    }

    public AnimalAction CurrentAction { get; private set; } = AnimalAction.Roaming;

    // Name of the thing the animal is currently interacting with.
    // Example: "Blue Sheep", "Grass", "Water"
    public string CurrentTargetName { get; private set; } = "";

    private NavMeshAgent agent;
    private float wander_timer = 0f;
    public AudioSource audio;
    private float footstep_timer = 0f;

    private SimulationSpeedController simulationController;

    // Predator / prey
    [Header("Predator / Prey")]
    [Tooltip("Predators notice prey within this distance.")]
    public float huntRadius = 12f;

    [Tooltip("Prey notices predators within this distance.")]
    public float fleeRadius = 7f;

    [Tooltip("Distance at which a predator catches its prey.")]
    public float catchDistance = 1.5f;

    [Tooltip("Seconds a predator stops to eat after a catch.")]
    public float eatDuration = 2f;

    [Tooltip("Speed multiplier while chasing.")]
    public float chaseSpeedMultiplier = 1.4f;

    [Tooltip("Speed multiplier while fleeing.")]
    public float fleeSpeedMultiplier = 1.25f;

    // Eat / starve
    [Header("Eat / Starve")]
    [Tooltip("Plant-eaters die if they haven't eaten or drunk for this many seconds.")]
    public float preyStarveAfterSeconds = 15f;

    [Tooltip("Predators die if they haven't caught prey for this many seconds.")]
    public float predatorStarveAfterSeconds = 20f;

    [Tooltip("Plant-eaters look for grass / water within this distance.")]
    public float forageRadius = 20f;

    [Tooltip("Amount taken from a grass / water patch per visit.")]
    public float biteSize = 20f;

    private float base_speed;
    private float eat_timer = 0f;
    private float hunger_time = 0f;

    private ResourcePatch forage_target;

    private readonly List<ResourceDefinition> wanted_resources = new();

    private EcosystemSimulator simulator;

    bool EventsMode =>
        simulator != null &&
        simulator.mode == EcosystemSimulator.SimulationMode.Events;

    bool IsPredator =>
        species_data != null &&
        species_data.GetSpeciesTargets(RelationshipType.Eats).Count > 0;

    float StarveAfter =>
        IsPredator ? predatorStarveAfterSeconds : preyStarveAfterSeconds;

    // Set when this animal is caught or starves.
    public float CaughtTime { get; private set; } = -1f;

    public bool IsCaught =>
        CaughtTime >= 0f &&
        Time.time - CaughtTime < 5f;

    public void MarkCaught()
    {
        CaughtTime = Time.time;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            base_speed = agent.speed;

            // Let the NavMeshAgent rotate the animal.
            agent.updateRotation = true;
            agent.angularSpeed = 360f;
        }

        audio = GetComponent<AudioSource>();

        simulationController =
            FindAnyObjectByType<SimulationSpeedController>();

        simulator =
            FindAnyObjectByType<EcosystemSimulator>();

        // Find collider to determine where the floor is.
        Collider col = GetComponentInChildren<Collider>();

        if (col != null && agent != null)
        {
            float dist =
                col.bounds.min.y -
                transform.position.y;

            agent.baseOffset = -dist;
        }

        // Fall back to the species the spawner assigned.
        if (species_data == null &&
            TryGetComponent(out EcoEntity entity))
        {
            species_data =
                entity.Definition as SpeciesDefinition;
        }

        if (species_data != null)
        {
            gameObject.name =
                species_data.speciesName;

            // Resources this animal eats or needs.
            wanted_resources.AddRange(
                species_data.GetResourceTargets(
                    RelationshipType.Eats));

            wanted_resources.AddRange(
                species_data.GetResourceTargets(
                    RelationshipType.Needs));
        }

        CurrentAction = AnimalAction.Roaming;
        CurrentTargetName = "";
    }

    void Update()
    {
        // Yande (7bee8ba3): skip while the agent is off the NavMesh, e.g. during an island swap.
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // Stop animal AI when simulation is paused.
        if (simulationController != null &&
            !simulationController.IsRunning)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        // A caught animal stays still.
        if (IsCaught)
        {
            agent.isStopped = true;
            CurrentAction = AnimalAction.Hunting;
            return;
        }

        float speed = 1f;

        if (simulationController != null)
        {
            speed =
                simulationController.CurrentSpeed;
        }

        // Events mode:
        // hunger_time represents how long the animal has gone
        // without eating/drinking/catching prey.
        if (EventsMode && species_data != null)
        {
            hunger_time +=
                Time.deltaTime * speed;

            // Convert the existing starvation timer into
            // a simple 0-100 hunger display.
            curr_hunger =
                Mathf.Clamp(
                    100f -
                    (hunger_time / StarveAfter * 100f),
                    0f,
                    100f);

            if (hunger_time >= StarveAfter)
            {
                hunger_time = 0f;

                simulator.RecordStarvation(
                    species_data);

                MarkCaught();

                agent.isStopped = true;

                return;
            }
        }

        // Priority:
        // eating/drinking after a successful interaction
        // > flee
        // > hunt
        // > forage
        // > roam
        if (eat_timer > 0f)
        {
            eat_timer -=
                Time.deltaTime * speed;

            agent.isStopped = true;
        }
        else if (!Flee(speed) &&
                 !Hunt(speed) &&
                 !Forage(speed))
        {
            agent.speed =
                base_speed * speed;

            CurrentAction =
                AnimalAction.Roaming;

            CurrentTargetName = "";

            // Simulation speed affects wandering timer.
            wander_timer -=
                Time.deltaTime * speed;

            if (wander_timer <= 0f)
            {
                // Hungry plant-eaters look for
                // grass / water.
                if (wanted_resources.Count > 0 &&
                    (!EventsMode ||
                     hunger_time >
                     StarveAfter * 0.3f ||
                     Random.value < 0.3f))
                {
                    forage_target =
                        ResourcePatch.FindNearest(
                            transform.position,
                            wanted_resources,
                            forageRadius);
                }

                if (forage_target == null)
                {
                    Roam();
                }

                wander_timer =
                    Random.Range(3f, 6f);
            }
        }

        // Footstep audio.
        if (agent.velocity.magnitude > 0.1f)
        {
            footstep_timer -=
                Time.deltaTime;

            if (footstep_timer <= 0f)
            {
                if (audio != null &&
                    audio.clip != null &&
                    AudioManager.Instance != null)
                {
                    AudioManager.Instance.Play3DSound(
                        audio,
                        audio.clip);
                }

                footstep_timer =
                    Random.Range(0.4f, 0.6f);
            }
        }
    }

    // ---------------------------------------------------------
    // FLEE
    // ---------------------------------------------------------

    bool Flee(float speed)
    {
        if (species_data == null)
            return false;

        Transform predator =
            EcoRegistry.NearestPredator(
                species_data,
                transform.position,
                fleeRadius);

        if (predator == null)
            return false;

        CurrentAction =
            AnimalAction.Fleeing;

        CurrentTargetName =
            predator.GetComponent<AnimalBrain>() != null
                ? predator.GetComponent<AnimalBrain>()
                    .species_data.speciesName
                : "Predator";

        Vector3 away =
            transform.position -
            predator.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
        {
            away =
                Random.insideUnitSphere;

            away.y = 0f;
        }

        Vector3 target =
            transform.position +
            away.normalized * 6f;

        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
                target,
                out hit,
                2f,
                NavMesh.AllAreas))
        {
            Vector3 side =
                Vector3.Cross(
                    Vector3.up,
                    away.normalized) *
                (Random.value < 0.5f
                    ? 6f
                    : -6f);

            if (!NavMesh.SamplePosition(
                    transform.position + side,
                    out hit,
                    3f,
                    NavMesh.AllAreas))
            {
                return false;
            }
        }

        agent.speed =
            base_speed *
            fleeSpeedMultiplier *
            speed;

        agent.SetDestination(
            hit.position);

        wander_timer = 0.5f;

        return true;
    }

    // ---------------------------------------------------------
    // FORAGE / EAT / DRINK
    // ---------------------------------------------------------

    bool Forage(float speed)
    {
        if (forage_target == null)
            return false;

        if (!forage_target.IsEdible)
        {
            forage_target = null;
            return false;
        }

        // Determine whether this resource is food or water.
        Relationship resourceRelationship =
            species_data != null
                ? species_data.FindRelationship(
                    forage_target.Resource)
                : null;

        bool isWater =
            resourceRelationship != null &&
            resourceRelationship.type ==
            RelationshipType.Needs;

        // Update current action.
        if (isWater)
        {
            CurrentAction =
                AnimalAction.DrinkingWater;
        }
        else
        {
            CurrentAction =
                AnimalAction.EatingGrass;
        }

        CurrentTargetName =
            forage_target.Resource != null
                ? forage_target.Resource.resourceName
                : (isWater ? "Water" : "Grass");

        Vector3 to =
            forage_target.transform.position -
            transform.position;

        to.y = 0f;

        // Reached resource.
        if (to.magnitude <=
            catchDistance + 0.5f)
        {
            forage_target.Consume(
                biteSize);

            if (EventsMode)
            {
                simulator.RecordEat(
                    species_data,
                    forage_target.Resource,
                    biteSize);
            }

            // Reset hunger after eating/drinking.
            hunger_time = 0f;
            curr_hunger = 100f;

            forage_target = null;

            eat_timer =
                eatDuration;

            agent.ResetPath();

            return true;
        }

        agent.speed =
            base_speed * speed;

        agent.SetDestination(
            forage_target.transform.position);

        return true;
    }

    // ---------------------------------------------------------
    // HUNT
    // ---------------------------------------------------------

    bool Hunt(float speed)
    {
        if (species_data == null)
            return false;

        Transform prey =
            EcoRegistry.NearestPrey(
                species_data,
                transform.position,
                huntRadius);

        if (prey == null)
            return false;

        prey.TryGetComponent(
            out AnimalBrain preyBrain);

        if (preyBrain != null &&
            preyBrain.IsCaught)
        {
            return false;
        }

        CurrentAction =
            AnimalAction.Hunting;

        if (preyBrain != null &&
            preyBrain.species_data != null)
        {
            CurrentTargetName =
                preyBrain.species_data.speciesName;
        }
        else
        {
            CurrentTargetName =
                prey.name;
        }

        if (Vector3.Distance(
                transform.position,
                prey.position)
            <= catchDistance)
        {
            // Mark prey as caught.
            if (preyBrain != null)
            {
                preyBrain.MarkCaught();

                if (EventsMode)
                {
                    simulator.RecordPredation(
                        species_data,
                        preyBrain.species_data);
                }
            }

            // Predator has successfully eaten.
            hunger_time = 0f;
            curr_hunger = 100f;

            eat_timer =
                eatDuration;

            agent.ResetPath();

            return true;
        }

        agent.speed =
            base_speed *
            chaseSpeedMultiplier *
            speed;

        agent.SetDestination(
            prey.position);

        wander_timer = 0.5f;

        if (Vector3.Distance(transform.position, prey.position) <= catchDistance) {
            eat_timer = eatDuration;
            agent.ResetPath();

            if (audio != null && species_data != null && species_data.eat != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.Play3DSound(audio, species_data.eat);
            }
            return true;
        }

            return true;
    }

    // ---------------------------------------------------------
    // ROAM
    // ---------------------------------------------------------

    void Roam()
    {
        CurrentAction =
            AnimalAction.Roaming;

        CurrentTargetName = "";

        Vector3 rand_dir =
            Random.insideUnitSphere * 10f;

        rand_dir +=
            transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
                rand_dir,
                out hit,
                10f,
                NavMesh.AllAreas))
        {
            agent.SetDestination(
                hit.position);
        }
    }


    public void DeathSound()
    {
        if (species_data != null && species_data.death != null)
        {
            AudioSource.PlayClipAtPoint(species_data.death, transform.position);
        }
    }

}
