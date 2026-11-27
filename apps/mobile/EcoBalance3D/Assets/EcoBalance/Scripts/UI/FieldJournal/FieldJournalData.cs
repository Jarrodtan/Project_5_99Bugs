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
    /// <summary>History keys of what this species eats (its food chain links), for the observation notes.</summary>
    public List<string> Eats = new();
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5): Food & Water lines
    /// <summary>No observation notes for this line (e.g. grass eaten per day, which goes up and down all the time).</summary>
    public bool Quiet;
    /// <summary>A level line (famine, thirst): one note the first time it reaches <see cref="AlertAbove"/>, with this text.</summary>
    public string AlertText;
    public float AlertAbove = 80f;
    // [AI-GENERATED END]
}

/// <summary>Something worth noticing in a run, pinned to the graph at <see cref="Index"/>.</summary>
public sealed class ObservationNote
{
    public int Index;      // history sample it happened on
    public int Tick;
    public string Text;    // without the day: the journal adds it
    public Color Color;
}

/// <summary>
/// Reads a run's history the way a field scientist would and writes short notes for the Observations page:
/// each animal's biggest rise and fall, an animal dying out, a resource running out and growing back, and
/// the investigation cases found. When something on the same food chain changed shortly before, the note
/// asks the player to compare the two lines instead of giving the answer away. Pure, so it is unit-tested.
/// </summary>
public static class ObservationNotes
{
    /// <summary>How many notes the journal lists (the most recent ones).</summary>
    public const int MaxShown = 5;
    /// <summary>"Shortly before": about two HUD days (one tick is 2 s, one day 120 s).</summary>
    public const int RelatedWithinTicks = 120;
    public static readonly Color CaseColor = new(0.85f, 0.51f, 0.17f);

    enum Kind { Rise, Fall, Gone, Low, Back, Alert }

    sealed class Change
    {
        public PopulationSeries Series;
        public Kind Kind;
        public int Index, Tick;
        public float Value;
        // An alert (famine, thirst) is bad news, like a fall: it links to food going down just before.
        public bool Down => Kind == Kind.Fall || Kind == Kind.Gone || Kind == Kind.Low || Kind == Kind.Alert;
    }

    /// <summary>Notes in the order they happened. <paramref name="casesFound"/> are the cases unlocked during this run.</summary>
    public static List<ObservationNote> Find(PopulationHistory history, IReadOnlyList<PopulationSeries> series,
        IEnumerable<(int tick, string title)> casesFound = null)
    {
        var notes = new List<(ObservationNote note, int order)>();
        int n = history?.Count ?? 0;
        if (n == 0) return new List<ObservationNote>();

        var changes = new List<Change>();
        if (series != null && n >= 2)
            foreach (var s in series)
            {
                if (s == null || s.Quiet) continue;
                // A value recorded only from some day on (e.g. one added by the Food & Water meter after the
                // first sample) starts there, so the missing days don't read as "0, then it grew back".
                int from = 0;
                while (from < n && !history[from].Has(s.Key)) from++;
                if (n - from < 2) continue;
                var v = new List<float>(n - from);
                for (int i = from; i < n; i++) v.Add(history[i].Get(s.Key));
                int before = changes.Count;
                if (s.AlertText != null) AlertChanges(s, v, history, changes);
                else if (s.IsResource) ResourceChanges(s, v, history, changes);
                else SpeciesChanges(s, v, history, changes);
                for (int i = before; i < changes.Count; i++)
                {
                    changes[i].Index += from;
                    changes[i].Tick = history[changes[i].Index].Tick;
                }
            }

        foreach (var c in changes)
        {
            string text = Describe(c);
            string compare = RelatedLine(c, changes, series);
            if (compare != null) text += $" Look at the {compare} line: what changed just before?";
            notes.Add((new ObservationNote { Index = c.Index, Tick = c.Tick, Text = text, Color = c.Series.Color }, notes.Count));
        }

        if (casesFound != null)
            foreach (var (tick, title) in casesFound)
            {
                if (tick < history[0].Tick) continue;   // older than the history still kept
                int i = 0;
                while (i < n - 1 && history[i].Tick < tick) i++;
                notes.Add((new ObservationNote { Index = i, Tick = history[i].Tick, Text = $"Case found: {title}!", Color = CaseColor }, notes.Count));
            }

        return notes.OrderBy(x => x.note.Index).ThenBy(x => x.order).Select(x => x.note).ToList();
    }

    static void SpeciesChanges(PopulationSeries s, List<float> v, PopulationHistory h, List<Change> into)
    {
        float start = v[0];
        float enough = Mathf.Max(2f, start * 0.25f);   // small wobbles aren't worth a note
        int hi = 0, lo = 0, gone = -1;
        for (int i = 1; i < v.Count; i++)
        {
            if (v[i] > v[hi]) hi = i;
            if (v[i] < v[lo]) lo = i;
            if (gone < 0 && v[i] < 0.5f && v[i - 1] >= 0.5f) gone = i;
        }
        if (hi > 0 && v[hi] - start >= enough) into.Add(New(s, Kind.Rise, hi, v[hi], h));
        if (gone > 0) into.Add(New(s, Kind.Gone, gone, 0f, h));
        else if (lo > 0 && start - v[lo] >= enough) into.Add(New(s, Kind.Fall, lo, v[lo], h));
    }

    static void ResourceChanges(PopulationSeries s, List<float> v, PopulationHistory h, List<Change> into)
    {
        float top = v[0];
        int low = -1;
        for (int i = 1; i < v.Count; i++)
        {
            if (low < 0)
            {
                top = Mathf.Max(top, v[i]);
                if (top > 0f && v[i] <= top * 0.3f) { low = i; into.Add(New(s, Kind.Low, i, v[i], h)); }
            }
            else if (v[i] >= top * 0.6f)
            {
                into.Add(New(s, Kind.Back, i, v[i], h));
                break;
            }
        }
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): famine / thirst level reaching its alert level for the first time.
    static void AlertChanges(PopulationSeries s, List<float> v, PopulationHistory h, List<Change> into)
    {
        for (int i = 1; i < v.Count; i++)
            if (v[i] >= s.AlertAbove && v[i - 1] < s.AlertAbove) { into.Add(New(s, Kind.Alert, i, v[i], h)); return; }
    }

    // Index is relative to the values passed in (Find shifts it to the history index afterwards).
    static Change New(PopulationSeries s, Kind kind, int index, float value, PopulationHistory h) =>
        new() { Series = s, Kind = kind, Index = index, Value = value };

    static string Describe(Change c) => c.Kind switch
    {
        Kind.Alert => c.Series.AlertText,
        Kind.Rise => $"{c.Series.Label} reached {c.Value:0}, the most so far.",
        Kind.Fall => $"{c.Series.Label} fell to {c.Value:0}, the fewest so far.",
        Kind.Gone => $"The last {c.Series.Label} is gone.",
        Kind.Low => $"The {c.Series.Label} is running out.",
        _ => $"The {c.Series.Label} grew back.",
    };

    // A change shortly before on the same food chain: the food going the same way (less food, fewer
    // animals) or the animals that eat it going the other way (more hunters, fewer prey). Null if none.
    static string RelatedLine(Change c, List<Change> all, IReadOnlyList<PopulationSeries> series)
    {
        bool Before(Change r) => r != c && r.Index < c.Index && c.Tick - r.Tick <= RelatedWithinTicks;
        foreach (var r in all)
            if (Before(r) && r.Down == c.Down && c.Series.Eats.Contains(r.Series.Key)) return r.Series.Label;
        foreach (var r in all)
            if (Before(r) && r.Down != c.Down && r.Series.Eats.Contains(c.Series.Key)) return r.Series.Label;
        return null;
    }
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
                var eats = def.GetSpeciesTargets(RelationshipType.Eats).Select(t => t.name)
                    .Concat(def.GetResourceTargets(RelationshipType.Eats).Select(t => t.name)).ToList();
                result.Add(new PopulationSeries { Key = def.name, Label = label, Color = SpeciesColors[s++ % SpeciesColors.Length], Visible = true, Eats = eats });
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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>
    /// Which of <paramref name="count"/> samples to draw when at most <paramref name="max"/> fit: evenly spread,
    /// always the first and the last (today). All of them when they fit.
    /// </summary>
    public static List<int> Thin(int count, int max)
    {
        var result = new List<int>();
        if (count <= 0) return result;
        if (count <= max || max < 2) { for (int i = 0; i < count; i++) result.Add(i); return result; }
        for (int k = 0; k < max; k++)
        {
            int i = Mathf.RoundToInt(k * (count - 1) / (float)(max - 1));
            if (result.Count == 0 || result[result.Count - 1] != i) result.Add(i);
        }
        return result;
    }

    /// <summary>
    /// A smooth curve through <paramref name="points"/> (Catmull-Rom, <paramref name="steps"/> pieces between two
    /// points). It passes through every point and stays between <paramref name="minY"/> and <paramref name="maxY"/>,
    /// so a line never dips below zero between two days.
    /// </summary>
    public static List<Vector2> Smooth(IReadOnlyList<Vector2> points, int steps, float minY, float maxY)
    {
        var result = new List<Vector2>();
        if (points == null || points.Count == 0) return result;
        if (points.Count < 3 || steps < 2) { result.AddRange(points); return result; }
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = points[Mathf.Max(0, i - 1)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(points.Count - 1, i + 2)];
            float lo = Mathf.Max(minY, Mathf.Min(p1.y, p2.y)), hi = Mathf.Min(maxY, Mathf.Max(p1.y, p2.y));
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                var p = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                p.y = Mathf.Clamp(p.y, lo, hi);   // no bumps above or below the two days it joins
                result.Add(p);
            }
        }
        result.Add(points[points.Count - 1]);
        return result;
    }
    // [AI-GENERATED END]
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
    /// <summary>The Food &amp; Water view's lines (grass, water, eaten, drunk, famine, thirst); null when the run has no meter.</summary>
    public static List<PopulationSeries> FoodSeries { get; private set; }

    /// <summary>Every line of both views, for the notes (so "the grass ran out" can point at the sheep and back).</summary>
    public static List<PopulationSeries> AllSeries
    {
        get
        {
            var all = new List<PopulationSeries>();
            if (Series != null) all.AddRange(Series);
            if (FoodSeries != null) all.AddRange(FoodSeries);
            return all;
        }
    }
    /// <summary>The run was Guided Exploration (no investigations to find in it).</summary>
    public static bool WasGuided { get; private set; }

    static PopulationHistoryRecorder live;

    /// <summary>A simulation is loaded right now (false once its scene has been unloaded).</summary>
    public static bool IsLive => live != null;

    public static bool HasData => History != null && History.Count > 0;

    // Cases unlocked during the run in History, so a case can point at the graph that shows it happening.
    static readonly Dictionary<string, (int tick, string title)> unlockedInRun = new();

    /// <summary>The case was discovered during the run the graph shows.</summary>
    public static bool UnlockedInShownRun(string caseKey) => caseKey != null && unlockedInRun.ContainsKey(caseKey);

    /// <summary>The cases discovered during the run the graph shows, with the tick they were found on.</summary>
    public static IEnumerable<(int tick, string title)> CasesFoundInShownRun => unlockedInRun.Values;

    public static void NoteUnlocked(InvestigationCase c)
    {
        // The detector checks right after each new day is recorded, so the latest day is when it happened.
        if (c == null || unlockedInRun.ContainsKey(c.Key)) return;
        int tick = History?.Latest?.Tick ?? 0;
        unlockedInRun[c.Key] = (tick, string.IsNullOrWhiteSpace(c.title) ? c.Key : c.title);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()   // domain reload is off in this project
    {
        if (History != null) History.Cleared -= OnHistoryCleared;
        History = null; Days = null; Series = null; FoodSeries = null; WasGuided = false; live = null;
        unlockedInRun.Clear();
    }

    static void OnHistoryCleared() => unlockedInRun.Clear();   // the run was reset: its graph starts again

    /// <param name="foodAndWater">A <see cref="ResourceUseMeter"/> records this run: show the Food &amp; Water view, which
    /// measures the real grass and water patches, instead of the simulator's resource totals on the Animals view.</param>
    public static void Track(PopulationHistoryRecorder recorder, PopulationHistoryDays days, EcosystemSimulator sim, bool foodAndWater = false)
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
        keys.RemoveWhere(FoodWater.IsKey);   // not animals: they have their own view
        Series = PopulationGraphMath.BuildSeries(keys, sim != null ? sim.species : null, sim != null ? sim.resources : null);
        FoodSeries = null;
        if (foodAndWater)
        {
            Series.RemoveAll(s => s.IsResource);   // the Food & Water view shows the real grass and water instead
            FoodSeries = FoodWater.BuildSeries(Series);
        }
    }
}
