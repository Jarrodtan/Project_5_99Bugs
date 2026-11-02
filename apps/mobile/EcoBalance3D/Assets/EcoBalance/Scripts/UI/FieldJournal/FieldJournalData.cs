using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// The investigation cases shown in the Field Journal and which of them are unlocked.
/// Uses the running simulation's <see cref="CaseDetector"/> when there is one (so the journal and the
/// popup share one <see cref="CaseJournal"/>); otherwise, e.g. when the journal is opened from a menu,
/// loads the cases from Resources/Cases and reads the saved journal directly.
/// </summary>
public sealed class FieldJournalCases
{
    public const string ResourcesFolder = "Cases";

    public readonly List<InvestigationCase> Cases;
    public readonly CaseJournal Journal;
    /// <summary>The live detector, or null when no Free Exploration run is open.</summary>
    public readonly CaseDetector Detector;

    public FieldJournalCases(IEnumerable<InvestigationCase> cases, CaseJournal journal, CaseDetector detector = null)
    {
        Cases = cases != null ? cases.Where(c => c != null).ToList() : new List<InvestigationCase>();
        Journal = journal ?? throw new System.ArgumentNullException(nameof(journal));
        Detector = detector;
    }

    public static FieldJournalCases Find()
    {
        var detector = Object.FindAnyObjectByType<CaseDetector>();
        if (detector != null && detector.cases != null && detector.cases.Count > 0)
            return new FieldJournalCases(detector.cases, detector.Journal, detector);
        return new FieldJournalCases(LoadAll(), new CaseJournal(new PlayerPrefsCaseJournalStore()), detector);
    }

    /// <summary>Every case asset in Resources/Cases, in file-name order (Case_01, Case_02...).</summary>
    public static List<InvestigationCase> LoadAll() =>
        Resources.LoadAll<InvestigationCase>(ResourcesFolder).Where(c => c != null).OrderBy(c => c.name).ToList();

    public bool IsUnlocked(InvestigationCase c) => c != null && Journal.IsUnlocked(c.Key);
    public int UnlockedCount => Cases.Count(IsUnlocked);

    /// <summary>
    /// Which case the Investigations page opens on: the requested one if it exists, else the first
    /// unlocked case not yet read, else the first unlocked one, else the first case. -1 if there are none.
    /// </summary>
    public int PickInitial(CaseSeenTracker seen, string requestedKey = null)
    {
        if (Cases.Count == 0) return -1;
        if (!string.IsNullOrEmpty(requestedKey))
        {
            int r = Cases.FindIndex(c => c.Key == requestedKey);
            if (r >= 0) return r;
        }
        if (seen != null)
        {
            int n = Cases.FindIndex(c => seen.IsNew(Journal, c.Key));
            if (n >= 0) return n;
        }
        int u = Cases.FindIndex(IsUnlocked);
        return u >= 0 ? u : 0;
    }
}

/// <summary>
/// Which unlocked cases the player has already read in the Field Journal, for the "NEW" tag.
/// Saved separately from the journal so the detector's save format stays unchanged.
/// </summary>
public sealed class CaseSeenTracker
{
    public const string PrefsKey = "EcoBalance.CaseJournal.Seen";

    readonly ICaseJournalStore store;
    readonly HashSet<string> seen = new();

    public CaseSeenTracker(ICaseJournalStore store)
    {
        this.store = store ?? throw new System.ArgumentNullException(nameof(store));
        foreach (var id in (store.Load() ?? "").Split('\n'))
            if (!string.IsNullOrWhiteSpace(id)) seen.Add(id.Trim());
    }

    public static CaseSeenTracker ForDevice() => new(new PlayerPrefsCaseJournalStore(PrefsKey));

    /// <summary>Unlocked but not opened in the journal yet.</summary>
    public bool IsNew(CaseJournal journal, string caseKey) =>
        journal != null && journal.IsUnlocked(caseKey) && !seen.Contains(caseKey);

    public void MarkSeen(string caseKey)
    {
        if (string.IsNullOrWhiteSpace(caseKey) || !seen.Add(caseKey)) return;
        Save();
    }

    /// <summary>Forgets cases that are no longer unlocked (after a journal reset), so they show NEW again.</summary>
    public void Prune(CaseJournal journal)
    {
        if (journal == null) return;
        if (seen.RemoveWhere(k => !journal.IsUnlocked(k)) > 0) Save();
    }

    void Save() => store.Save(string.Join("\n", seen));
}

/// <summary>One line on the population graph.</summary>
public sealed class PopulationSeries
{
    public string Key;      // history key: the species / resource asset name
    public string Label;    // shown to the player
    public Color Color;
    public bool IsResource;
    public bool Visible;
}

/// <summary>Pure helpers for the population graph (unit-tested).</summary>
public static class PopulationGraphMath
{
    // Colour-blind-friendly on the light journal pages. Species and resources use separate sets so
    // animals always read as "blue / orange" and resources as "green / teal".
    static readonly Color[] SpeciesColors =
    {
        new Color32(0x2F, 0x6D, 0xB5, 0xFF), new Color32(0xD9, 0x82, 0x2B, 0xFF),
        new Color32(0x8E, 0x5B, 0xB5, 0xFF), new Color32(0xC8, 0x46, 0x3D, 0xFF),
    };
    static readonly Color[] ResourceColors =
    {
        new Color32(0x3E, 0x9B, 0x4F, 0xFF), new Color32(0x2A, 0xA3, 0xB8, 0xFF), new Color32(0x9A, 0x7B, 0x4F, 0xFF),
    };

    /// <summary>"BlueSheep" -> "Blue Sheep", "Snow_Leopard" -> "Snow Leopard".</summary>
    public static string Pretty(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string s = raw.Replace('_', ' ').Trim();
        return Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");
    }

    /// <summary>
    /// Lines for every key in <paramref name="keys"/>: species first (in the simulator's order, shown),
    /// then resources (hidden by default: their 0-100 scale would flatten the animal lines), then anything else.
    /// </summary>
    public static List<PopulationSeries> BuildSeries(IEnumerable<string> keys,
        IList<SpeciesDefinition> species, IList<ResourceDefinition> resources)
    {
        var remaining = new List<string>(keys ?? Enumerable.Empty<string>());
        var result = new List<PopulationSeries>();
        int s = 0, r = 0;

        if (species != null)
            foreach (var def in species)
            {
                if (def == null || !remaining.Remove(def.name)) continue;
                string label = Pretty(string.IsNullOrWhiteSpace(def.speciesName) ? def.name : def.speciesName);
                result.Add(new PopulationSeries { Key = def.name, Label = label, Color = SpeciesColors[s++ % SpeciesColors.Length], Visible = true });
            }
        if (resources != null)
            foreach (var def in resources)
            {
                if (def == null || !remaining.Remove(def.name)) continue;
                string label = Pretty(string.IsNullOrWhiteSpace(def.resourceName) ? def.name : def.resourceName);
                result.Add(new PopulationSeries { Key = def.name, Label = label, Color = ResourceColors[r++ % ResourceColors.Length], IsResource = true });
            }
        foreach (var key in remaining.OrderBy(k => k))
            result.Add(new PopulationSeries { Key = key, Label = Pretty(key), Color = SpeciesColors[s++ % SpeciesColors.Length], Visible = true });
        return result;
    }

    /// <summary>
    /// Y axis for counts: a whole-number gridline step of 1, 2 or 5 x 10^n and how many steps reach
    /// <paramref name="max"/> (about five gridlines). 10 -> step 2 x 5, 17 -> step 5 x 4, 3 -> step 1 x 3.
    /// </summary>
    public static (float step, int divisions) Scale(float max)
    {
        if (!(max > 0f)) return (1f, 5);
        float raw = max / 5f;
        float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
        float step = magnitude * 10f;
        foreach (float m in new[] { 1f, 2f, 5f, 10f })
            if (m * magnitude >= raw - 1e-4f) { step = m * magnitude; break; }
        step = Mathf.Max(1f, step);
        int divisions = Mathf.Max(1, Mathf.CeilToInt(max / step - 1e-4f));
        return (step, divisions);
    }
}

/// <summary>
/// Remembers the HUD day (<see cref="ClockService.Day"/>) at each recorded tick, so the graph labels time
/// in the same days the player sees (one simulator tick is 2 s, one HUD day is 120 s).
/// Added next to the <see cref="PopulationHistoryRecorder"/> by <see cref="FieldJournalInstaller"/>.
/// </summary>
public class PopulationHistoryDays : MonoBehaviour
{
    public PopulationHistoryRecorder recorder;

    readonly Dictionary<int, int> dayAtTick = new();
    PopulationHistory watched;
    ClockService clock;

    void OnEnable()
    {
        if (recorder == null) recorder = GetComponent<PopulationHistoryRecorder>();
        clock = FindAnyObjectByType<ClockService>();
        if (recorder == null) return;
        watched = recorder.History;
        watched.SampleAdded += OnSample;
        watched.Cleared += OnCleared;
        // Samples recorded before we subscribed (the starting state): close enough to today.
        for (int i = 0; i < watched.Count; i++) Note(watched[i].Tick);
    }

    void OnDisable()
    {
        if (watched == null) return;
        watched.SampleAdded -= OnSample;
        watched.Cleared -= OnCleared;
        watched = null;
    }

    void OnSample(PopulationHistory.Sample s) => Note(s.Tick);
    void OnCleared() => dayAtTick.Clear();

    void Note(int tick)
    {
        if (clock != null && !dayAtTick.ContainsKey(tick)) dayAtTick[tick] = clock.Day;
    }

    public bool TryGetDay(int tick, out int day) => dayAtTick.TryGetValue(tick, out day);

    /// <summary>Tick to HUD day. A plain dictionary, so it still answers after the simulation scene is gone.</summary>
    public IReadOnlyDictionary<int, int> DayAtTick => dayAtTick;
}

/// <summary>
/// What the Observations page shows: the run going on now, or, after leaving it, the last run, so the
/// journal keeps what the player observed. Set by <see cref="FieldJournalInstaller"/> when a simulation loads.
/// Lasts until the app closes (nothing is saved to disk).
/// </summary>
public static class FieldJournalObservations
{
    public static PopulationHistory History { get; private set; }
    public static IReadOnlyDictionary<int, int> Days { get; private set; }
    public static List<PopulationSeries> Series { get; private set; }
    /// <summary>The run was Guided Exploration (no investigations to find in it).</summary>
    public static bool WasGuided { get; private set; }

    static PopulationHistoryRecorder live;

    /// <summary>A simulation is loaded right now (false once its scene has been unloaded).</summary>
    public static bool IsLive => live != null;

    public static bool HasData => History != null && History.Count > 0;

    // Cases unlocked during the run in History, so a case can point at the graph that shows it happening.
    static readonly HashSet<string> unlockedInRun = new();

    /// <summary>The case was discovered during the run the graph shows.</summary>
    public static bool UnlockedInShownRun(string caseKey) => caseKey != null && unlockedInRun.Contains(caseKey);

    public static void NoteUnlocked(InvestigationCase c)
    {
        if (c != null) unlockedInRun.Add(c.Key);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()   // domain reload is off in this project
    {
        if (History != null) History.Cleared -= OnHistoryCleared;
        History = null; Days = null; Series = null; WasGuided = false; live = null;
        unlockedInRun.Clear();
    }

    static void OnHistoryCleared() => unlockedInRun.Clear();   // the run was reset: its graph starts again

    public static void Track(PopulationHistoryRecorder recorder, PopulationHistoryDays days, EcosystemSimulator sim)
    {
        if (recorder == null) return;
        if (History != null) History.Cleared -= OnHistoryCleared;
        unlockedInRun.Clear();
        live = recorder;
        History = recorder.History;
        History.Cleared += OnHistoryCleared;
        Days = days != null ? days.DayAtTick : null;
        WasGuided = ScreenFlow.GuidedMode;
        var keys = new HashSet<string>();
        if (sim != null)
        {
            if (sim.species != null) foreach (var s in sim.species) if (s != null) keys.Add(s.name);
            if (sim.resources != null) foreach (var r in sim.resources) if (r != null) keys.Add(r.name);
        }
        for (int i = 0; i < History.Count; i++) keys.UnionWith(History[i].Keys);
        Series = PopulationGraphMath.BuildSeries(keys, sim != null ? sim.species : null, sim != null ? sim.resources : null);
    }
}
