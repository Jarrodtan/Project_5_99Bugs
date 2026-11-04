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
    // [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5): how far animals notice each other is set
    // only on AnimalPerception now (vision range, angle, hearing, stalker spotting).
    [Header("Predator / Prey")]
    [Tooltip("Distance at which a predator catches its prey.")]
    public float catchDistance = 1.5f;

    [Tooltip("Seconds a predator stops to eat after a catch.")]
    public float eatDuration = 2f;

    [Tooltip("Speed multiplier while chasing.")]
    public float chaseSpeedMultiplier = 1.4f;

    [Tooltip("Speed multiplier while fleeing.")]
    public float fleeSpeedMultiplier = 1.25f;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Kept from WeiLeong on top of Yusuf's stalk/sprint: research hunt rules, Guided hunt bonus.
    // Real snow leopard hunts: only when hungry, creep up low and slow using the slope for cover,
    // then a short fast sprint. Prey only flee from a predator they can actually see.
    [Header("Stalk and pounce")]
    [Tooltip("Only hunts when hunger is below this (100 = full), so a leopard that just ate leaves the herd alone.")]
    [Range(0f, 100f)] public float huntBelowHunger = 60f;
    [Tooltip("While stalking, moves at this fraction of walking speed.")]
    public float stalkSpeedMultiplier = 0.45f;
    [Tooltip("Starts the sprint when this close, or as soon as the prey runs.")]
    public float pounceDistance = 8f;
    [Tooltip("Sprint speed (snow leopards are much faster than blue sheep over a short dash).")]
    public float sprintSpeedMultiplier = 2.2f;
    [Tooltip("Gives up a sprint after this many seconds (at 1x) and rests, like real leopards.")]
    public float maxSprintSeconds = 6f;
    [Tooltip("Chance that reaching the prey ends in a catch (Shumin's research: 70%).")]
    [Range(0f, 1f)] public float huntSuccessRate = 0.7f;
    [Tooltip("Rest after a missed catch (research: 1 simulated day = 120 s at 1x).")]
    public float huntCooldownSeconds = 120f;
    [Tooltip("Rest after a failed hunt (seconds at 1x).")]
    public float giveUpRestSeconds = 20f;

    public bool IsStalking { get; private set; }
    public bool IsFleeing => CurrentAction == AnimalAction.Fleeing;
    float sprintTimer, huntRestTimer;
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // Losing sight of the prey is a normal part of a real snow leopard's stalk: it creeps in behind ridges
    // and rocks so the prey can't see it either. Like other cats, it remembers where the prey went out of
    // sight and keeps going there, low and slow. If nothing is there when it arrives, or it has waited too
    // long, it gives up and rests instead of searching or chasing far.
    [Tooltip("Seconds (at 1x) a hunting predator remembers where it last saw its prey and keeps creeping " +
             "towards that spot. 0 = gives up the moment it loses sight.")]
    [Min(0f)] public float preyMemorySeconds = 15f;

    Vector3 last_prey_position;
    float prey_memory_timer;
    AnimalBrain remembered_prey;

    // Still hunting a prey it can't see right now, from memory.
    bool SearchingForPrey =>
        prey == null && prey_memory_timer > 0f && remembered_prey != null && !remembered_prey.IsCaught;

    void ForgetPrey()
    {
        prey_memory_timer = 0f;
        remembered_prey = null;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // Camouflage: a pale, spotted snow leopard blends into snow and grey rock but stands out on green grass.
    // Prey see it from this many times their normal distance, by the ground it is standing on
    // (a mix of ground types blends the values). Hearing / smell up close (AnimalPerception) isn't affected.
    [Header("Camouflage (predators)")]
    [Tooltip("Spotted from this many times the normal distance when standing on grass (stands out).")]
    [Range(0.1f, 2f)] public float visibilityOnGrass = 1.4f;
    [Tooltip("... on rock (blends in fairly well).")]
    [Range(0.1f, 2f)] public float visibilityOnRock = 0.75f;
    [Tooltip("... on snow (almost invisible).")]
    [Range(0.1f, 2f)] public float visibilityOnSnow = 0.45f;

    /// <summary>Largest visibility value, so perception knows how far out to look.</summary>
    public const float MaxVisibility = 2f;

    /// <summary>
    /// How easy this animal is to spot where it stands now: 1 = normal, below 1 = camouflaged,
    /// above 1 = stands out. Only predators are camouflaged; everything else is 1.
    /// </summary>
    public float Visibility
    {
        get
        {
            if (!IsPredator || !TerrainCover.TryGet(transform.position, out var c)) return 1f;
            float sum = c.grass + c.rock + c.snow;
            if (sum <= 0.001f) return 1f;
            float v = (c.grass * visibilityOnGrass + c.rock * visibilityOnRock + c.snow * visibilityOnSnow) / sum;
            // Fast movement gives it away: a sprinting leopard is seen at least as easily as normal.
            bool sprinting = CurrentAction == AnimalAction.Hunting && !IsStalking;
            return sprinting ? Mathf.Max(1f, v) : v;
        }
    }

    [Header("Territory (predators)")]
    [Tooltip("How strongly a predator picks snowy ground to roam and rest on, where it is camouflaged. 0 = doesn't care.")]
    [Min(0f)] public float snowPreference = 2f;
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    [Header("Herd alarm (prey)")]
    [Tooltip("Seconds (at 1x) a herd mate runs after hearing an alarm call, even without seeing the predator.")]
    public float alarmFleeSeconds = 2.5f;

    Vector3 alarm_position;
    float alarm_flee_timer;

    bool HasThreat => predator != null || alarm_flee_timer > 0f;
    Vector3 ThreatPosition => predator != null ? predator.position : alarm_position;
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // Prey remember where they saw a predator and keep away from there for a while, instead of
    // forgetting it the moment it is out of sight and grazing their way straight back to it.
    [Header("Danger memory (prey)")]
    [Tooltip("Seconds (at 1x) a predator sighting is remembered. 60 = one game day. 0 = no memory.")]
    [Min(0f)] public float dangerMemorySeconds = 60f;

    [Tooltip("Area around a sighting that feels dangerous. Grass, water and wander spots inside it are avoided.")]
    [Min(0f)] public float dangerRadius = 25f;

    [Tooltip("When choosing grass / water, a patch right on a fresh sighting counts as this many metres further away.")]
    [Min(0f)] public float dangerDetour = 60f;

    [Tooltip("How much a remembered danger lowers the urge to eat or drink there (0 = not at all, 1 = only when desperate).")]
    [Range(0f, 1f)] public float dangerAppetiteCut = 0.6f;

    [Tooltip("Herd mates within this distance hear the alarm and remember the sighting too.")]
    [Min(0f)] public float alarmRadius = 15f;

    [Tooltip("Most sightings remembered at once; the oldest is forgotten first.")]
    [Min(1)] public int maxDangerSpots = 4;

    readonly DangerMemory danger_memory = new();
    System.Func<Vector3, float> danger_detour_cost;
    float alarm_timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => all.Clear();

    /// <summary>Remembered predator sightings, for debug views.</summary>
    public IReadOnlyList<DangerMemory.Spot> DangerSpots => danger_memory.Spots;

    /// <summary>0..1, how dangerous this animal thinks a spot is from the predators it remembers.</summary>
    public float DangerAt(Vector3 p) => danger_memory.DangerAt(p, dangerRadius, dangerMemorySeconds);

    /// <summary>Hears a herd mate's alarm: runs from the predator's position and remembers it.</summary>
    public void HearAlarm(Vector3 predatorPosition)
    {
        alarm_position = predatorPosition;
        alarm_flee_timer = alarmFleeSeconds;
        if (dangerMemorySeconds > 0f)
            danger_memory.Remember(predatorPosition, dangerMemorySeconds, dangerRadius * 0.5f, maxDangerSpots);
    }

    // Called at each decision while a predator is in sight.
    void RememberPredator(Vector3 predatorPosition, float dt)
    {
        bool isNew = dangerMemorySeconds > 0f &&
                     danger_memory.Remember(predatorPosition, dangerMemorySeconds, dangerRadius * 0.5f, maxDangerSpots);

        // Alarm call: at once for a new spot, then about once a second while it stays in view.
        alarm_timer -= dt;
        if (!isNew && alarm_timer > 0f) return;
        alarm_timer = 1f;

        float alarmSqr = alarmRadius * alarmRadius;
        foreach (var other in all)
        {
            if (other == this || other.species_data != species_data || other.IsCaught) continue;
            if ((other.transform.position - transform.position).sqrMagnitude <= alarmSqr)
                other.HearAlarm(predatorPosition);
        }
    }

    // Extra "metres" added to a patch's distance when choosing where to eat or drink.
    float DangerDetourCost(Vector3 p) => DangerAt(p) * dangerDetour;

    // Multiplier on an eat / drink score for a patch in remembered danger.
    float AppetiteNear(Vector3 p) => 1f - dangerAppetiteCut * DangerAt(p);
    // [AI-GENERATED END]

    // Eat / starve
    [Header("Eat / Starve")]
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Shumin's research (Simulation Logic): Fullness and Hydration drop by a fixed amount per simulated day,
    // animals look for food / water at 50% or less, and health (HP) only falls once one of them is empty.
    // Timings are in real seconds at 1x for a 120 s game day; they are scaled to the clock's day length (DayScale).
    [Tooltip("Fullness 100 -> 0 in this many seconds for plant-eaters (research: -20 per day = 600 s).")]
    public float preyStarveAfterSeconds = 600f;

    [Tooltip("Fullness 100 -> 0 in this many seconds for predators (research: -12 per day = 1000 s).")]
    public float predatorStarveAfterSeconds = 1000f;

    [Tooltip("Hydration 100 -> 0 in this many seconds for plant-eaters (research: -20 per day = 600 s).")]
    public float dehydrateAfterSeconds = 600f;

    [Tooltip("Hydration 100 -> 0 in this many seconds for predators (research: -12 per day = 1000 s).")]
    public float predatorDehydrateAfterSeconds = 1000f;

    [Tooltip("Starts looking for food when Fullness is at or below this (research: 50%).")]
    [Range(0f, 100f)] public float seekFoodBelow = 50f;

    [Tooltip("Starts looking for water when Hydration is at or below this (research: 50%).")]
    [Range(0f, 100f)] public float seekWaterBelow = 50f;

    [Header("Health")]
    [Tooltip("Health points (research: 100 HP; dies at 0).")]
    public float maxHealth = 100f;
    [Tooltip("HP lost per simulated day while Fullness is 0, and again while Hydration is 0 (research: 20 each, 40 for both).")]
    public float emptyNeedDamagePerDay = 20f;
    [Tooltip("HP regained per simulated day while both Fullness and Hydration are above 50% (0 = never heals).")]
    public float healPerDay = 10f;
    [Tooltip("Food a successful hunt gives a predator (research: +80).")]
    public float huntFill = 80f;

    // The second-based timings above and below were written for 120-second days. They are scaled by the
    // clock's real day length, so "5 days to starve" stays 5 game days whatever ClockConfig says (60 s since 3 Nov).
    const float AuthoredDaySeconds = 120f;
    static float DaySeconds => ClockService.DaySeconds;
    static float DayScale => DaySeconds / AuthoredDaySeconds;
    public float curr_health = 100f;
    float DehydrateAfter => (IsPredator ? predatorDehydrateAfterSeconds : dehydrateAfterSeconds) * DayScale;

    /// <summary>Guided events: change health directly (illness -20, help +15), capped at max.</summary>
    public void ChangeHealth(float amount) => curr_health = Mathf.Clamp(curr_health + amount, 0f, maxHealth);
    // [AI-GENERATED END]

    [Tooltip("Need (0-1) above which the animal searches further for food or water.")]
    [Range(0f, 1f)] public float desperateNeed = 0.6f;

    [Tooltip("Predators hungrier than this (0-1) leave their high territory to look for prey.")]
    [Range(0f, 1f)] public float predatorRoamHunger = 0.35f;

    [Tooltip("How much further a desperate animal searches (x forage radius).")]
    public float desperateSearchMultiplier = 2f;

    [Tooltip("Hunger restored by one full bite of grass (a part-empty patch gives less).")]
    public float fullBiteFill = 40f;   // research: +40 per graze

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
    public float predatorMateRadius = 1000f;   // the whole valley: a ready pair seeks each other out

    float MateRadius => IsPredator ? predatorMateRadius : mateRadius;

    [Tooltip("Distance at which two mates reproduce.")]
    public float mateDistance = 2f;

    [Tooltip("Both hunger and thirst must be at least this high (0-100) before it wants to mate.")]
    [Range(0f, 100f)]
    public float reproduceMinWellFed = 70f;   // research: Fullness of at least 70%

    [Tooltip("Seconds before a plant-eater pair can breed again. Also the wait after it first appears (research: every 5 days = 600 s).")]
    public float reproduceCooldownSeconds = 600f;

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): research - snow leopards breed far less often than blue sheep.
    [Tooltip("Same for predators (research: every 12 days = 1440 s).")]
    public float predatorReproduceCooldownSeconds = 1440f;
    float ReproduceCooldown => (IsPredator ? predatorReproduceCooldownSeconds : reproduceCooldownSeconds) * DayScale;

    [Tooltip("Hunger used up by each parent when reproducing.")]
    public float reproduceHungerCost = 25f;

    [Tooltip("Seconds both mates stand still after reproducing.")]
    public float mateDuration = 2f;

    // Utility AI
    [Header("Utility AI")]
    [Tooltip("How much escaping predators matters. Any predator in sight scores at least this, up to double when close.")]
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

    // [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5): the only way this animal finds predators
    // and prey. Added at Start if the prefab doesn't have one.
    private AnimalPerception perception;

    /// <summary>This animal's vision (what predators and prey it can see).</summary>
    public AnimalPerception Perception => perception;

    /// <summary>Eating or drinking with its head down, so slower to spot a stalking predator.</summary>
    public bool IsHeadDown =>
        busy_timer > 0f &&
        (CurrentAction == AnimalAction.EatingGrass || CurrentAction == AnimalAction.DrinkingWater);

    // How far this animal sees (0 before Start).
    float VisionRange => perception != null ? perception.visionRange : 0f;

    bool EventsMode =>
        simulator != null &&
        simulator.mode == EcosystemSimulator.SimulationMode.Events;

    bool IsPredator =>
        species_data != null &&
        species_data.GetSpeciesTargets(RelationshipType.Eats).Count > 0;

    // True for species that Need a resource (e.g. Water); others never get thirsty.
    public bool HasThirst => water_resources.Count > 0;

    float StarveAfter =>
        (IsPredator ? predatorStarveAfterSeconds : preyStarveAfterSeconds) * DayScale;

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
    public bool IsYoung => age < growUpSeconds * DayScale;
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
        float t = Mathf.Clamp01(age / (growUpSeconds * DayScale));
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

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): patches this animal found it can't walk to, and until when to skip them.
    readonly Dictionary<ResourcePatch, float> unreachable = new Dictionary<ResourcePatch, float>();
    bool IsUnreachable(ResourcePatch p) =>
        p != null && unreachable.TryGetValue(p, out var until) && Time.time < until;

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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Every animal is male or female (half and half); only a male and a female can mate.
    public enum Sex { Female, Male }
    public Sex AnimalSex { get; private set; }
    bool sexChosen;
    public string SexSymbol => AnimalSex == Sex.Male ? "♂" : "♀";   // ♂ / ♀

    static readonly List<AnimalBrain> all = new List<AnimalBrain>();
    void OnEnable()
    {
        if (!sexChosen) { AnimalSex = Random.value < 0.5f ? Sex.Female : Sex.Male; sexChosen = true; }   // Random isn't allowed in field initialisers
        all.Add(this);
    }
    void OnDisable() { all.Remove(this); }

    // A pure coin flip gave the 2 starting leopards the same sex 1 time in 2, so they could never breed.
    // Once the species is known, take whichever sex this species has fewer of (coin flip on a tie), so
    // the starting animals are always split as evenly as possible. Babies get the same rule.
    bool sexBalanced;
    void BalanceSex()
    {
        if (sexBalanced || species_data == null) return;
        sexBalanced = true;
        int males = 0, females = 0;
        foreach (var other in all)
        {
            if (other == this || other == null || other.IsCaught || !other.sexBalanced || other.species_data != species_data) continue;
            if (other.AnimalSex == Sex.Male) males++; else females++;
        }
        if (males != females) AnimalSex = males < females ? Sex.Male : Sex.Female;
    }

    /// <summary>Living animals right now (for Guided Exploration counts, events and endings).</summary>
    public static IReadOnlyList<AnimalBrain> All => all;
    public bool IsPredatorSpecies => IsPredator;

    /// <summary>Guided "support the animals": adds fullness and hydration points (0-100 scale), capped at 100.</summary>
    public void Refresh(float fullness, float hydration)
    {
        curr_hunger = Mathf.Min(100f, curr_hunger + fullness);
        curr_thirst = Mathf.Min(100f, curr_thirst + hydration);
    }

    // Nearest ready animal of the same species and the other sex.
    Transform NearestMate(Vector3 pos, float radius)
    {
        Transform best = null; float bestSq = radius * radius;
        foreach (var o in all)
        {
            if (o == this || o.species_data != species_data || o.AnimalSex == AnimalSex || !o.CanReproduce) continue;
            float d = (o.transform.position - pos).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = o.transform; }
        }
        return best;
    }
    // [AI-GENERATED END]

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<AnimalPerception>();
        if (perception == null)
            perception = gameObject.AddComponent<AnimalPerception>();   // [AI-GENERATED] Claude Code (Claude Opus 5.5): default vision

        if (agent != null)
        {
            base_speed = agent.speed;
            base_acceleration = agent.acceleration;

            // (Yande, PR #51) One brain drives the agent. A second brain on the same object
            // (the IslandPlatform sheep, the snow leopard prefab) fights every path.
            var otherBrain = GetComponent<GoalOrientedAnimalBrain>();
            if (otherBrain != null)
                otherBrain.enabled = false;
            agent.autoBraking = false;
            agent.stoppingDistance = 0.6f;

            // Let the NavMeshAgent rotate the animal.
            agent.updateRotation = true;
            agent.angularSpeed = 360f;
            base_angular_speed = agent.angularSpeed;
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): with everyone at the same priority, agents in a herd
            // keep shoving each other and shake on the spot. Different priorities let one give way.
            agent.avoidancePriority = Random.Range(30, 70);
        }

        audio = GetComponent<AudioSource>();

        simulationController =
            FindAnyObjectByType<SimulationSpeedController>();

        simulator =
            FindAnyObjectByType<EcosystemSimulator>();

        if (fitAgentRadiusToModel)
            FitAgentRadiusToModel();

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

            food_resources.AddRange(
                species_data.GetResourceTargets(RelationshipType.Eats));

            water_resources.AddRange(
                species_data.GetResourceTargets(RelationshipType.Needs));
        }

        BalanceSex();

        CurrentAction = AnimalAction.Roaming;
        CurrentTargetName = "";

        danger_detour_cost = DangerDetourCost;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): cached so patch searches don't allocate

        // Spread decisions out so every animal doesn't think on the same frame.
        decision_timer = Random.Range(0f, decisionInterval);

        // New animals wait before mating, and not all at the same moment. Starting animals are adults, so
        // only a short wait (leopards about 2-4 days instead of 6-12, which left at most one try in a
        // 20-day Guided run); babies are kept from mating by IsYoung until they grow up.
        reproduce_cooldown = ReproduceCooldown * Random.Range(0.15f, 0.35f);

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
        danger_memory.Tick(dt);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): sightings fade in simulation time
        alarm_flee_timer -= dt;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

        // Think a few times a second...
        decision_timer -= dt;

        if (decision_timer <= 0f)
        {
            Decide();
            decision_timer = decisionInterval;
        }

        // ...but move / catch / eat every frame.
        if (huntRestTimer > 0f) huntRestTimer -= dt;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
        if (prey_memory_timer > 0f) prey_memory_timer -= dt;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
        if (CurrentAction != AnimalAction.Hunting && (IsStalking || sprintTimer > 0f)) { sprintTimer = 0f; SetCrouch(false); }

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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // At x8 both animals move several metres per frame, so a leopard can pass right by a sheep between two
    // frames without ever ending a frame in reach. This also checks the closest they came during the frame.
    Vector3 sweep_my_prev, sweep_other_prev;
    Transform sweep_other;
    int sweep_frame = -10;

    bool InReachSwept(Transform other, AnimalBrain otherBrain, float reach)
    {
        // Only sweep from last frame's positions, never from a chase that stopped a while ago.
        bool hadPrev = sweep_other == other && sweep_frame == Time.frameCount - 1;
        Vector3 relPrev = sweep_other_prev - sweep_my_prev;
        Vector3 relNow = other.position - transform.position;
        sweep_frame = Time.frameCount;
        sweep_other = other;
        sweep_my_prev = transform.position;
        sweep_other_prev = other.position;

        if (InReach(other, otherBrain, reach)) return true;
        if (!hadPrev) return false;

        float touching = BodyRadius + (otherBrain != null ? otherBrain.BodyRadius : 0f) + 0.2f;
        float r = Mathf.Max(reach, touching);
        relPrev.y = relNow.y = 0f;
        // Closest point to the origin on the segment relPrev -> relNow (relative motion this frame).
        Vector3 seg = relNow - relPrev;
        float t = seg.sqrMagnitude > 1e-6f ? Mathf.Clamp01(-Vector3.Dot(relPrev, seg) / seg.sqrMagnitude) : 0f;
        return (relPrev + seg * t).sqrMagnitude <= r * r;
    }

    // Only asks for a new path when the target really moved: paths are computed a few per frame, so many
    // animals re-pathing every frame queue up and lag, which at x8 is 8x as much simulated time.
    void SetDestinationIfMoved(Vector3 target, float tolerance)
    {
        if (agent.hasPath || agent.pathPending)
        {
            Vector3 d = agent.destination - target;
            d.y = 0f;
            if (d.sqrMagnitude <= tolerance * tolerance) return;
        }
        agent.SetDestination(target);
    }
    // [AI-GENERATED END]

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
            curr_hunger - dt / StarveAfter * 100f * SeasonWeather.HungerDrainMultiplier * GuidedEffects.Hunger(IsPredator));

        if (HasThirst)
        {
            curr_thirst = Mathf.Max(
                0f,
                curr_thirst - dt / DehydrateAfter * 100f * SeasonWeather.ThirstDrainMultiplier * GuidedEffects.Thirst(IsPredator));
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): research health rule. Empty food and empty water each
        // cost emptyNeedDamagePerDay; a well-fed, watered animal slowly recovers.
        float empty = (curr_hunger <= 0f ? 1f : 0f) + (HasThirst && curr_thirst <= 0f ? 1f : 0f);
        if (empty > 0f) curr_health -= empty * emptyNeedDamagePerDay * dt / DaySeconds;
        else if (curr_hunger > 50f && (!HasThirst || curr_thirst > 50f)) curr_health += healPerDay * dt / DaySeconds;
        curr_health = Mathf.Clamp(curr_health, 0f, maxHealth);

        // Only Events mode lets animals die; in Formula mode
        // the needs still drive decisions.
        if (!EventsMode || curr_health > 0f)
        {
            return false;
        }

        simulator.RecordStarvation(species_data);

        MarkCaught();

        // If this model isn't removed it starts again with full needs.
        curr_hunger = 100f;
        curr_thirst = 100f;
        curr_health = maxHealth;

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
        // A herd mate's alarm call (no predator seen yet) scores like a predator at the edge of the flee radius.
        scores[(int)AnimalAction.Fleeing] =
            HasThreat
                ? fleeWeight * (1f + (predator != null ? Closeness(predator.position, VisionRange) : 0f))
                : 0f;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a leopard that is even a little hungry and sees prey goes for it
        // (before, it waited until starving, so hunts were rare). A full leopard (just ate) still rests,
        // and so does one catching its breath after a failed sprint.
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a hunt already under way carries on from memory when the
        // prey goes out of sight (it only ever starts on a prey the predator can see).
        bool huntFromMemory = SearchingForPrey && CurrentAction == AnimalAction.Hunting;
        scores[(int)AnimalAction.Hunting] =
            (prey != null || huntFromMemory) && curr_hunger < huntBelowHunger && huntRestTimer <= 0f
                ? huntWeight * (0.4f + HungerNeed) *
                  DistanceFactor(prey != null ? prey.position : last_prey_position, VisionRange)
                : 0f;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): AppetiteNear makes a patch where a predator was seen
        // less tempting, so only a really hungry / thirsty animal goes back there before it forgets.
        scores[(int)AnimalAction.EatingGrass] =
            food_patch != null && curr_hunger <= seekFoodBelow   // research: look for food at 50% or less
                ? eatWeight * HungerNeed * DistanceFactor(food_patch.transform.position, forageRadius)
                  * AppetiteNear(food_patch.transform.position)
                : 0f;

        scores[(int)AnimalAction.DrinkingWater] =
            water_patch != null && curr_thirst <= seekWaterBelow   // research: look for water at 50% or less
                ? drinkWeight * ThirstNeed * DistanceFactor(water_patch.transform.position, forageRadius)
                  * AppetiteNear(water_patch.transform.position)
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
            has_patrol = false;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): a hunt replaced the patrol path
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a hunt starting is the big moment to show the player
        if (best == AnimalAction.Hunting && CurrentAction != AnimalAction.Hunting)
        {
            sprintTimer = 0f;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): every hunt starts with a stalk and a fresh sprint
        }

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

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
        // Predators and prey come only from what this animal can see (AnimalPerception): inside its vision
        // cone and range, not behind a hill, camouflage and stalking taken into account, or close enough to
        // hear / smell. Scanned here, once per decision, so it runs in simulation time.
        if (perception != null)
        {
            perception.Scan();

            AnimalBrain seenPredator = perception.NearestPredator;
            if (seenPredator != null)
                predator = seenPredator.transform;

            if (IsPredator && perception.NearestPrey != null)
            {
                prey_brain = perception.NearestPrey;
                prey = prey_brain.transform;
            }
        }

        // Remember where the prey was last seen, for when it goes out of sight.
        if (prey_brain != null)
        {
            remembered_prey = prey_brain;
            last_prey_position = prey.position;
            prey_memory_timer = preyMemorySeconds;
        }
        else if (remembered_prey != null && remembered_prey.IsCaught)
        {
            ForgetPrey();   // caught by another predator, or starved
        }
        // [AI-GENERATED END]

        if (predator != null)
            RememberPredator(predator.position, decisionInterval);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

        if (food_resources.Count > 0)
        {
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): with a remembered predator, a patch away from it beats a closer one beside it
            food_patch = ResourcePatch.FindBest(
                pos,
                food_resources,
                SearchRadius(HungerNeed),
                danger_memory.Count > 0 ? danger_detour_cost : null,
                IsUnreachable);
        }

        if (water_resources.Count > 0)
        {
            water_patch = ResourcePatch.FindBest(
                pos,
                water_resources,
                SearchRadius(ThirstNeed),
                danger_memory.Count > 0 ? danger_detour_cost : null,
                IsUnreachable);
        }

        // Nearest animal of the same species, if both are ready to mate.
        if (CanReproduce)
        {
            mate = NearestMate(pos, MateRadius);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): male + female only

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
                if (prey != null)
                    return prey.name;
                // [AI-GENERATED] Claude Code (Claude Opus 5.5): out of sight, still hunting it from memory
                return remembered_prey != null && remembered_prey.species_data != null
                    ? remembered_prey.species_data.speciesName
                    : "";

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

    int fleeSide;

    bool Flee(float speed)
    {
        if (!HasThreat)
        {
            fleeSide = 0;
            return false;
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): runs from the predator it sees, or from where a herd mate's alarm came from
        Vector3 away =
            transform.position -
            ThreatPosition;

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
                (fleeSide == 0 ? (fleeSide = Random.value < 0.5f ? 1 : -1) : fleeSide) * 12f;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): pick a side once, not every frame

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

        SetDestinationIfMoved(hit.position, 2f);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): don't re-path every frame

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

        // Reached resource. [AI-GENERATED] Claude Code (Claude Opus 5.5): also counts when the agent has stopped
        // close by because others crowd the patch, instead of jostling forever trying to reach the centre.
        bool crowdedStop = to.magnitude <= catchDistance + 2.5f && agent.hasPath && !agent.pathPending &&
                           agent.pathStatus == NavMeshPathStatus.PathComplete && agent.velocity.sqrMagnitude < 0.04f;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the patch can't be reached (across the river, up a cliff):
        // the agent would stand at the nearest point re-trying forever and shake. Forget it for a while.
        if (!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            unreachable[patch] = Time.time + 30f;
            if (isWater) water_patch = null; else food_patch = null;
            agent.ResetPath();
            return false;
        }
        if (to.magnitude <=
            catchDistance + 0.5f || crowdedStop)
        {
            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Fill up by what the patch actually gave: a nearly empty patch barely helps.
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): Guided "give the grass a break" takes less grass per bite, same meal
            float requested = isWater ? biteSize : biteSize * GuidedEffects.Graze;
            float taken = patch.Consume(requested);
            float share = requested > 0f ? taken / requested : 1f;

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
            return SearchingForPrey && GoToLastSeen(speed);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

        // Another predator got it first.
        if (prey_brain != null &&
            prey_brain.IsCaught)
        {
            return false;
        }

        if (InReachSwept(prey, prey_brain, catchDistance))   // [AI-GENERATED] Claude Code (Claude Opus 5.5): swept, so x8 can't skip past
        {
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): research: 70% of catches succeed (Guided can add or take
            // 15 points). A miss lets the sheep escape, and the leopard rests a simulated day before trying again.
            if (Random.value > Mathf.Clamp01(huntSuccessRate + GuidedEffects.HuntSuccessBonus))
            {
                sprintTimer = 0f; SetCrouch(false);
                huntRestTimer = huntCooldownSeconds * DayScale;
                agent.ResetPath();
                return false;
            }
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

            // Predator has successfully eaten (research: +80 Fullness, capped).
            curr_hunger = Mathf.Min(100f, curr_hunger + huntFill);
            sprintTimer = 0f; SetCrouch(false);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

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

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
        // Stalk until close (or the prey bolts), then sprint. The sprint only lasts maxSprintSeconds:
        // a sheep that got away in time is safe, and the leopard rests before trying again.
        Vector3 gap = prey.position - transform.position; gap.y = 0f;
        bool preyRunning = prey_brain != null && prey_brain.IsFleeing;
        bool sprinting = sprintTimer > 0f || preyRunning || gap.magnitude <= pounceDistance;

        if (sprinting)
        {
            SetCrouch(false);
            sprintTimer += Time.deltaTime * speed;
            if (sprintTimer > maxSprintSeconds)
            {
                // Out of breath: give up and rest before trying again.
                sprintTimer = 0f;
                huntRestTimer = giveUpRestSeconds;
                agent.ResetPath();
                DramaDirector.Report(DramaDirector.Moment.Catch, transform,
                    $"The {SpeciesLabel(prey_brain != null ? prey_brain.species_data : null)} got away! The {SpeciesLabel(species_data)} is out of breath.");
                return false;
            }
        }
        else
        {
            SetCrouch(true);   // creep in low and slow
        }

        agent.speed =
            base_speed *
            (sprinting ? sprintSpeedMultiplier : stalkSpeedMultiplier) *
            speed;

        SetDestinationIfMoved(prey.position, 1f);

        // In the open, a sprinting leopard runs straight at the prey at full speed every frame instead of
        // following a path that is a frame or more old (at x8 that lag lost almost every chase).
        if (sprinting &&
            !NavMesh.Raycast(transform.position, prey.position, out _, NavMesh.AllAreas))
        {
            Vector3 dir = prey.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                agent.velocity = dir.normalized * agent.speed;
        }
        // [AI-GENERATED END]

        return true;
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // The prey is out of sight: head for where it was last seen. A sprint already under way finishes its dash
    // (still limited by maxSprintSeconds); otherwise it creeps there low and slow, out of the prey's view too.
    // The next decision picks the hunt straight back up if the prey comes into view. Returns false when it
    // gives up (arrived and found nothing, or out of breath), and it then rests before hunting again.
    bool GoToLastSeen(float speed)
    {
        Vector3 gap = last_prey_position - transform.position;
        gap.y = 0f;

        if (gap.magnitude <= catchDistance + BodyRadius)
        {
            GiveUpHunt();   // it's not here any more
            return false;
        }

        bool sprinting = sprintTimer > 0f;
        if (sprinting)
        {
            sprintTimer += Time.deltaTime * speed;
            if (sprintTimer > maxSprintSeconds)
            {
                DramaDirector.Report(DramaDirector.Moment.Catch, transform,
                    $"The {SpeciesLabel(remembered_prey != null ? remembered_prey.species_data : null)} got away! The {SpeciesLabel(species_data)} is out of breath.");
                GiveUpHunt();
                return false;
            }
        }
        else
        {
            SetCrouch(true);
        }

        agent.speed = base_speed * (sprinting ? sprintSpeedMultiplier : stalkSpeedMultiplier) * speed;
        SetDestinationIfMoved(last_prey_position, 1f);
        return true;
    }

    void GiveUpHunt()
    {
        sprintTimer = 0f;
        SetCrouch(false);
        huntRestTimer = giveUpRestSeconds;
        ForgetPrey();
        agent.ResetPath();
    }
    // [AI-GENERATED END]

    // Stalking flag: the procedural animator shows the low, creeping Stalk pose (AnimalMotionBridge).
    void SetCrouch(bool on) => IsStalking = on;
    // [AI-GENERATED END]

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
            ReproduceCooldown;   // both partners wait, so it counts per pair (research)

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
        // "BlueSheep" -> "blue sheep": a space before each capital, then lower case.
        string name = s != null && !string.IsNullOrEmpty(s.speciesName)
            ? System.Text.RegularExpressions.Regex.Replace(s.speciesName, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant()
            : "animal";
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
    // [AI-GENERATED] Claude Code (Claude Opus 5.5): the spawner's group radius keeps leopards spawning close
    // together; they patrol a wider territory than that.
    float RoamRadius => IsPredator ? homeRadius * predatorTerritoryScale : homeRadius;
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
            outside += Mathf.Max(0f, d.magnitude - RoamRadius) * 0.01f;   // 10 m outside ≈ 0.1 of the height range
        }
        return outside;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // Predator patrol: instead of short random steps (which kept a leopard circling one spot), walk to a
    // distant point in the territory, pause to look around, then pick another one away from the places
    // visited recently, so over time it covers the whole range.
    [Header("Predator patrol")]
    [Tooltip("Predators roam this many times the spawner's group radius (they spawn close, but patrol wide).")]
    [Min(1f)] public float predatorTerritoryScale = 3f;
    [Tooltip("Shortest and longest leg of a patrol, in metres.")]
    public float patrolMinDistance = 25f;
    public float patrolMaxDistance = 60f;
    [Tooltip("Seconds (at 1x) a predator pauses at each patrol point.")]
    public Vector2 patrolPauseSeconds = new Vector2(1.5f, 4f);
    [Tooltip("Recent patrol points it avoids going back to.")]
    [Min(0)] public int patrolMemory = 6;

    readonly Queue<Vector3> patrol_visited = new();
    bool has_patrol;
    float patrol_timeout;

    void RememberPatrolPoint(Vector3 p)
    {
        patrol_visited.Enqueue(p);
        while (patrol_visited.Count > patrolMemory) patrol_visited.Dequeue();
    }

    // 0 for somewhere new, higher near recently visited points.
    float PatrolStaleness(Vector3 p)
    {
        float stale = 0f;
        float r = Mathf.Max(1f, patrolMinDistance);
        foreach (var v in patrol_visited)
        {
            Vector3 d = v - p; d.y = 0f;
            stale += Mathf.Max(0f, 1f - d.magnitude / r);
        }
        return stale;
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): snow is where a snow leopard is camouflaged, so it roams and
    // rests there: 0 on full snow, snowPreference on bare grass. A hungry one follows prey anywhere, like
    // OutsideHabitat. Off a painted terrain (no snow layer) every spot costs the same.
    float NotSnowCost(Vector3 p)
    {
        if (snowPreference <= 0f || HungerNeed >= predatorRoamHunger) return 0f;
        if (!TerrainCover.TryGet(p, out var c)) return 0f;
        float sum = c.grass + c.rock + c.snow;
        if (sum <= 0.001f) return 0f;
        // Rock hides it fairly well too, so it only counts half as "not snow".
        return snowPreference * (c.grass + 0.5f * c.rock) / sum;
    }

    void Patrol(float speed)
    {
        float dt = Time.deltaTime * speed;

        if (has_patrol)
        {
            patrol_timeout -= dt;
            bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= 2f);
            if (!arrived && patrol_timeout > 0f) return;

            // Arrived (or stuck): stop and look around before the next leg.
            has_patrol = false;
            RememberPatrolPoint(transform.position);
            wander_timer = Random.Range(patrolPauseSeconds.x, patrolPauseSeconds.y);
        }

        wander_timer -= dt;
        if (wander_timer > 0f) return;

        Vector3 best = Vector3.zero;
        float bestScore = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(patrolMinDistance, patrolMaxDistance);
            Vector3 candidate = transform.position + new Vector3(dir.x, 0f, dir.y) * dist;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas)) continue;
            // Staying in the territory matters most (10 m outside ≈ 0.5), then snowy ground, then going somewhere new.
            float score = OutsideHabitat(hit.position) * 5f + NotSnowCost(hit.position) + PatrolStaleness(hit.position);
            if (score < bestScore) { bestScore = score; best = hit.position; found = true; }
        }

        if (!found)
        {
            wander_timer = 1f;   // nowhere reachable this time: try again shortly
            return;
        }

        RememberPatrolPoint(transform.position);
        agent.SetDestination(best);
        has_patrol = true;
        float legLength = Vector3.Distance(transform.position, best);
        patrol_timeout = legLength / Mathf.Max(0.5f, base_speed) * 2f + 5f;   // give up on a leg that takes far too long
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

        if (IsPredator)
        {
            Patrol(speed);   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
            return;
        }

        // Simulation speed affects wandering timer.
        wander_timer -=
            Time.deltaTime * speed;

        if (wander_timer > 0f)
            return;

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Try a few random spots and keep the one most inside the home height band.
        // Outside home, look further so the animal makes real progress back.
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): standing where a predator was seen counts as "away" too,
        // so the animal walks well clear of it, and every spot's score includes the remembered danger.
        bool inDanger = DangerAt(transform.position) > 0.2f;
        bool away = inDanger || OutsideHabitat(transform.position) > 0f;
        float reach = away ? 30f : 12f;
        Vector3 best = Vector3.zero;
        float bestScore = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < (away ? 8 : 4); i++)
        {
            Vector3 candidate = transform.position + Random.insideUnitSphere * reach;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas)) continue;
            float score = OutsideHabitat(hit.position) + DangerAt(hit.position);   // danger 1 ≈ 100 m outside home
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

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): remembered predator sightings, fading as they are forgotten
        foreach (var spot in danger_memory.Spots)
        {
            float fresh = dangerMemorySeconds > 0f ? Mathf.Clamp01(spot.remaining / dangerMemorySeconds) : 1f;
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.25f + 0.6f * fresh);
            Gizmos.DrawWireSphere(spot.position, dangerRadius);
            Gizmos.DrawLine(transform.position, spot.position);
        }
    }
#endif
}
