// Animal Core AI logic - Utility AI.
//
// A few times a second the animal gives every action a score and does
// whichever one scores highest:
//
//   Flee   = fleeWeight  x how close the nearest predator is
//   Hunt   = huntWeight  x how hungry it is  x how close the nearest prey is
//   Eat    = eatWeight   x how hungry it is  x how close the nearest food is
//   Drink  = drinkWeight x how thirsty it is x how close the nearest water is
//   Mate   = reproduceWeight x how well-fed it is x how close a ready mate is
//   Roam   = roamScore (fixed), so it wanders when no need is pressing
//
// Behaviour is tuned with the weights in the Inspector, not by changing code.
// Select an animal in Play mode to see its live scores in the Scene view.
//
// Simulation speed (x2, x4, x8) doesn't add extra AI updates: Update still
// runs once per frame, but every timer, need and movement speed is
// multiplied by the speed, so each frame covers more simulated time.

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AnimalBrain : MonoBehaviour
{
    public SpeciesDefinition species_data;

    [Header("Animal Status")]
    [Tooltip("100 = full, 0 = starving.")]
    [Range(0f, 100f)]
    public float curr_hunger = 100f;

    [Tooltip("100 = not thirsty, 0 = dehydrated. Only drains for species that Need a resource (e.g. Water).")]
    [Range(0f, 100f)]
    public float curr_thirst = 100f;

    public enum AnimalAction
    {
        Roaming,
        EatingGrass,
        Hunting,
        Fleeing,
        DrinkingWater,
        Reproducing
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
    public float huntRadius = 35f;

    [Tooltip("Prey notices predators within this distance.")]
    public float fleeRadius = 20f;

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
    // [AI-GENERATED] Claude Code (Claude Opus 5.5): timings follow the design spec, 1 real minute (at 1x) = 1 game day
    [Tooltip("Hunger goes from full to empty in this many seconds for plant-eaters (180 = 3 game days).")]
    public float preyStarveAfterSeconds = 180f;

    [Tooltip("Hunger goes from full to empty in this many seconds for predators (300 = 5 game days).")]
    public float predatorStarveAfterSeconds = 300f;

    [Tooltip("Thirst goes from full to empty in this many seconds (90 = 1.5 game days).")]
    public float dehydrateAfterSeconds = 90f;

    [Tooltip("Need (0-1) above which the animal searches further for food or water.")]
    [Range(0f, 1f)] public float desperateNeed = 0.6f;

    [Tooltip("Predators hungrier than this (0-1) leave their high territory to look for prey.")]
    [Range(0f, 1f)] public float predatorRoamHunger = 0.35f;

    [Tooltip("How much further a desperate animal searches (x forage radius).")]
    public float desperateSearchMultiplier = 2f;

    [Tooltip("Hunger restored by one full bite of grass (a part-empty patch gives less).")]
    public float fullBiteFill = 50f;

    [Tooltip("Thirst restored by one full drink (a part-empty patch gives less).")]
    public float fullDrinkFill = 60f;

    [Tooltip("Plant-eaters look for grass / water within this distance.")]
    public float forageRadius = 60f;

    [Tooltip("Amount taken from a grass / water patch per visit.")]
    public float biteSize = 20f;

    [Tooltip("Seconds a caught or starved animal lies still before it is removed or gets up.")]
    public float caughtDuration = 5f;

    // Reproduction
    [Header("Reproduction")]
    [Tooltip("Looks for a mate of the same species within this distance.")]
    public float mateRadius = 10f;

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): snow leopards live alone and far apart and find mates
    // by scent marks and calls, so a herd-sized radius meant they almost never met one.
    [Tooltip("Mate search distance for predators, which live alone and far apart.")]
    public float predatorMateRadius = 90f;

    float MateRadius => IsPredator ? predatorMateRadius : mateRadius;

    [Tooltip("Distance at which two mates reproduce.")]
    public float mateDistance = 2f;

    [Tooltip("Both hunger and thirst must be at least this high (0-100) before it wants to mate.")]
    [Range(0f, 100f)]
    public float reproduceMinWellFed = 60f;

    [Tooltip("Seconds before an animal can reproduce again. Also the wait after it first appears.")]
    public float reproduceCooldownSeconds = 30f;

    [Tooltip("Hunger used up by each parent when reproducing.")]
    public float reproduceHungerCost = 25f;

    [Tooltip("Seconds both mates stand still after reproducing.")]
    public float mateDuration = 2f;

    // Utility AI
    [Header("Utility AI")]
    [Tooltip("How much escaping predators matters. A predator anywhere in fleeRadius scores at least half this.")]
    public float fleeWeight = 1.5f;

    [Tooltip("How much chasing prey matters when hungry.")]
    public float huntWeight = 1f;

    [Tooltip("How much eating grass matters when hungry.")]
    public float eatWeight = 1f;

    [Tooltip("How much drinking matters when thirsty.")]
    public float drinkWeight = 1f;

    [Tooltip("How much mating matters when well-fed. Keep it below the need weights so hungry animals eat first.")]
    public float reproduceWeight = 0.8f;

    [Tooltip("Fixed score for wandering. A need has to score above this before the animal acts on it.")]
    [Min(0f)]
    public float roamScore = 0.2f;

    [Tooltip("Added to the action already running, so the animal doesn't flip between two close scores.")]
    public float commitmentBonus = 0.1f;

    [Tooltip("Seconds between decisions (scaled by simulation speed). Moving and catching still run every frame.")]
    public float decisionInterval = 0.25f;

    // Collision
    [Header("Collision")]
    [Tooltip("Animals avoid each other with the NavMeshAgent's radius. On: size it to fit this model at start, " +
             "so they don't walk through each other. Off: keep the radius set on the prefab.")]
    public bool fitAgentRadiusToModel = true;

    private float base_speed;
    private float base_acceleration;
    private float base_angular_speed;

    // Stands still while > 0 (eating, or after mating).
    private float busy_timer = 0f;
    private float decision_timer = 0f;
    private float reproduce_cooldown = 0f;
    private float caught_timer = 0f;

    // Latest score of each action, indexed by (int)AnimalAction.
    private readonly float[] scores = new float[System.Enum.GetValues(typeof(AnimalAction)).Length];

    // Targets found at the last decision.
    private Transform predator;
    private Transform prey;
    private AnimalBrain prey_brain;
    private ResourcePatch food_patch;
    private ResourcePatch water_patch;
    private Transform mate;
    private AnimalBrain mate_brain;

    // Resources this animal Eats (e.g. Grass) and Needs (e.g. Water).
    private readonly List<ResourceDefinition> food_resources = new();
    private readonly List<ResourceDefinition> water_resources = new();

    private EcosystemSimulator simulator;

    bool EventsMode =>
        simulator != null &&
        simulator.mode == EcosystemSimulator.SimulationMode.Events;

    bool IsPredator =>
        species_data != null &&
        species_data.GetSpeciesTargets(RelationshipType.Eats).Count > 0;

    // True for species that Need a resource (e.g. Water); others never get thirsty.
    public bool HasThirst => water_resources.Count > 0;

    float StarveAfter =>
        IsPredator ? predatorStarveAfterSeconds : preyStarveAfterSeconds;

    // 0 = full, 1 = starving / dehydrated.
    float HungerNeed => 1f - curr_hunger / 100f;
    float ThirstNeed => 1f - curr_thirst / 100f;

    // 0 at reproduceMinWellFed or below, 1 when both hunger and thirst are full.
    float MatingReadiness =>
        Mathf.InverseLerp(
            reproduceMinWellFed,
            100f,
            Mathf.Min(curr_hunger, curr_thirst));

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Babies: start small beside their mother, follow her, grow to full size, and can't mate until grown.
    [Header("Young")]
    [Tooltip("Seconds (at 1x) for a baby to grow to adult size.")]
    public float growUpSeconds = 60f;
    [Tooltip("Baby size as a fraction of adult size.")]
    [Range(0.2f, 1f)] public float babyScale = 0.45f;
    [Tooltip("A baby walks back to its mother when it is further than this.")]
    public float followMotherDistance = 2.5f;

    public AnimalBrain Mother { get; private set; }
    public bool IsYoung => age < growUpSeconds;
    float age = float.MaxValue;   // spawned animals are adults
    Vector3 adultScale;

    /// <summary>Turns a freshly spawned model into a newborn of this mother.</summary>
    public void MakeBaby(AnimalBrain mother)
    {
        Mother = mother;
        age = 0f;
        adultScale = transform.localScale;
        transform.localScale = adultScale * babyScale;
    }

    void GrowUp(float dt)
    {
        if (!IsYoung) return;
        age += dt;
        float t = Mathf.Clamp01(age / growUpSeconds);
        transform.localScale = adultScale * Mathf.Lerp(babyScale, 1f, t);
        if (!IsYoung) Mother = null;   // grown up: independent
    }

    bool HasMotherNearby =>
        Mother != null && Mother.isActiveAndEnabled && !Mother.IsCaught;
    // [AI-GENERATED END]

    // Off cooldown, alive and well-fed enough to mate.
    public bool CanReproduce =>
        species_data != null &&
        !IsYoung &&
        reproduce_cooldown <= 0f &&
        !IsCaught &&
        MatingReadiness > 0f;

    // Real time this animal was caught or starved (CreatureSpawner removes the latest first).
    public float CaughtTime { get; private set; } = -1f;

    // Counted in simulation time, so at x8 the body doesn't lie there 8 times longer.
    public bool IsCaught => caught_timer > 0f;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    const float DeadHoldTimer = 0.001f;

    /// <summary>Dead (Events mode) and the lie-down time is over: ready for the spawner to remove.</summary>
    public bool DeathFinished => EventsMode && IsCaught && caught_timer <= DeadHoldTimer;
    // [AI-GENERATED END]

    public void MarkCaught()
    {
        CaughtTime = Time.time;
        caught_timer = caughtDuration;
    }

    // Latest utility score of an action (for debug UI).
    public float GetScore(AnimalAction action) => scores[(int)action];

    // How far this animal's body reaches from its centre, in world units.
    // (NavMeshAgent.radius is scaled by the object's scale.)
    public float BodyRadius =>
        agent != null
            ? agent.radius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z))
            : 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            base_speed = agent.speed;
            base_acceleration = agent.acceleration;

            // Let the NavMeshAgent rotate the animal.
            agent.updateRotation = true;
            agent.angularSpeed = 360f;
            base_angular_speed = agent.angularSpeed;
        }

        audio = GetComponent<AudioSource>();

        simulationController =
            FindAnyObjectByType<SimulationSpeedController>();

        simulator =
            FindAnyObjectByType<EcosystemSimulator>();

        if (fitAgentRadiusToModel)
            FitAgentRadiusToModel();

        // Find collider to determine where the floor is.
        //Collider col = GetComponentInChildren<Collider>();

        //if (col != null && agent != null)
        //{
        //    float dist =
        //        col.bounds.min.y -
        //        transform.position.y;

        //    agent.baseOffset = -dist;
        //}

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

            food_resources.AddRange(
                species_data.GetResourceTargets(RelationshipType.Eats));

            water_resources.AddRange(
                species_data.GetResourceTargets(RelationshipType.Needs));
        }

        CurrentAction = AnimalAction.Roaming;
        CurrentTargetName = "";

        // Spread decisions out so every animal doesn't think on the same frame.
        decision_timer = Random.Range(0f, decisionInterval);

        // New animals wait before mating, and not all at the same moment.
        reproduce_cooldown = reproduceCooldownSeconds * Random.Range(0.5f, 1f);

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): procedural animation (ProcAnim module) for every animal.
        if (!TryGetComponent(out AnimalMotionBridge _))
            gameObject.AddComponent<AnimalMotionBridge>();
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

        float speed = 1f;

        if (simulationController != null)
        {
            speed =
                simulationController.CurrentSpeed;
        }

        float dt = Time.deltaTime * speed;

        // A caught animal stays still.
        if (IsCaught)
        {
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): in Events mode the death was already counted,
            // so the body stays down until CreatureSpawner removes it, instead of getting back up.
            caught_timer = EventsMode ? Mathf.Max(DeadHoldTimer, caught_timer - dt) : caught_timer - dt;
            agent.isStopped = true;
            CurrentAction = AnimalAction.Hunting;
            return;
        }

        // Faster simulation: also speed up and turn faster, so at x8 the
        // animal moves like a sped-up video instead of sliding around corners.
        // (Velocity scales with speed, so acceleration scales with speed^2.)
        agent.acceleration = base_acceleration * speed * speed;
        agent.angularSpeed = base_angular_speed * speed;

        if (UpdateNeeds(dt))
            return;

        GrowUp(dt);

        // Think a few times a second...
        decision_timer -= dt;

        if (decision_timer <= 0f)
        {
            Decide();
            decision_timer = decisionInterval;
        }

        // ...but move / catch / eat every frame.
        if (busy_timer > 0f)
        {
            busy_timer -= dt;
            agent.isStopped = true;
        }
        else
        {
            Act(speed);
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
    // COLLISION
    // ---------------------------------------------------------

    // Animals don't use physics to collide: their NavMeshAgents steer
    // around each other (local avoidance), treating each animal as a
    // circle of agent.radius. The prefabs are scaled down to 0.2, which
    // shrinks that circle too, so this sizes it to the model's footprint.
    void FitAgentRadiusToModel()
    {
        if (agent == null)
            return;

        // Footprint of every mesh, measured in this object's own space
        // (the same space agent.radius is in), so rotation doesn't matter.
        bool found = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                continue;

            Bounds b = r.localBounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(
                    b.extents,
                    new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));

                Vector3 p = transform.InverseTransformPoint(
                    r.transform.TransformPoint(corner));

                Vector2 flat = new Vector2(p.x, p.z);

                if (!found)
                {
                    min = max = flat;
                    found = true;
                }
                else
                {
                    min = Vector2.Min(min, flat);
                    max = Vector2.Max(max, flat);
                }
            }
        }

        if (!found)
            return;

        Vector2 size = max - min;

        // A circle can't match a long body exactly: use the average of
        // half the width and half the length, so bodies mostly stay apart
        // without leaving big gaps beside each other.
        agent.radius = (size.x + size.y) / 4f;
    }

    // True when this animal is within reach of other, or their bodies touch.
    // (Avoidance keeps animals about one body apart, so a fixed distance
    // smaller than that could never be reached.)
    bool InReach(Transform other, AnimalBrain otherBrain, float reach)
    {
        float touching =
            BodyRadius +
            (otherBrain != null ? otherBrain.BodyRadius : 0f) +
            0.2f;

        return Vector3.Distance(transform.position, other.position) <=
               Mathf.Max(reach, touching);
    }

    // ---------------------------------------------------------
    // NEEDS
    // ---------------------------------------------------------

    // Drains hunger and thirst. Returns true if the animal died this frame.
    bool UpdateNeeds(float dt)
    {
        if (species_data == null)
            return false;

        reproduce_cooldown -= dt;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): weather matters: heat dries animals out, hard cold burns energy
        curr_hunger = Mathf.Max(
            0f,
            curr_hunger - dt / StarveAfter * 100f * SeasonWeather.HungerDrainMultiplier);

        if (HasThirst)
        {
            curr_thirst = Mathf.Max(
                0f,
                curr_thirst - dt / dehydrateAfterSeconds * 100f * SeasonWeather.ThirstDrainMultiplier);
        }

        // Only Events mode lets animals die; in Formula mode
        // the needs still drive decisions.
        if (!EventsMode ||
            (curr_hunger > 0f && curr_thirst > 0f))
        {
            return false;
        }

        simulator.RecordStarvation(species_data);

        MarkCaught();

        // If this model isn't removed it starts again with full needs.
        curr_hunger = 100f;
        curr_thirst = 100f;

        agent.isStopped = true;

        return true;
    }

    // ---------------------------------------------------------
    // DECIDE (utility scoring)
    // ---------------------------------------------------------

    void Decide()
    {
        FindTargets();

        // Each score: weight x need x distance factor.
        // The distance factor is 0.5 at the edge of the radius and
        // 1 right next to the target, so anything in range counts.
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): fear beats hunger. Even at the edge of the flee
        // radius this outscores the best possible eat/drink/mate score (about 1.1), and it doubles up close,
        // so a hungry sheep no longer keeps grazing beside a leopard.
        scores[(int)AnimalAction.Fleeing] =
            predator != null
                ? fleeWeight * (1f + Closeness(predator.position, fleeRadius))
                : 0f;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a leopard that is even a little hungry and sees prey goes for it
        // (before, it waited until starving, so hunts were rare). A full leopard (just ate) still rests.
        scores[(int)AnimalAction.Hunting] =
            prey != null && HungerNeed > 0.15f
                ? huntWeight * (0.4f + HungerNeed) * DistanceFactor(prey.position, huntRadius)
                : 0f;

        scores[(int)AnimalAction.EatingGrass] =
            food_patch != null
                ? eatWeight * HungerNeed * DistanceFactor(food_patch.transform.position, forageRadius)
                : 0f;

        scores[(int)AnimalAction.DrinkingWater] =
            water_patch != null
                ? drinkWeight * ThirstNeed * DistanceFactor(water_patch.transform.position, forageRadius)
                : 0f;

        scores[(int)AnimalAction.Reproducing] =
            mate != null
                ? reproduceWeight * MatingReadiness * DistanceFactor(mate.position, MateRadius)
                : 0f;

        scores[(int)AnimalAction.Roaming] =
            roamScore;

        // Highest score wins (ties go to Roaming). The current action gets
        // a small bonus so a tiny change in score doesn't make it switch
        // back and forth.
        AnimalAction best = AnimalAction.Roaming;
        float best_score = float.NegativeInfinity;

        for (int i = 0; i < scores.Length; i++)
        {
            float score = scores[i];

            if (i == (int)CurrentAction && score > 0f)
                score += commitmentBonus;

            if (score > best_score)
            {
                best_score = score;
                best = (AnimalAction)i;
            }
        }

        // While eating or mating, only danger interrupts.
        if (busy_timer > 0f)
        {
            if (best != AnimalAction.Fleeing)
                return;

            busy_timer = 0f;
        }

        if (best == AnimalAction.Roaming &&
            CurrentAction != AnimalAction.Roaming)
        {
            // Pick a fresh wander point instead of finishing the old path.
            wander_timer = 0f;
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a hunt starting is the big moment to show the player
        if (best == AnimalAction.Hunting && CurrentAction != AnimalAction.Hunting && prey != null)
            DramaDirector.Report(DramaDirector.Moment.Hunt, transform,
                $"A {SpeciesLabel(species_data)} is stalking a {SpeciesLabel(prey_brain != null ? prey_brain.species_data : null)}!");

        CurrentAction = best;
        CurrentTargetName = TargetName(best);
    }

    void FindTargets()
    {
        predator = null;
        prey = null;
        prey_brain = null;
        food_patch = null;
        water_patch = null;
        mate = null;
        mate_brain = null;

        if (species_data == null)
            return;

        Vector3 pos = transform.position;

        predator = EcoRegistry.NearestPredator(
            species_data,
            pos,
            fleeRadius);

        // A caught or starved predator is no threat.
        if (predator != null &&
            predator.TryGetComponent(out AnimalBrain predatorBrain) &&
            predatorBrain.IsCaught)
        {
            predator = null;
        }

        if (IsPredator)
        {
            prey = EcoRegistry.NearestPrey(
                species_data,
                pos,
                huntRadius * Mathf.Lerp(1f, 4f, HungerNeed));   // [AI-GENERATED] Claude Code (Claude Opus 5.5): the hungrier, the further a leopard scans the slopes below

            if (prey != null)
            {
                prey.TryGetComponent(out prey_brain);

                if (prey_brain != null && prey_brain.IsCaught)
                {
                    prey = null;
                    prey_brain = null;
                }
            }
        }

        if (food_resources.Count > 0)
        {
            food_patch = ResourcePatch.FindNearest(
                pos,
                food_resources,
                SearchRadius(HungerNeed));
        }

        if (water_resources.Count > 0)
        {
            water_patch = ResourcePatch.FindNearest(
                pos,
                water_resources,
                SearchRadius(ThirstNeed));
        }

        // Nearest animal of the same species, if both are ready to mate.
        if (CanReproduce)
        {
            mate = EcoRegistry.Nearest(
                pos,
                species_data,
                MateRadius,
                transform);

            if (mate != null &&
                (!mate.TryGetComponent(out mate_brain) ||
                 !mate_brain.CanReproduce))
            {
                mate = null;
                mate_brain = null;
            }
        }
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): a very hungry or thirsty animal looks further,
    // so it travels to the next valley instead of wandering until it starves.
    float SearchRadius(float need) =>
        need >= desperateNeed ? forageRadius * desperateSearchMultiplier : forageRadius;

    // 0 at the edge of the radius, 1 right next to the target.
    float Closeness(Vector3 target, float radius) =>
        radius <= 0f ? 1f : Mathf.Clamp01(1f - Vector3.Distance(transform.position, target) / radius);

    // 1 right next to the target, 0.5 at the edge of the radius.
    float DistanceFactor(Vector3 target, float radius)
    {
        if (radius <= 0f)
            return 1f;

        float closeness =
            1f - Vector3.Distance(transform.position, target) / radius;

        return 0.5f + 0.5f * Mathf.Clamp01(closeness);
    }

    string TargetName(AnimalAction action)
    {
        switch (action)
        {
            case AnimalAction.Fleeing:
                return predator != null &&
                       predator.TryGetComponent(out AnimalBrain predatorBrain) &&
                       predatorBrain.species_data != null
                    ? predatorBrain.species_data.speciesName
                    : "Predator";

            case AnimalAction.Hunting:
                if (prey_brain != null && prey_brain.species_data != null)
                    return prey_brain.species_data.speciesName;
                return prey != null ? prey.name : "";

            case AnimalAction.EatingGrass:
                return food_patch != null && food_patch.Resource != null
                    ? food_patch.Resource.resourceName
                    : "Grass";

            case AnimalAction.DrinkingWater:
                return water_patch != null && water_patch.Resource != null
                    ? water_patch.Resource.resourceName
                    : "Water";

            case AnimalAction.Reproducing:
                return species_data != null ? species_data.speciesName : "";

            default:
                return "";
        }
    }

    // ---------------------------------------------------------
    // ACT (runs every frame for the chosen action)
    // ---------------------------------------------------------

    void Act(float speed)
    {
        bool ok;

        switch (CurrentAction)
        {
            case AnimalAction.Fleeing:
                ok = Flee(speed);
                break;

            case AnimalAction.Hunting:
                ok = Hunt(speed);
                break;

            case AnimalAction.EatingGrass:
                ok = Forage(food_patch, false, speed);
                break;

            case AnimalAction.DrinkingWater:
                ok = Forage(water_patch, true, speed);
                break;

            case AnimalAction.Reproducing:
                ok = Mate(speed);
                break;

            default:
                Wander(speed);
                ok = true;
                break;
        }

        // Target gone (eaten, caught, out of reach): decide again next frame.
        if (!ok)
            decision_timer = 0f;
    }

    // ---------------------------------------------------------
    // FLEE
    // ---------------------------------------------------------

    bool Flee(float speed)
    {
        if (predator == null)
            return false;

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
            away.normalized * 12f;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): longer dash, scaled for the big map

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
                    ? 12f
                    : -12f);

            if (!NavMesh.SamplePosition(
                    transform.position + side,
                    out hit,
                    3f,
                    NavMesh.AllAreas))
            {
                // Cornered: keep the current path until the next decision.
                return true;
            }
        }

        agent.speed =
            base_speed *
            fleeSpeedMultiplier *
            speed;

        agent.SetDestination(
            hit.position);

        return true;
    }

    // ---------------------------------------------------------
    // FORAGE / EAT / DRINK
    // ---------------------------------------------------------

    bool Forage(ResourcePatch patch, bool isWater, float speed)
    {
        if (patch == null || !patch.IsEdible)
            return false;

        Vector3 to =
            patch.transform.position -
            transform.position;

        to.y = 0f;

        // Reached resource.
        if (to.magnitude <=
            catchDistance + 0.5f)
        {
            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Fill up by what the patch actually gave: a nearly empty patch barely helps.
            float taken = patch.Consume(biteSize);
            float share = biteSize > 0f ? taken / biteSize : 1f;

            if (EventsMode)
            {
                simulator.RecordEat(
                    species_data,
                    patch.Resource,
                    taken);
            }

            if (isWater)
            {
                curr_thirst = Mathf.Min(100f, curr_thirst + share * fullDrinkFill);
                water_patch = null;
            }
            else
            {
                curr_hunger = Mathf.Min(100f, curr_hunger + share * fullBiteFill);
                food_patch = null;
            }
            // [AI-GENERATED END]

            busy_timer =
                eatDuration;

            agent.ResetPath();

            return true;
        }

        agent.speed =
            base_speed * speed;

        agent.SetDestination(
            patch.transform.position);

        return true;
    }

    // ---------------------------------------------------------
    // HUNT
    // ---------------------------------------------------------

    bool Hunt(float speed)
    {
        if (prey == null)
            return false;

        // Another predator got it first.
        if (prey_brain != null &&
            prey_brain.IsCaught)
        {
            return false;
        }

        if (InReach(prey, prey_brain, catchDistance))
        {
            // Mark prey as caught.
            if (prey_brain != null)
            {
                prey_brain.MarkCaught();
                DramaDirector.Report(DramaDirector.Moment.Catch, transform,   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
                    $"The {SpeciesLabel(species_data)} caught a {SpeciesLabel(prey_brain.species_data)}. Hunting is how it feeds its family.");

                if (EventsMode)
                {
                    simulator.RecordPredation(
                        species_data,
                        prey_brain.species_data);
                }
            }

            // Predator has successfully eaten.
            curr_hunger = 100f;

            busy_timer =
                eatDuration;

            agent.ResetPath();

            if (audio != null &&
                species_data.eat != null &&
                AudioManager.Instance != null)
            {
                AudioManager.Instance.Play3DSound(
                    audio,
                    species_data.eat);
            }

            return true;
        }

        agent.speed =
            base_speed *
            chaseSpeedMultiplier *
            speed;

        agent.SetDestination(
            prey.position);

        return true;
    }

    // ---------------------------------------------------------
    // REPRODUCE
    // ---------------------------------------------------------

    bool Mate(float speed)
    {
        // Mate gone, caught, already mated with someone else, or one of
        // the two got too hungry on the way.
        if (mate == null ||
            mate_brain == null ||
            !mate_brain.CanReproduce ||
            !CanReproduce)
        {
            return false;
        }

        if (InReach(mate, mate_brain, mateDistance))
        {
            // Only the one that arrives records the birth, so a pair
            // makes one baby, not two.
            FinishMating(true, mate_brain);
            mate_brain.FinishMating(false, this);

            return true;
        }

        agent.speed =
            base_speed * speed;

        agent.SetDestination(
            mate.position);

        return true;
    }

    void FinishMating(bool recordBirth, AnimalBrain partner)
    {
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Make the moment visible: turn to face the partner and float hearts above.
        if (partner != null)
        {
            Vector3 look = partner.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        }
        HeartPop.Spawn(transform.position + Vector3.up * HeartHeight(), recordBirth ? 3 : 2);
        if (recordBirth)
            DramaDirector.Report(DramaDirector.Moment.Mating, transform,
                $"Two {SpeciesLabel(species_data, plural: true)} are starting a family! ♥");
        // [AI-GENERATED END]

        reproduce_cooldown =
            reproduceCooldownSeconds;

        curr_hunger = Mathf.Max(
            0f,
            curr_hunger - reproduceHungerCost);

        busy_timer =
            mateDuration;

        CurrentAction =
            AnimalAction.Reproducing;

        CurrentTargetName =
            TargetName(AnimalAction.Reproducing);

        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        // Events mode: +1 to the population; CreatureSpawner adds the
        // model on its next refresh. Formula mode already grows the
        // population from reproductionRate, so nothing is recorded there.
        if (recordBirth)
            Debug.Log($"[Birth] {species_data?.name} mated (Events mode: {EventsMode}); queuing baby beside mother.", this);

        if (recordBirth && EventsMode)
        {
            CreatureSpawner.QueueBirth(this);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): baby appears beside this mother
            simulator.RecordBirth(
                species_data);
        }
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): readable species name for captions, e.g. "snow leopard"
    public static string SpeciesLabel(SpeciesDefinition s, bool plural = false)
    {
        string name = s != null && !string.IsNullOrEmpty(s.speciesName) ? s.speciesName.ToLowerInvariant() : "animal";
        return plural ? (name.EndsWith("sheep") ? name : name + "s") : name;
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): just above the model's top, whatever its size
    float HeartHeight()
    {
        var r = GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.max.y - transform.position.y + 0.3f : 1.5f;
    }

    // ---------------------------------------------------------
    // ROAM
    // ---------------------------------------------------------

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Home range as a height band of the terrain (0 = lowest valley, 1 = highest peak), set by the spawner.
    // Wandering drifts back into it, so sheep stay on the lower grassy slopes and leopards up on the snowy ridges.
    float habitatMin = 0f, habitatMax = 1f;
    public void SetHabitat(float minFraction, float maxFraction) { habitatMin = minFraction; habitatMax = maxFraction; }

    static float HeightFraction(Vector3 p)
    {
        var t = Terrain.activeTerrain;
        if (t == null || t.terrainData.size.y <= 0f) return 0.5f;
        return Mathf.Clamp01((p.y - t.GetPosition().y) / t.terrainData.size.y);
    }

    // Group home (herd area or leopard territory): wandering stays within homeRadius of it.
    Vector3 home;
    float homeRadius;   // 0 = no group
    public void SetHome(Vector3 centre, float radius) { home = centre; homeRadius = radius; }
    public void CopyHome(AnimalBrain other) { if (other != null) { home = other.home; homeRadius = other.homeRadius; } }

    float OutsideHabitat(Vector3 p)
    {
        // A hungry predator follows its prey down the mountain, like real snow leopards.
        if (IsPredator && HungerNeed >= predatorRoamHunger) return 0f;
        float f = HeightFraction(p);
        float outside = f < habitatMin ? habitatMin - f : f > habitatMax ? f - habitatMax : 0f;
        if (homeRadius > 0f)
        {
            Vector3 d = p - home; d.y = 0f;
            outside += Mathf.Max(0f, d.magnitude - homeRadius) * 0.01f;   // 10 m outside ≈ 0.1 of the height range
        }
        return outside;
    }
    // [AI-GENERATED END]

    void Wander(float speed)
    {
        agent.speed =
            base_speed * speed;

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // A baby stays close to its mother instead of wandering off.
        if (IsYoung && HasMotherNearby)
        {
            Vector3 mum = Mother.transform.position;
            if ((mum - transform.position).sqrMagnitude > followMotherDistance * followMotherDistance)
            {
                agent.speed = base_speed * speed * 1.2f;   // trots to catch up
                agent.SetDestination(mum - Mother.transform.forward * 1f);
            }
            return;
        }
        // [AI-GENERATED END]

        // Simulation speed affects wandering timer.
        wander_timer -=
            Time.deltaTime * speed;

        if (wander_timer > 0f)
            return;

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Try a few random spots and keep the one most inside the home height band.
        // Outside home, look further so the animal makes real progress back.
        bool away = OutsideHabitat(transform.position) > 0f;
        float reach = away ? 30f : 12f;
        Vector3 best = Vector3.zero;
        float bestScore = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < (away ? 8 : 4); i++)
        {
            Vector3 candidate = transform.position + Random.insideUnitSphere * reach;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas)) continue;
            float score = OutsideHabitat(hit.position);
            if (score < bestScore) { bestScore = score; best = hit.position; found = true; }
            if (score <= 0f && !away) break;   // already home: any spot inside is fine
        }

        if (found)
            agent.SetDestination(best);
        // [AI-GENERATED END]

        wander_timer =
            Random.Range(3f, 6f);
    }


    public void DeathSound()
    {
        if (species_data != null && species_data.death != null)
        {
            AudioSource.PlayClipAtPoint(species_data.death, transform.position);
        }
    }

#if UNITY_EDITOR
    // Testing aid: select an animal in Play mode to see what it chose and why.
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        string text = $"{CurrentAction}\nHunger {curr_hunger:0}  Thirst {curr_thirst:0}\n";

        for (int i = 0; i < scores.Length; i++)
            text += $"{(AnimalAction)i}: {scores[i]:0.00}\n";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, text);
    }
#endif
}
