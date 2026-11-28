// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - SCRUM-380 / SCRUM-381
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Which family the player follows in Guided Exploration.</summary>
public enum FamilyStory { BlueSheep, SnowLeopard }

/// <summary>
/// Guided Exploration's named family (research doc: Family Interaction, Challenge Rules &amp; Starting Conditions).
///
/// The family are normal animals of their species, spawned as usual and then tagged with <see cref="FamilyMember"/>:
///   Milo's story:  Milo (lamb, main animal), Mum, 1 sibling. Lambs born to Mum join the family.
///   Snowy's story: Snowy (cub, main animal), Mother, 1 sibling. No new cubs during the story.
///
/// For the rest of the team (camera, chapters, endings): <see cref="MainAnimal"/>, <see cref="Members"/>,
/// <see cref="Get"/>, <see cref="State"/>, <see cref="Lost"/>, and the events <see cref="FamilyReady"/>,
/// <see cref="MainAnimalDied"/> (ends the story early), <see cref="MemberDied"/>, <see cref="MemberJoined"/>.
///
/// Also here: the focal animal protection rule (<see cref="CanBeHunted"/>), maternal feeding for the family
/// only, and what the young do when their mother dies.
/// </summary>
public class FamilyManager : MonoBehaviour
{
    // ---------- Public API (static, so callers don't need a reference) ----------

    /// <summary>Set by the family picker before Guided starts (also decides the starting populations).</summary>
    public static FamilyStory SelectedStory { get; set; } = FamilyStory.BlueSheep;

    public static FamilyManager Instance { get; private set; }

    /// <summary>The family has been picked from the spawned animals.</summary>
    public static bool Ready => Instance != null && Instance.formed;

    /// <summary>Milo or Snowy while alive; null before the family is ready and after the main animal dies.</summary>
    public static FamilyMember MainAnimal => Ready && Instance.main != null && Instance.main.IsAlive ? Instance.main : null;

    /// <summary>The main animal died this run (the story ends early).</summary>
    public static bool MainAnimalDead => Instance != null && Instance.mainDied;

    /// <summary>Living family members, the main animal included.</summary>
    public static IReadOnlyList<FamilyMember> Members => Instance != null ? Instance.members : (IReadOnlyList<FamilyMember>)Array.Empty<FamilyMember>();

    /// <summary>Family members who died this run, oldest first.</summary>
    public static IReadOnlyList<FamilyLoss> Lost => Instance != null ? Instance.lost : (IReadOnlyList<FamilyLoss>)Array.Empty<FamilyLoss>();

    /// <summary>A family member who died: who, how, and on which simulated day.</summary>
    public readonly struct FamilyLoss
    {
        public readonly string Name, Id;
        public readonly FamilyRole Role;
        public readonly FamilyDeathCause Cause;
        public readonly int Day;
        public FamilyLoss(FamilyMember m, FamilyDeathCause cause, int day)
        { Name = m.DisplayName; Id = m.Id; Role = m.Role; Cause = cause; Day = day; }
    }

    /// <summary>For story text (doc: Family-State Narrative Variations).</summary>
    public enum FamilyState { AllAlive, SomeLost, OnlyMainLeft, MainDead }

    public static FamilyState State
    {
        get
        {
            if (MainAnimal == null) return FamilyState.MainDead;
            if (Members.Count <= 1) return FamilyState.OnlyMainLeft;
            return Lost.Count == 0 ? FamilyState.AllAlive : FamilyState.SomeLost;
        }
    }

    /// <summary>First living member with this role (e.g. Mother), or null.</summary>
    public static FamilyMember Get(FamilyRole role)
    {
        foreach (var m in Members) if (m != null && m.Role == role && m.IsAlive) return m;
        return null;
    }

    /// <summary>True if this animal is a living family member.</summary>
    public static bool IsFamily(AnimalBrain animal) =>
        animal != null && animal.TryGetComponent(out FamilyMember m) && m.IsAlive;

    public static event Action FamilyReady;
    /// <summary>The main animal died: the story ends early.</summary>
    public static event Action<FamilyMember, FamilyDeathCause> MainAnimalDied;
    /// <summary>Any family member died (the main animal too, before <see cref="MainAnimalDied"/>).</summary>
    public static event Action<FamilyMember, FamilyDeathCause> MemberDied;
    /// <summary>A new member joined (a lamb born to Mum).</summary>
    public static event Action<FamilyMember> MemberJoined;

    /// <summary>Research doc, Starting Population Configuration.</summary>
    public static (int sheep, int leopards) StartingPopulation(FamilyStory story) =>
        story == FamilyStory.SnowLeopard ? (25, 3) : (20, 2);   // 28 Nov (Shumin): 2 leopards in Miloy's story so they can breed

    /// <summary>
    /// SCRUM-381, focal animal protection rule (research doc): predators can't target Milo while at least one
    /// other Blue Sheep is alive. Once he is the last one, normal predation applies. Hunger, thirst and illness
    /// still apply, and every other animal (his family too) can be hunted. Snowy has no protection.
    /// </summary>
    public static bool CanBeHunted(AnimalBrain prey)
    {
        if (!Ready || prey == null || SelectedStory != FamilyStory.BlueSheep) return true;
        var m = Instance.main;
        if (m == null || m.Brain != prey || !m.IsAlive) return true;
        foreach (var other in AnimalBrain.All)
            if (other != null && other != prey && !other.IsCaught && other.species_data == prey.species_data)
                return false;   // another Blue Sheep is alive: Milo is protected
        return true;
    }

    // ---------- Settings (defaults from the research; editable on the [FamilyManager] object in Play mode) ----------

    [Header("Names")]
    public string miloName = "Miloy";
    public string snowyName = "Snowy";
    public string motherName = "Mum";
    public string brotherName = "Brother";
    public string sisterName = "Sister";
    [Tooltip("Names for lambs born to Mum during Milo's story, in order.")]
    public string[] offspringNames = { "Pip", "Tashi", "Pema", "Nima", "Dawa" };

    [Header("Ages (ecological months; 1 simulated day = 4 ecological days)")]
    [Tooltip("Blue Sheep lambs stay with their mother until about a year old.")]
    public float lambDependentMonths = 15f;
    [Tooltip("Milo is a lamb born just before the story starts in Summer.")]
    public float lambStartMonths = 1f;
    [Tooltip("Snow Leopard cubs stay with their mother until about 18-22 months.")]
    public float cubDependentMonths = 20f;
    [Tooltip("Snowy and her sibling are 6 months old at the start (research doc).")]
    public float cubStartMonths = 6f;

    [Header("Maternal feeding (family only)")]
    [Tooltip("Snowy's story: Fullness each cub gets when Mother makes a kill.")]
    public float cubShareOfKill = 60f;
    [Tooltip("Cubs further than this from Mother's kill miss out.")]
    public float shareKillDistance = 40f;
    [Tooltip("Milo's story: Fullness and Hydration a lamb gets per simulated day from nursing while near Mum.")]
    public float nursingPerDay = 30f;
    [Tooltip("A lamb nurses only within this distance of Mum.")]
    public float nursingDistance = 6f;

    [Header("If Mother dies")]
    [Tooltip("Off (research doc): orphaned cubs don't hunt and rely on events for food. On: they hunt like adults.")]
    public bool orphanCubsHunt = false;

    // ---------- State ----------

    const float EcoDaysPerSimDay = 4f;
    const float DaysPerMonth = 30.4f;
    static float MonthsToSimDays(float months) => months * DaysPerMonth / EcoDaysPerSimDay;

    readonly List<FamilyMember> members = new List<FamilyMember>();
    readonly List<FamilyLoss> lost = new List<FamilyLoss>();
    FamilyMember main, mother;
    bool formed, mainDied, motherDied;
    int offspringCount;
    float guardianCheckTimer;

    ClockService clock;
    SimulationSpeedController speed;

    // ---------- Startup ----------

    // Statics survive Play without a domain reload: drop old subscribers and the old run.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        FamilyReady = null;
        MainAnimalDied = null;
        MemberDied = null;
        MemberJoined = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != ScreenFlow.Simulation || !ScreenFlow.GuidedMode || Instance != null) return;
        new GameObject("[FamilyManager]").AddComponent<FamilyManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        AnimalBrain.Caught += OnCaught;
        AnimalBrain.Starved += OnStarved;
        AnimalBrain.Born += OnBorn;
    }

    void OnDisable()
    {
        AnimalBrain.Caught -= OnCaught;
        AnimalBrain.Starved -= OnStarved;
        AnimalBrain.Born -= OnBorn;
    }

    void Start()
    {
        clock = FindFirstObjectByType<ClockService>();
        speed = FindFirstObjectByType<SimulationSpeedController>();
        gameObject.AddComponent<FamilyMarkers>();
    }

    void Update()
    {
        if (!formed) { TryForm(); return; }

        // Simulation time, like the animals: nothing happens while paused, x8 is 8 times as much.
        if (speed != null && !speed.IsRunning) return;
        float dt = Time.deltaTime * (speed != null ? speed.CurrentSpeed : 1f);

        if (SelectedStory == FamilyStory.BlueSheep) Nurse(dt);
        else UpdateMotherHunger();

        guardianCheckTimer -= Time.deltaTime;
        if (motherDied && guardianCheckTimer <= 0f)
        {
            guardianCheckTimer = 2f;
            ReassignOrphans();
        }
    }

    // ---------- Forming the family from the spawned animals ----------

    bool WantPredator => SelectedStory == FamilyStory.SnowLeopard;

    // Ready once the spawner has placed the animals and their brains have started (species and sex known).
    void TryForm()
    {
        var pool = new List<AnimalBrain>();
        foreach (var a in AnimalBrain.All)
            if (a != null && !a.IsCaught && a.species_data != null && a.IsPredatorSpecies == WantPredator && OnNavMesh(a))
                pool.Add(a);
        if (pool.Count < 3) return;   // mother + 2 young

        members.Clear();
        lost.Clear();
        mainDied = motherDied = false;
        offspringCount = 0;

        if (SelectedStory == FamilyStory.BlueSheep) FormMiloFamily(pool);
        else FormSnowyFamily(pool);

        formed = true;
        Debug.Log($"[Family] {SelectedStory} family ready: " + string.Join(", ", members.ConvertAll(m => $"{m.DisplayName} ({m.Role})")));
        FamilyReady?.Invoke();
    }

    void FormMiloFamily(List<AnimalBrain> sheep)
    {
        // Mum: an adult female; Milo and his sibling: the two sheep nearest her, moved to her side.
        var mum = sheep.Find(s => s.AnimalSex == AnimalBrain.Sex.Female) ?? sheep[0];
        sheep.Remove(mum);
        sheep.Sort((a, b) => Dist(a, mum).CompareTo(Dist(b, mum)));
        var milo = sheep[0];
        var sibling = sheep[1];

        mum.SetSex(AnimalBrain.Sex.Female);
        milo.SetSex(AnimalBrain.Sex.Male);   // "his family" in the research

        float youngDays = MonthsToSimDays(lambDependentMonths);
        float grown = lambStartMonths / Mathf.Max(0.01f, lambDependentMonths);
        MakeYoungBeside(milo, mum, youngDays, grown, 1f);
        MakeYoungBeside(sibling, mum, youngDays, grown, -1f);

        mother = Tag(mum, motherName, FamilyRole.Mother, "milo-mum");
        main = Tag(milo, miloName, FamilyRole.Main, "milo");
        Tag(sibling, SiblingName(sibling), FamilyRole.Sibling, "milo-sibling");
    }

    void FormSnowyFamily(List<AnimalBrain> leopards)
    {
        var mum = leopards[0];
        leopards.Remove(mum);
        leopards.Sort((a, b) => Dist(a, mum).CompareTo(Dist(b, mum)));
        var snowy = leopards[0];
        var sibling = leopards[1];

        mum.SetSex(AnimalBrain.Sex.Female);
        snowy.SetSex(AnimalBrain.Sex.Female);   // "her mother and her sibling"
        mum.canBreed = false;                   // the family has no new cubs during the story

        float youngDays = MonthsToSimDays(cubDependentMonths);
        float grown = cubStartMonths / Mathf.Max(0.01f, cubDependentMonths);
        MakeYoungBeside(snowy, mum, youngDays, grown, 1f);
        MakeYoungBeside(sibling, mum, youngDays, grown, -1f);
        snowy.canHunt = sibling.canHunt = false;   // only Mother hunts while the cubs depend on her

        mother = Tag(mum, motherName, FamilyRole.Mother, "snowy-mother");
        main = Tag(snowy, snowyName, FamilyRole.Main, "snowy");
        Tag(sibling, SiblingName(sibling), FamilyRole.Sibling, "snowy-sibling");
    }

    // Moves a young one just behind its mother (one on each side) and makes it hers.
    static void MakeYoungBeside(AnimalBrain young, AnimalBrain mum, float youngDays, float grownFraction, float side)
    {
        Vector3 spot = mum.transform.position - mum.transform.forward * 1.2f + mum.transform.right * side;
        if (young.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh &&
            NavMesh.SamplePosition(spot, out var hit, 4f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        young.CopyHome(mum);
        young.MakeYoung(mum, youngDays, grownFraction);
    }

    FamilyMember Tag(AnimalBrain animal, string displayName, FamilyRole role, string id)
    {
        var m = animal.gameObject.GetComponent<FamilyMember>();
        if (m == null) m = animal.gameObject.AddComponent<FamilyMember>();
        m.Init(displayName, role, id);
        members.Add(m);
        return m;
    }

    string SiblingName(AnimalBrain a) => a.AnimalSex == AnimalBrain.Sex.Male ? brotherName : sisterName;

    static bool OnNavMesh(AnimalBrain a) => a.TryGetComponent(out NavMeshAgent agent) && agent.enabled && agent.isOnNavMesh;

    static float Dist(AnimalBrain a, AnimalBrain b) => (a.transform.position - b.transform.position).sqrMagnitude;

    // ---------- Maternal feeding (family only) ----------

    // Milo's story: lambs drink Mum's milk while close to her, on top of grazing.
    void Nurse(float dt)
    {
        if (mother == null || !mother.IsAlive) return;
        float amount = nursingPerDay * dt / Mathf.Max(1f, ClockService.DaySeconds);
        float maxSq = nursingDistance * nursingDistance;
        foreach (var m in members)
        {
            if (m == mother || !m.IsAlive || !m.Brain.IsYoung || m.Brain.Mother != mother.Brain) continue;
            if ((m.transform.position - mother.transform.position).sqrMagnitude <= maxSq)
                m.Brain.Refresh(amount, amount);
        }
    }

    // Snowy's story: Mother hunts when she or either cub is hungry.
    void UpdateMotherHunger()
    {
        if (mother == null || !mother.IsAlive) return;
        float hungriest = 100f;
        foreach (var m in members)
            if (m != mother && m.IsAlive && !m.Brain.canHunt) hungriest = Mathf.Min(hungriest, m.Brain.curr_hunger);
        mother.Brain.dependentsHunger = hungriest;
    }

    // Snowy's story: Mother shares her kill with the cubs nearby.
    void ShareKill()
    {
        float maxSq = shareKillDistance * shareKillDistance;
        foreach (var m in members)
        {
            if (m == mother || !m.IsAlive || m.Brain.canHunt) continue;
            if ((m.transform.position - mother.transform.position).sqrMagnitude <= maxSq)
                m.Brain.Refresh(cubShareOfKill, 0f);
        }
    }

    // ---------- When Mother dies ----------

    // Lambs stay together and follow the nearest adult of the herd, as orphaned lambs do.
    // Cubs stay together: the sibling follows Snowy, who roams without hunting unless orphanCubsHunt is on.
    void ReassignOrphans()
    {
        var young = members.FindAll(m => m.IsAlive && m.Brain.IsYoung);
        if (young.Count == 0) return;

        if (SelectedStory == FamilyStory.BlueSheep)
        {
            var leader = main != null && main.IsAlive ? main.Brain : young[0].Brain;
            AnimalBrain guardian = null;
            float best = float.PositiveInfinity;
            foreach (var a in AnimalBrain.All)
            {
                if (a == null || a.IsCaught || a.IsYoung || a.species_data != leader.species_data) continue;
                float d = Dist(a, leader);
                if (d < best) { best = d; guardian = a; }
            }
            foreach (var m in young)
                if (guardian != null && (m.Brain.Mother == null || m.Brain.Mother.IsCaught))   // Mum, or the last guardian, is gone
                    m.Brain.FollowInstead(guardian);
        }
        else
        {
            var snowy = main != null && main.IsAlive ? main.Brain : null;
            foreach (var m in young)
            {
                if (orphanCubsHunt) m.Brain.canHunt = true;
                if (m.Brain == snowy) continue;
                if (snowy != null) m.Brain.FollowInstead(snowy);
            }
        }
    }

    // ---------- Births and deaths ----------

    void OnBorn(AnimalBrain baby)
    {
        if (!formed || baby == null || mother == null || baby.Mother != mother.Brain || !mother.IsAlive) return;
        // A lamb born to Mum joins the family and stays a lamb as long as Milo did.
        baby.MakeYoung(mother.Brain, MonthsToSimDays(lambDependentMonths), 0f);
        string name = offspringCount < offspringNames.Length ? offspringNames[offspringCount] : $"Lamb {offspringCount + 1}";
        offspringCount++;
        var m = Tag(baby, name, FamilyRole.Offspring, $"milo-offspring-{offspringCount}");
        Debug.Log($"[Family] {name} was born to {mother.DisplayName}.");
        MemberJoined?.Invoke(m);
    }

    void OnCaught(AnimalBrain predator, AnimalBrain prey)
    {
        if (!formed) return;
        if (mother != null && mother.IsAlive && predator == mother.Brain && SelectedStory == FamilyStory.SnowLeopard)
            ShareKill();
        if (prey != null && prey.TryGetComponent(out FamilyMember m)) Died(m, FamilyDeathCause.Caught);
    }

    void OnStarved(AnimalBrain animal)
    {
        if (!formed || animal == null || !animal.TryGetComponent(out FamilyMember m)) return;
        // Raised before the needs are reset, so they still show what ran out.
        var cause = animal.HasThirst && animal.curr_thirst <= 0f ? FamilyDeathCause.Thirst
                  : animal.curr_hunger <= 0f ? FamilyDeathCause.Hunger
                  : FamilyDeathCause.Other;   // health lost another way, e.g. the illness event
        Died(m, cause);
    }

    void Died(FamilyMember m, FamilyDeathCause cause)
    {
        if (!members.Contains(m)) return;
        m.MarkDead();
        members.Remove(m);
        lost.Add(new FamilyLoss(m, cause, clock != null ? clock.Day : 0));
        Debug.Log($"[Family] {m.DisplayName} ({m.Role}) died: {cause}.");

        if (m == mother)
        {
            motherDied = true;
            guardianCheckTimer = 0f;
        }
        MemberDied?.Invoke(m, cause);
        if (m == main)
        {
            mainDied = true;
            MainAnimalDied?.Invoke(m, cause);
        }
    }

    // A member's model was removed without dying. The main animal gone that way means the run was reset:
    // untag the rest and form the family again from the new animals.
    internal static void OnMemberRemoved(FamilyMember m)
    {
        var fm = Instance;
        if (fm == null || !fm.formed || !fm.members.Contains(m)) return;
        fm.members.Remove(m);
        if (m != fm.main) return;

        var rest = new List<FamilyMember>(fm.members);
        fm.formed = false;
        fm.main = fm.mother = null;
        fm.members.Clear();
        fm.lost.Clear();
        foreach (var other in rest) if (other != null) Destroy(other);
    }

    // ---------- Testing ----------

    // Right-click the [FamilyManager] component in Play mode to test the early ending.
    // Health alone isn't enough: a well-fed animal heals before the death check, so hunger is emptied too.
    [ContextMenu("Debug: kill main animal (starves on its next update)")]
    void DebugKillMain() => DebugKill(MainAnimal);

    [ContextMenu("Debug: kill mother")]
    void DebugKillMother() => DebugKill(Get(FamilyRole.Mother));

    static void DebugKill(FamilyMember m)
    {
        if (m == null) return;
        m.Brain.curr_hunger = 0f;
        m.Brain.curr_health = 0f;
    }
}
