using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives FieldJournalScene. The scene's existing objects are found by NAME, the same way as SimHud,
/// so it can be restyled freely as long as these names stay: JournalPanel (the page; its children are
/// the Ecosystem page), HeaderTxt / SubTxt (fonts copied for the new pages), HeaderTxt_02 (kept on every
/// page), EcosystemBtn / InvestigationBtn / ObservationBtn / DYKBtn (tabs), PreviousBtn / NextBtn (page through tabs).
/// Adds three pages in code:
///   Investigations (wireframes 04/05): every case, locked ones with their clue, unlocked ones with what happened.
///   Observations (wireframe 06): the population graph of the run going on now, or of the last run.
///   Did You Know? (wireframe 02): fun facts about the animals.
/// Opens on Observations inside a run, on the discovered case from the popup, otherwise on Ecosystem.
/// Attached by <see cref="ScreenFlow"/> when the scene loads; BackBtn is wired there.
/// </summary>
public class FieldJournalScreen : MonoBehaviour
{
    public enum Page { Ecosystem, Investigations, Observations, Facts, Runs}
    const int PageCount = 5;

    static Page? requestedPage;
    static string requestedCaseKey;

    // My Runs
    GameObject runsPage;
    List<MyRuns> pastRuns = new();      // newest first
    int selectedRun;
    RectTransform runsContent;
    TextMeshProUGUI runsSub, runTitle, runBody, runLegend;
    RawImage runGraph;
    Texture2D runTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { requestedPage = null; requestedCaseKey = null; }   // domain reload is off

    /// <summary>Open on <paramref name="page"/> (and that case) next time the journal loads. Used by the discovery popup.</summary>
    public static void Request(Page page, string caseKey = null)
    {
        requestedPage = page;
        requestedCaseKey = caseKey;
    }

    public static void Attach(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Canvas>() == null) continue;
            if (root.GetComponent<FieldJournalScreen>() == null) root.AddComponent<FieldJournalScreen>();
            return;
        }
    }

    // Colours of the Guided popups / journal mock-up.
    static readonly Color Ink = new(0.12f, 0.16f, 0.2f);
    static readonly Color Body = new(0.2f, 0.24f, 0.28f);
    static readonly Color Muted = new(0.45f, 0.5f, 0.54f);
    static readonly Color Row = new(0.93f, 0.96f, 0.95f);
    static readonly Color RowSelected = new(0.8f, 0.92f, 0.84f);
    static readonly Color Card = new(0.95f, 0.97f, 0.96f);
    static readonly Color Green = new(0.3f, 0.62f, 0.38f);
    static readonly Color Blue = new(0.28f, 0.5f, 0.75f);
    static readonly Color Orange = new(0.85f, 0.51f, 0.17f);
    static readonly Color Grey = new(0.62f, 0.64f, 0.66f);
    static readonly Color TabSelected = new(0.8f, 0.92f, 0.84f);

    // Did You Know? facts. Placeholders built from the facts already in the game (inspection panel) plus
    // well-known ones; replace with the research set DY01-DY10 (SCRUM-18) when it is written.
    static readonly (string title, Color color, string[] facts)[] FactCards =
    {
        ("Snow Leopard", Orange, new[]
        {
            "Snow leopards live alone and are so hard to spot that people call them the “ghost of the mountains”.",
            "Their wide, furry paws work like snowshoes, so they don't sink into deep snow.",
            "Their long, thick tail helps them balance on rocky cliffs, and they wrap it around themselves to keep warm.",
            "Snow leopards can't roar. They hiss, growl and make a friendly puffing sound called a chuff.",
        }),
        ("Blue Sheep", Blue, new[]
        {
            "Blue sheep are excellent climbers and run up steep cliffs to escape snow leopards.",
            "Their grey-blue coat matches the rocks, so when they stand still they are very hard to see.",
            "They live in herds, so many eyes can watch out for danger.",
            "Blue sheep are the snow leopard's main food in the Himalaya.",
        }),
        ("The Food Chain", Green, new[]
        {
            "Grass feeds the blue sheep, and blue sheep feed the snow leopards. This is called a food chain.",
            "Plants make their own food from sunlight and water.",
            "If there are too many sheep, they eat the grass faster than it can grow back.",
            "Snow leopards keep the number of sheep in balance, which gives the grass time to recover.",
        }),
    };

    // The animal card's "Did you know?" line: the next fact for that species each time an animal is
    // tapped, so tapping sheep after sheep (or leopard after leopard) shows different facts.
    static readonly Dictionary<bool, int> nextFact = new();

    /// <summary>Next fun fact for this species (cycles through its Did You Know? card).</summary>
    public static string NextFactFor(bool predator)
    {
        var facts = FactCards[predator ? 0 : 1].facts;
        nextFact.TryGetValue(predator, out int i);
        nextFact[predator] = (i + 1) % facts.Length;
        return facts[i % facts.Length];
    }

    readonly Dictionary<string, Transform> byName = new();
    RectTransform panel;
    TextMeshProUGUI headerTemplate, subTemplate;
    Sprite roundSprite;
    readonly List<GameObject> ecosystemPage = new();
    GameObject investigationsPage, observationsPage, factsPage;
    readonly Dictionary<Page, (Image image, Color normal)> tabs = new();
    readonly Dictionary<Page, string> titles = new()
    {
        { Page.Ecosystem, "Ecosystem" }, { Page.Investigations, "Investigation" },
        { Page.Observations, "Observations" }, { Page.Facts, "Did You Know?" },
    };
    Page current;
    TextMeshProUGUI previousCaption, nextCaption;
    GameObject newBadge;

    // Investigations
    FieldJournalCases cases;
    CaseSeenTracker seen;
    int selected = -1;
    RectTransform listContent;
    TextMeshProUGUI invSub, detailPill, detailTitle, detailBody;
    Image detailPillBg;
    GameObject graphLink;

    // Observations
    PopulationGraph graph;
    TextMeshProUGUI obsSub, summary;
    // Play / Pause and speed for the run behind the journal (it opens paused, so the graph would never move).
    SimulationSpeedController liveSpeed;
    Image playBg;
    TextMeshProUGUI playLabel, speedLabel;

    /// <summary>A Free Exploration run is open behind the journal, so locked cases can still be found in it.</summary>
    static bool InFreeRun => FieldJournalObservations.IsLive && !FieldJournalObservations.WasGuided;

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string key = t.name.Trim().Trim('\'').Trim();
            if (!byName.ContainsKey(key)) byName[key] = t;
        }
        panel = Find("JournalPanel") as RectTransform;
        if (panel == null)
        {
            Debug.LogWarning("[FieldJournalScreen] No JournalPanel in the Field Journal scene; pages not added.", this);
            return;
        }
        headerTemplate = Find("HeaderTxt")?.GetComponent<TextMeshProUGUI>();
        subTemplate = Find("SubTxt")?.GetComponent<TextMeshProUGUI>();
        // Cards use the tab buttons' white sprite: JournalPanel's own "Background" sprite is grey and made them look muddy.
        var tabImage = Find("EcosystemBtn")?.GetComponent<Image>();
        roundSprite = tabImage != null && tabImage.sprite != null ? tabImage.sprite : panel.GetComponent<Image>()?.sprite;

        foreach (Transform child in panel)
            if (child.name != "HeaderTxt_02") ecosystemPage.Add(child.gameObject);

        cases = FieldJournalCases.Find();
        seen = CaseSeenTracker.ForDevice();
        seen.Prune(cases.Journal);
        if (cases.Detector != null) cases.Detector.CaseUnlocked += OnCaseUnlocked;

        // Tabs first: their labels name the pages, so Previous / Next say the same words.
        Tab("EcosystemBtn", Page.Ecosystem);
        Tab("InvestigationBtn", Page.Investigations);
        Tab("ObservationBtn", Page.Observations);
        Tab("DYKBtn", Page.Facts);
        Tab("RunsBtn", Page.Runs);
        Wire("PreviousBtn", () => Show(Previous(current)));
        Wire("NextBtn", () => Show(Next(current)));
        // The arrows are blank squares in the scene: label them with the page they lead to.
        previousCaption = ButtonCaption.Ensure(Find("PreviousBtn")?.GetComponent<Button>(), "", 30f);
        nextCaption = ButtonCaption.Ensure(Find("NextBtn")?.GetComponent<Button>(), "", 30f);
        newBadge = Badge(Find("InvestigationBtn"));

        BuildInvestigations();
        BuildObservations();
        BuildFacts();
        BuildRuns();      // new

        selected = cases.PickInitial(seen, requestedCaseKey);
        // Inside a run the journal is mostly for watching that run, so open on its graph.
        Show(requestedPage ?? (FieldJournalObservations.IsLive ? Page.Observations : Page.Ecosystem));
        requestedPage = null;
        requestedCaseKey = null;
    }

    void OnDestroy()
    {
        if (cases?.Detector != null) cases.Detector.CaseUnlocked -= OnCaseUnlocked;
        if (runTex != null) Destroy(runTex);
    }

    void OnCaseUnlocked(InvestigationCase _)
    {
        RefreshInvestigations();
        RefreshBadge();
    }

    public void Show(Page page)
    {
        current = page;
        foreach (var go in ecosystemPage) if (go != null) go.SetActive(page == Page.Ecosystem);
        if (investigationsPage != null) investigationsPage.SetActive(page == Page.Investigations);
        if (observationsPage != null) observationsPage.SetActive(page == Page.Observations);
        if (runsPage != null) runsPage.SetActive(page == Page.Runs);
        if (factsPage != null) factsPage.SetActive(page == Page.Facts);
        foreach (var kv in tabs) kv.Value.image.color = kv.Key == page ? TabSelected : kv.Value.normal;
        if (previousCaption != null) previousCaption.text = $"<size=220%>‹</size>\n{titles[Previous(page)]}";
        if (nextCaption != null) nextCaption.text = $"<size=220%>›</size>\n{titles[Next(page)]}";
        if (page == Page.Investigations) RefreshInvestigations();
        if (page == Page.Runs) RefreshRuns();
        RefreshBadge();
    }

    static Page Previous(Page p) => (Page)(((int)p + PageCount - 1) % PageCount);
    static Page Next(Page p) => (Page)(((int)p + 1) % PageCount);

    // "NEW" on the Investigation tab while a discovered case hasn't been read.
    void RefreshBadge()
    {
        if (newBadge == null || cases == null) return;
        bool any = false;
        foreach (var c in cases.Cases)
            if (c != null && seen.IsNew(cases.Journal, c.Key)) { any = true; break; }
        newBadge.SetActive(any);
    }

    // ---------- Investigations ----------

    void BuildInvestigations()
    {
        investigationsPage = PageRoot("InvestigationsPage");
        var p = investigationsPage.transform;
        Header(p, "Investigations");
        invSub = Sub(p, "");

        // Left: scrolling list of cases.
        // Cards end above the PreviousBtn / NextBtn squares in the bottom corners.
        var view = Box(p, "CaseList", new Vector2(-640f, -60f), new Vector2(980f, 880f), new Color(1f, 1f, 1f, 0f));
        view.gameObject.AddComponent<RectMask2D>();
        listContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        listContent.SetParent(view, false);
        listContent.anchorMin = new Vector2(0f, 1f); listContent.anchorMax = Vector2.one; listContent.pivot = new Vector2(0.5f, 1f);
        listContent.sizeDelta = Vector2.zero;
        var v = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 20f; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = listContent; scroll.viewport = view; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Right: the selected case.
        var detail = Box(p, "CaseDetail", new Vector2(450f, -60f), new Vector2(980f, 880f), Card);
        detailPillBg = Box(detail, "StatusPill", Vector2.zero, new Vector2(240f, 56f), Grey).GetComponent<Image>();
        var pill = (RectTransform)detailPillBg.transform;
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0f, 1f);
        pill.anchoredPosition = new Vector2(56f, -56f);
        detailPill = Text(pill, "", 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Fill(detailPill.rectTransform);
        detailTitle = Text(detail, "", 56f, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(detailTitle.rectTransform, new Vector2(56f, -140f), new Vector2(868f, 150f));
        detailBody = Text(detail, "", 38f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(detailBody.rectTransform, new Vector2(56f, -310f), new Vector2(868f, 420f));

        // From a solved case to the graph of the run where it happened.
        var link = Box(detail, "SeeGraphBtn", Vector2.zero, new Vector2(440f, 90f), Blue);
        link.anchorMin = link.anchorMax = link.pivot = new Vector2(0f, 0f);
        link.anchoredPosition = new Vector2(56f, 48f);
        link.gameObject.AddComponent<Button>().onClick.AddListener(() => Show(Page.Observations));
        Fill(Text(link, "See it on the graph  ›", 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);
        graphLink = link.gameObject;
    }

    void RefreshInvestigations()
    {
        if (listContent == null) return;
        int total = cases.Cases.Count, found = cases.UnlockedCount;
        string findMore = InFreeRun ? "Keep watching this run to find the rest." : "Play Free Exploration to find the rest.";
        invSub.text = total == 0 ? "No investigations yet."
            : found == total ? $"All {total} cases solved. Great detective work!"
            : found == 0 ? $"{total} cases to discover. Read a clue, then watch what happens to the animals."
            : $"{found} of {total} cases discovered. {findMore}";

        foreach (Transform c in listContent) { c.gameObject.SetActive(false); Destroy(c.gameObject); }   // Destroy waits for the frame end
        for (int i = 0; i < total; i++) CaseRow(i);
        ShowDetail();
    }

    void CaseRow(int index)
    {
        var c = cases.Cases[index];
        bool open = cases.IsUnlocked(c);
        bool isNew = seen.IsNew(cases.Journal, c.Key);

        var row = Box(listContent, $"Case_{index + 1}", Vector2.zero, Vector2.zero, index == selected ? RowSelected : Row);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 150f;
        int i = index;
        row.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(i));

        var title = Text(row, open ? $"Case {index + 1}: {c.title}" : $"Case {index + 1}: ???", 40f, open ? Ink : Muted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(40f, 50f); title.rectTransform.offsetMax = new Vector2(-230f, -16f);
        var sub = Text(row, open ? "Discovered. Tap to read what happened." : "Not discovered yet. Tap for a clue.", 28f, Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        sub.rectTransform.anchorMin = Vector2.zero; sub.rectTransform.anchorMax = new Vector2(1f, 0f);
        sub.rectTransform.pivot = new Vector2(0.5f, 0f);
        sub.rectTransform.offsetMin = new Vector2(40f, 16f); sub.rectTransform.offsetMax = new Vector2(-230f, 60f);

        var (label, color) = isNew ? ("NEW", Orange) : open ? ("SOLVED", Green) : ("LOCKED", Grey);
        var pill = Box(row, "Pill", Vector2.zero, new Vector2(180f, 56f), color);
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(1f, 0.5f);
        pill.anchoredPosition = new Vector2(-32f, 0f);
        var pt = Text(pill, label, 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Fill(pt.rectTransform);
    }

    void Select(int index)
    {
        selected = index;
        RefreshInvestigations();
    }

    void ShowDetail()
    {
        graphLink.SetActive(false);
        if (selected < 0 || selected >= cases.Cases.Count)
        {
            detailPillBg.gameObject.SetActive(false);
            detailTitle.text = "No cases yet";
            detailBody.text = "Investigation cases will appear here.";
            return;
        }
        var c = cases.Cases[selected];
        bool open = cases.IsUnlocked(c);
        bool wasNew = seen.IsNew(cases.Journal, c.Key);
        detailPillBg.gameObject.SetActive(true);
        detailPillBg.color = wasNew ? Orange : open ? Green : Grey;
        detailPill.text = wasNew ? "NEW!" : open ? "SOLVED" : "LOCKED";

        if (open)
        {
            detailTitle.text = c.title;
            detailBody.text = $"<b>What happened?</b>\n{c.explanation}";
            graphLink.SetActive(FieldJournalObservations.HasData && FieldJournalObservations.UnlockedInShownRun(c.Key));
            if (wasNew)
            {
                // Reading it clears the NEW tag (for the list next time, the tab badge and queued popups).
                seen.MarkSeen(c.Key);
                RefreshBadge();
            }
        }
        else
        {
            detailTitle.text = $"Case {selected + 1}: ???";
            string how = InFreeRun
                ? "Keep the simulation running and watch what happens to the animals. This case unlocks when it happens."
                : "Play Free Exploration and watch what happens to the animals. This case unlocks when it happens.";
            detailBody.text = $"<b>Clue</b>\n{c.hint}\n\n<color=#73808A>{how}</color>";
        }
    }

    // ---------- Observations ----------

    void BuildObservations()
    {
        observationsPage = PageRoot("ObservationsPage");
        var p = observationsPage.transform;
        Header(p, "Observations");
        obsSub = Sub(p, "");

        var graphBox = Box(p, "GraphCard", new Vector2(-370f, -60f), new Vector2(1540f, 880f), Card);
        var inner = new GameObject("Inner", typeof(RectTransform)).GetComponent<RectTransform>();
        inner.SetParent(graphBox, false);
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(24f, 16f); inner.offsetMax = new Vector2(-24f, -24f);
        graph = PopulationGraph.Create(inner, subTemplate != null ? subTemplate.font : null);
        graph.labelSize = 30f;
        graph.textColor = Body;

        var side = Box(p, "SummaryCard", new Vector2(720f, -60f), new Vector2(440f, 880f), Card);
        summary = Text(side, "", 32f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Inside a run: let the player run it from here and watch the lines move, at any speed.
        liveSpeed = FieldJournalObservations.IsLive ? FindAnyObjectByType<SimulationSpeedController>() : null;
        Place(summary.rectTransform, new Vector2(36f, -36f), new Vector2(368f, liveSpeed != null ? 680f : 808f));
        if (liveSpeed != null)
        {
            playBg = RunButton(side, "GraphPlayBtn", 36f, Green, () =>
            {
                if (liveSpeed.IsRunning) liveSpeed.Pause(); else liveSpeed.Play();
                RefreshRunControls();
            }, out playLabel).GetComponent<Image>();
            RunButton(side, "GraphSpeedBtn", 228f, Blue, () => { liveSpeed.CycleSpeed(); RefreshRunControls(); }, out speedLabel);
            RefreshRunControls();
        }

        // The run going on now, or the last one (kept after leaving it).
        graph.Redrawn += RefreshSummary;
        graph.Bind(FieldJournalObservations.History, FieldJournalObservations.Series, FieldJournalObservations.Days);
    }

    RectTransform RunButton(Transform card, string name, float x, Color color, UnityAction onClick, out TextMeshProUGUI label)
    {
        var b = Box(card, name, Vector2.zero, new Vector2(176f, 96f), color);
        b.anchorMin = b.anchorMax = b.pivot = Vector2.zero;
        b.anchoredPosition = new Vector2(x, 36f);
        b.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        label = Text(b, "", 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Fill(label.rectTransform);
        return b;
    }

    // Also run every frame: the run can stop or change speed elsewhere (the Leave box, a Guided popup...).
    void RefreshRunControls()
    {
        if (liveSpeed == null || playLabel == null) return;
        bool running = liveSpeed.IsRunning;
        string play = running ? "Pause" : "Play";
        if (playLabel.text != play) playLabel.text = play;
        playBg.color = running ? Orange : Green;
        string speed = $"Speed {liveSpeed.CurrentSpeed:0}x";
        if (speedLabel.text != speed) speedLabel.text = speed;
    }

    void Update()
    {
        if (current == Page.Observations) RefreshRunControls();
    }

    void RefreshSummary()
    {
        var h = graph.History;
        bool live = FieldJournalObservations.IsLive;
        if (h == null || h.Count == 0)
        {
            obsSub.text = "How each animal's number changes over time.";
            summary.text = "<b>Nothing observed yet</b>\n\nStart an exploration and the journal records how many animals there are every day.";
            return;
        }
        var first = h[0];
        var last = h.Latest;
        string day = graph.TimeLabel(last.Tick);
        string mode = FieldJournalObservations.WasGuided ? "Guided Exploration" : "Free Exploration";
        obsSub.text = (live ? $"This {mode} run so far." : $"Your last {mode} run, up to {day}.")
                      + " Tap a name to show or hide its line.";

        // A long or fast run fills the history and its oldest days are dropped: compare with the first day still kept.
        string since = h.Count >= h.Capacity ? $"since {graph.TimeLabel(first.Tick)}" : "since the start";
        var sb = new StringBuilder();
        sb.Append("<b>").Append(live ? "Right now" : "At the end").Append(" · ").Append(day).Append("</b>\n");
        foreach (var s in graph.Series)
        {
            if (s.IsResource && !s.Visible) continue;
            float now = last.Get(s.Key), start = first.Get(s.Key), hi = float.MinValue, lo = float.MaxValue;
            for (int i = 0; i < h.Count; i++) { float v = h[i].Get(s.Key); hi = Mathf.Max(hi, v); lo = Mathf.Min(lo, v); }
            float change = now - start;
            string trend = Mathf.Abs(change) < 0.5f ? $"no change {since}"
                : change > 0 ? $"up {change:0} {since}" : $"down {-change:0} {since}";
            sb.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(s.Color)).Append("><b>").Append(s.Label).Append("</b></color>\n")
              .Append("<size=120%><b>").Append(now.ToString("0")).Append("</b></size>  ").Append(trend).Append('\n')
              .Append("<color=#73808A>Highest ").Append(hi.ToString("0")).Append(" · Lowest ").Append(lo.ToString("0")).Append("</color>\n");
        }
        summary.text = sb.ToString();
    }


    // ---------- My Runs ----------

    void BuildRuns()
    {
        runsPage = PageRoot("RunsPage");
        var p = runsPage.transform;
        Header(p, "My Runs");
        runsSub = Sub(p, "");

        // Left: scrolling list of saved runs (same setup as the case list).
        var view = Box(p, "RunList", new Vector2(-640f, -60f), new Vector2(980f, 880f), new Color(1f, 1f, 1f, 0f));
        view.gameObject.AddComponent<RectMask2D>();
        runsContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        runsContent.SetParent(view, false);
        runsContent.anchorMin = new Vector2(0f, 1f); runsContent.anchorMax = Vector2.one; runsContent.pivot = new Vector2(0.5f, 1f);
        runsContent.sizeDelta = Vector2.zero;
        var v = runsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 20f; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        runsContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = runsContent; scroll.viewport = view; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Right: the selected run.
        var detail = Box(p, "RunDetail", new Vector2(450f, -60f), new Vector2(980f, 880f), Card);
        runTitle = Text(detail, "", 52f, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(runTitle.rectTransform, new Vector2(56f, -40f), new Vector2(868f, 80f));

        var go = new GameObject("RunGraph", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(detail, false);
        runGraph = go.GetComponent<RawImage>();
        runGraph.raycastTarget = false;
        Place(runGraph.rectTransform, new Vector2(56f, -130f), new Vector2(868f, 300f));

        runLegend = Text(detail, "", 30f, Body, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(runLegend.rectTransform, new Vector2(56f, -440f), new Vector2(868f, 50f));
        runBody = Text(detail, "", 36f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(runBody.rectTransform, new Vector2(56f, -510f), new Vector2(868f, 340f));
    }

    void RefreshRuns()
    {
        if (runsContent == null) return;
        pastRuns = RunHist.Load().runs;
        pastRuns.Reverse();                                   // newest first
        selectedRun = Mathf.Clamp(selectedRun, 0, Mathf.Max(0, pastRuns.Count - 1));

        runsSub.text = pastRuns.Count == 0
            ? "No runs saved yet. Finish a run and it will show up here."
            : $"Your last {pastRuns.Count} runs. Tap one to see how it went.";

        // The longest run gets the BEST tag (the newest one if there is a tie).
        int best = -1;
        for (int i = 0; i < pastRuns.Count; i++)
            if (best < 0 || pastRuns[i].days_survived > pastRuns[best].days_survived) best = i;

        foreach (Transform c in runsContent) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
        for (int i = 0; i < pastRuns.Count; i++) RunRow(i, i == best);
        ShowRunDetail();
    }

    static string RunDate(MyRuns r) =>
        System.DateTime.TryParse(r.start, out var dt)
            ? dt.ToString("d MMM, HH:mm", System.Globalization.CultureInfo.InvariantCulture)
            : r.id;

    void RunRow(int index, bool isBest)
    {
        var r = pastRuns[index];
        var row = Box(runsContent, $"Run_{index + 1}", Vector2.zero, Vector2.zero, index == selectedRun ? RowSelected : Row);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
        int i = index;
        row.gameObject.AddComponent<Button>().onClick.AddListener(() => { selectedRun = i; RefreshRuns(); });

        var title = Text(row, RunDate(r), 40f, Ink, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(40f, 50f); title.rectTransform.offsetMax = new Vector2(-230f, -16f);

        var sub = Text(row, $"{r.mode} · lasted {r.days_survived} days", 28f, Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        sub.rectTransform.anchorMin = Vector2.zero; sub.rectTransform.anchorMax = new Vector2(1f, 0f);
        sub.rectTransform.pivot = new Vector2(0.5f, 0f);
        sub.rectTransform.offsetMin = new Vector2(40f, 16f); sub.rectTransform.offsetMax = new Vector2(-230f, 60f);

        var pill = Box(row, "Pill", Vector2.zero, new Vector2(180f, 56f), isBest ? Orange : Green);
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(1f, 0.5f);
        pill.anchoredPosition = new Vector2(-32f, 0f);
        Fill(Text(pill, isBest ? "BEST" : $"{r.stars} / 3 stars", 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);
    }

    void ShowRunDetail()
    {
        if (pastRuns.Count == 0)
        {
            runTitle.text = "No runs yet";
            runBody.text = "Play an exploration. When you leave it, the run is saved here so you can compare it next time.";
            runLegend.text = "";
            runGraph.gameObject.SetActive(false);
            return;
        }
        runGraph.gameObject.SetActive(true);
        var r = pastRuns[selectedRun];
        var before = selectedRun + 1 < pastRuns.Count ? pastRuns[selectedRun + 1] : null;   // the run just before this one

        runTitle.text = $"{RunDate(r)} · {r.mode}";
        DrawRunGraph(r);
        runLegend.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Blue)}>Blue sheep</color>    " +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(Orange)}>Snow leopards</color>    " +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(Green)}>Trees</color>";

        var sb = new StringBuilder();
        sb.Append("<b>").Append(CompareRuns(r, before)).Append("</b>\n\n");
        sb.Append(OutcomeText(r.outcome)).Append("\n\n");
        sb.Append("Most blue sheep: ").Append(r.peaksheep).Append('\n');
        sb.Append("Most snow leopards: ").Append(r.peakleopard).Append('\n');
        sb.Append("At the end: ").Append(r.endsheep).Append(" sheep, ").Append(r.endleopard).Append(" leopards, ").Append(r.endtrees).Append(" trees");
        if (r.cases != null && r.cases.Count > 0) sb.Append("\nCases found: ").Append(r.cases.Count);
        runBody.text = sb.ToString();
    }

    static string CompareRuns(MyRuns now, MyRuns before)
    {
        if (before == null) return "This was your first saved run. Come back and beat it!";
        int diff = now.days_survived - before.days_survived;
        string head = $"Run before: {before.days_survived} days. This run: {now.days_survived} days.";
        return diff > 0 ? $"{head}\nYou lasted {diff} days longer!"
             : diff < 0 ? $"{head}\nSo close! Can you beat {before.days_survived} days?"
             : $"{head}\nSame as last time. Try something different!";
    }

    static string OutcomeText(string outcome) => outcome switch
    {
        "BothSurvived" => "Both animals made it to the end!",
        "SheepDied" => "The blue sheep ran out. What could help them next time?",
        "LeopardDied" => "The snow leopards ran out. What could help them next time?",
        _ => ""
    };

    // Small line graph drawn straight into a texture, so it doesn't depend on PopulationGraph.
    void DrawRunGraph(MyRuns r)
    {
        const int W = 868, H = 300;
        if (runTex != null) Destroy(runTex);
        runTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);

        int max = 1;
        foreach (var v in r.sheep) max = Mathf.Max(max, v);
        foreach (var v in r.leopard) max = Mathf.Max(max, v);
        foreach (var v in r.trees) max = Mathf.Max(max, v);

        PlotLine(px, W, H, r.trees, max, Green);
        PlotLine(px, W, H, r.sheep, max, Blue);
        PlotLine(px, W, H, r.leopard, max, Orange);

        runTex.SetPixels32(px);
        runTex.Apply();
        runGraph.texture = runTex;
    }

    static void PlotLine(Color32[] px, int W, int H, List<int> s, int max, Color color)
    {
        int n = s.Count;
        if (n == 0) return;
        Color32 c = color;
        int prevY = -1;
        for (int x = 0; x < W; x++)
        {
            float f = n == 1 ? 0f : x * (n - 1f) / (W - 1f);
            int i0 = (int)f, i1 = Mathf.Min(i0 + 1, n - 1);
            float val = Mathf.Lerp(s[i0], s[i1], f - i0);
            int y = 6 + Mathf.RoundToInt(val / max * (H - 12));
            int a = prevY < 0 ? y : Mathf.Min(prevY, y);
            int b = prevY < 0 ? y : Mathf.Max(prevY, y);
            for (int yy = Mathf.Max(0, a - 2); yy <= Mathf.Min(H - 1, b + 2); yy++) px[yy * W + x] = c;
            prevY = y;
        }
    }


    // ---------- Did You Know? ----------

    void BuildFacts()
    {
        factsPage = PageRoot("FactsPage");
        var p = factsPage.transform;
        Header(p, titles[Page.Facts]);
        Sub(p, "Fun facts about the animals of the Himalayan Highlands.");

        const float width = 650f, gap = 30f, left = -1130f;
        for (int i = 0; i < FactCards.Length; i++)
        {
            var (title, color, facts) = FactCards[i];
            var card = Box(p, $"FactCard_{i + 1}", new Vector2(left + width / 2f + i * (width + gap), -60f), new Vector2(width, 880f), Card);
            var band = Box(card, "Band", Vector2.zero, new Vector2(0f, 110f), color);
            band.anchorMin = new Vector2(0f, 1f); band.anchorMax = Vector2.one; band.pivot = new Vector2(0.5f, 1f);
            band.offsetMin = new Vector2(0f, -110f); band.offsetMax = Vector2.zero;
            Fill(Text(band, title, 48f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);

            var sb = new StringBuilder();
            foreach (var f in facts) sb.Append("•  ").Append(f).Append("\n\n");
            var body = Text(card, sb.ToString().TrimEnd(), 34f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, new Vector2(40f, -150f), new Vector2(width - 80f, 690f));
        }
    }

    // ---------- Scene wiring ----------

    Transform Find(string name) => byName.TryGetValue(name, out var t) ? t : null;

    void Tab(string buttonName, Page page)
    {
        var t = Find(buttonName);
        if (t == null || !t.TryGetComponent(out Button b)) return;
        if (b.TryGetComponent(out Image img)) tabs[page] = (img, img.color);
        var label = b.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !string.IsNullOrWhiteSpace(label.text))
            titles[page] = label.text.Replace("\n", " ").Replace("\r", " ")
                               .Trim().Trim('\'', '"').Replace(" Facts", "").Trim();
        Rewire(b, () => Show(page));
    }

    void Wire(string buttonName, UnityAction action)
    {
        var t = Find(buttonName);
        if (t != null && t.TryGetComponent(out Button b)) Rewire(b, action);
    }

    // Same as ScreenFlow: a fresh click list (old Inspector actions don't always switch off) (UIClickSound adds the click sound).
    static void Rewire(Button b, UnityAction action)
    {
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    // Small orange "NEW" tag on the top-right corner of a tab.
    GameObject Badge(Transform tab)
    {
        if (tab == null) return null;
        var badge = Box(tab, "NewBadge", Vector2.zero, new Vector2(96f, 44f), Orange);
        badge.anchorMin = badge.anchorMax = Vector2.one;
        badge.pivot = new Vector2(0.75f, 0.6f);
        badge.anchoredPosition = Vector2.zero;
        Fill(Text(badge, "NEW", 26f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);
        badge.gameObject.SetActive(false);
        return badge.gameObject;
    }

    // ---------- Building blocks ----------

    // A page covering JournalPanel; children use the same centre-based positions as the Ecosystem page.
    GameObject PageRoot(string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(panel, false);
        Fill(rt);
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    void Header(Transform parent, string text)
    {
        TextMeshProUGUI t;
        if (headerTemplate != null) t = Instantiate(headerTemplate, parent);
        else { t = Text(parent, "", 64f, Ink, FontStyles.Bold, TextAlignmentOptions.Left); Place(t.rectTransform, new Vector2(-1015f, 500f), new Vector2(671f, 83f), center: true); }
        t.name = "HeaderTxt";
        t.text = text;
    }

    TextMeshProUGUI Sub(Transform parent, string text)
    {
        TextMeshProUGUI t;
        if (subTemplate != null) t = Instantiate(subTemplate, parent);
        else { t = Text(parent, "", 32f, Body, FontStyles.Normal, TextAlignmentOptions.Left); Place(t.rectTransform, new Vector2(236f, 490f), new Vector2(549f, 62f), center: true); }
        t.name = "SubTxt";
        t.text = text;
        // Wider than the original subtitle box so the progress line fits on one or two lines.
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 860f), Mathf.Max(rt.sizeDelta.y, 90f));
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = t.fontSize * 0.6f;
        return t;
    }

    RectTransform Box(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        if (roundSprite != null) { img.sprite = roundSprite; img.type = Image.Type.Sliced; }
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        rt.sizeDelta = size;
        return rt;
    }

    TextMeshProUGUI Text(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        if (subTemplate != null) t.font = subTemplate.font;
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.richText = true;
        t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = size * 0.55f;
        return t;
    }

    // Top-left anchored box inside a card (or centre-based when center is true).
    static void Place(RectTransform rt, Vector2 pos, Vector2 size, bool center = false)
    {
        rt.anchorMin = rt.anchorMax = center ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
        rt.pivot = center ? new Vector2(0f, 0.5f) : new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
