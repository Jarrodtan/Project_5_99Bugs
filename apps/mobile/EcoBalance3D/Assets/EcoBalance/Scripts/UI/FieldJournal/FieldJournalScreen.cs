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
///   Observations (wireframe 06): the population graph of the run going on now (it keeps moving while the
///     journal is open), or of the last run, with numbered "What we noticed" notes pinned on it and, in Free
///     Exploration, the clue of a case still to find. Two views: Animals (how many of each) and Food &amp; Water
///     (grass and water left, grass eaten and water drunk in the last day, famine and thirst levels, with
///     "eaten today" tiles and green-to-red famine / thirst meters next to it).
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
    TextMeshProUGUI runsSub, runTitle, runBody;
    PopulationGraph runGraph;
    TextMeshProUGUI starsHint;

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
    List<ObservationNote> shownNotes = new();
    // Play / Pause and speed for the run behind the journal, which keeps running while the journal is open.
    SimulationSpeedController liveSpeed;
    Image playBg;
    TextMeshProUGUI playLabel, speedLabel;
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5): the Food & Water view
    bool foodView;
    Image animalsTab, foodTab;
    TextMeshProUGUI animalsTabLabel, foodTabLabel;
    GameObject foodWidgets;
    TextMeshProUGUI grassTileValue, grassTileFoot, waterTileValue, waterTileFoot;
    Meter famineMeter, thirstMeter;

    sealed class Meter
    {
        public TextMeshProUGUI band;
        public RectTransform needle;
        public RingDotGraphic needleDot;
        public float left, width;
    }
    // [AI-GENERATED END]

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

        var graphBox = Box(p, "GraphCard", new Vector2(-500f, -60f), new Vector2(1260f, 880f), Card);
        bool hasFood = FieldJournalObservations.FoodSeries != null;
        if (hasFood)
        {
            // Two views of the same run: the animals, and the food and water they need.
            animalsTab = ViewTab(graphBox, "AnimalsViewBtn", "Animals", 24f, false, out animalsTabLabel);
            foodTab = ViewTab(graphBox, "FoodWaterViewBtn", "Food & Water", 24f + 330f, true, out foodTabLabel);
        }
        var inner = new GameObject("Inner", typeof(RectTransform)).GetComponent<RectTransform>();
        inner.SetParent(graphBox, false);
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(24f, 16f); inner.offsetMax = new Vector2(-24f, hasFood ? -112f : -24f);
        graph = PopulationGraph.Create(inner, subTemplate != null ? subTemplate.font : null);
        graph.labelSize = 30f;
        graph.textColor = Body;
        graph.pillSprite = RoundedSprite.Get();

        var side = Box(p, "SummaryCard", new Vector2(550f, -60f), new Vector2(780f, 880f), Card);
        summary = Text(side, "", 30f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);   // first text in the card (tests read it)
        if (hasFood) BuildFoodWidgets(side);

        // Inside a run: let the player run it from here and watch the lines move, at any speed.
        liveSpeed = FieldJournalObservations.IsLive ? FindAnyObjectByType<SimulationSpeedController>() : null;
        PlaceSummary();
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

        // The run going on now, or the last one (kept after leaving it). The notes are worked out on every
        // redraw, so the numbered pins on the graph and the list next to it always match.
        graph.MarkerSource = NoteMarkers;
        graph.Redrawn += RefreshSummary;
        ShowView(false);
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>Animals (counts) or Food &amp; Water (grass, water, eaten, drunk, famine and thirst levels, all in %).</summary>
    public void ShowView(bool food)
    {
        foodView = food && FieldJournalObservations.FoodSeries != null;
        graph.valueSuffix = foodView ? "%" : "";
        graph.minAxisMax = foodView ? 100f : 0f;
        graph.Bind(FieldJournalObservations.History, foodView ? FieldJournalObservations.FoodSeries : FieldJournalObservations.Series, FieldJournalObservations.Days);
        if (animalsTab != null)
        {
            animalsTab.color = foodView ? Row : Green;
            foodTab.color = foodView ? Green : Row;
            animalsTabLabel.color = foodView ? Body : Color.white;
            foodTabLabel.color = foodView ? Color.white : Body;
        }
        if (foodWidgets != null) foodWidgets.SetActive(foodView);
        PlaceSummary();
    }

    public bool FoodViewShown => foodView;

    static void Round(RectTransform box)
    {
        if (box != null && box.TryGetComponent(out Image img)) { img.sprite = RoundedSprite.Get(); img.type = Image.Type.Sliced; }
    }

    /// <summary>Notes listed in the Food &amp; Water view (less room under the tiles and meters).</summary>
    public const int FoodViewNotes = 3;

    // Food & Water: the summary text goes under the tiles and meters.
    void PlaceSummary()
    {
        float top = foodView ? 452f : 36f;
        float bottom = liveSpeed != null ? 160f : 36f;
        Place(summary.rectTransform, new Vector2(36f, -top), new Vector2(708f, 880f - top - bottom));
    }

    Image ViewTab(Transform card, string name, string text, float x, bool food, out TextMeshProUGUI label)
    {
        var b = Box(card, name, Vector2.zero, new Vector2(310f, 72f), Row);
        b.anchorMin = b.anchorMax = b.pivot = new Vector2(0f, 1f);
        b.anchoredPosition = new Vector2(x, -22f);
        Round(b);
        b.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowView(food));
        label = Text(b, text, 32f, Body, FontStyles.Bold, TextAlignmentOptions.Center);
        Fill(label.rectTransform);
        return b.GetComponent<Image>();
    }

    // Two "eaten in the last day" tiles, then the famine and thirst meters (green = full, red = starving).
    void BuildFoodWidgets(Transform card)
    {
        var root = new GameObject("FoodWaterWidgets", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(card, false);
        Fill(root);
        foodWidgets = root.gameObject;

        Tile(root, "GrassTile", 36f, FoodWater.GrassColor, "of the grass eaten today", out grassTileValue, out grassTileFoot);
        Tile(root, "WaterTile", 402f, FoodWater.WaterColor, "of the water drunk today", out waterTileValue, out waterTileFoot);
        famineMeter = MeterRow(root, "FamineMeter", "Famine level", 214f, "Well fed", "Starving");
        thirstMeter = MeterRow(root, "ThirstMeter", "Thirst level", 334f, "Not thirsty", "Very thirsty");
        foodWidgets.SetActive(false);
    }

    void Tile(Transform parent, string name, float x, Color color, string caption, out TextMeshProUGUI value, out TextMeshProUGUI foot)
    {
        var tint = Color.Lerp(color, Color.white, 0.86f);
        var tile = Box(parent, name, Vector2.zero, new Vector2(342f, 160f), tint);
        Round(tile);
        tile.anchorMin = tile.anchorMax = tile.pivot = new Vector2(0f, 1f);
        tile.anchoredPosition = new Vector2(x, -30f);
        var stripe = Box(tile, "Stripe", Vector2.zero, new Vector2(12f, 0f), color);
        stripe.anchorMin = Vector2.zero; stripe.anchorMax = new Vector2(0f, 1f); stripe.pivot = new Vector2(0f, 0.5f);
        stripe.offsetMin = new Vector2(14f, 18f); stripe.offsetMax = new Vector2(26f, -18f);
        value = Text(tile, "-", 60f, color, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(value.rectTransform, new Vector2(42f, -10f), new Vector2(290f, 70f));
        var cap = Text(tile, caption, 24f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(cap.rectTransform, new Vector2(42f, -78f), new Vector2(290f, 36f));
        foot = Text(tile, "", 22f, Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(foot.rectTransform, new Vector2(42f, -116f), new Vector2(290f, 32f));
    }

    Meter MeterRow(Transform parent, string name, string title, float top, string low, string high)
    {
        var m = new Meter { left = 36f, width = 708f };
        var t = Text(parent, $"<b>{title}</b>", 30f, Ink, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        t.name = name + "Title";
        Place(t.rectTransform, new Vector2(m.left, -top), new Vector2(m.width, 44f));
        m.band = Text(parent, "", 30f, Ink, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        m.band.name = name + "Band";
        Place(m.band.rectTransform, new Vector2(m.left, -top), new Vector2(m.width, 44f));

        // Rounded track (the card sprite as a mask) with the green-to-red gradient inside it.
        var track = Box(parent, name, Vector2.zero, new Vector2(m.width, 30f), Color.white);
        track.anchorMin = track.anchorMax = track.pivot = new Vector2(0f, 1f);
        track.anchoredPosition = new Vector2(m.left, -(top + 52f));
        track.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var bar = new GameObject("Gradient", typeof(RectTransform)).AddComponent<GradientBarGraphic>();
        bar.transform.SetParent(track, false);
        bar.raycastTarget = false;
        Fill(bar.rectTransform);

        var endsLeft = Text(parent, low, 22f, Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(endsLeft.rectTransform, new Vector2(m.left, -(top + 86f)), new Vector2(m.width, 28f));
        var endsRight = Text(parent, high, 22f, Muted, FontStyles.Normal, TextAlignmentOptions.TopRight);
        Place(endsRight.rectTransform, new Vector2(m.left, -(top + 86f)), new Vector2(m.width, 28f));

        // Needle: a ringed dot sitting on the bar at the current level.
        m.needleDot = new GameObject(name + "Needle", typeof(RectTransform)).AddComponent<RingDotGraphic>();
        m.needleDot.transform.SetParent(parent, false);
        m.needleDot.raycastTarget = false;
        m.needleDot.color = Color.white;
        m.needle = m.needleDot.rectTransform;
        m.needle.anchorMin = m.needle.anchorMax = new Vector2(0f, 1f);
        m.needle.pivot = new Vector2(0.5f, 0.5f);
        m.needle.sizeDelta = new Vector2(44f, 44f);
        m.needle.anchoredPosition = new Vector2(m.left, -(top + 67f));
        m.needle.gameObject.SetActive(false);
        return m;
    }

    static void SetMeter(Meter m, PopulationHistory.Sample s, string key, System.Func<float, (string label, Color color)> band)
    {
        if (m == null) return;
        bool has = s != null && s.Has(key);
        m.needle.gameObject.SetActive(has);
        if (!has)
        {
            bool noneLeft = s != null && s.Has(FoodWater.Animals) && s.Get(FoodWater.Animals) < 0.5f;
            m.band.text = noneLeft ? "<color=#73808A>No animals left</color>" : "<color=#73808A>Waiting for the animals...</color>";
            return;
        }
        float level = Mathf.Clamp(s.Get(key), 0f, 100f);
        var (label, color) = band(level);
        m.band.text = $"<color=#{Hex(color)}>{label}</color>  <color=#73808A>{level:0}%</color>";
        m.needleDot.ring = color;
        m.needleDot.SetVerticesDirty();
        var pos = m.needle.anchoredPosition;
        m.needle.anchoredPosition = new Vector2(m.left + m.width * level / 100f, pos.y);
    }

    static void SetTile(TextMeshProUGUI value, TextMeshProUGUI foot, PopulationHistory.Sample s, string perDayKey, string totalKey, string verb)
    {
        if (value == null) return;
        if (s == null || !s.Has(perDayKey)) { value.text = "-"; foot.text = ""; return; }
        value.text = $"{s.Get(perDayKey):0}%";
        float total = s.Get(totalKey);
        foot.text = $"{FoodWater.Amount(total)} full {(verb == "eaten" ? "patches" : "pools")} {verb} so far";
    }

    void RefreshFoodWidgets(PopulationHistory.Sample latest)
    {
        if (foodWidgets == null) return;
        SetTile(grassTileValue, grassTileFoot, latest, FoodWater.GrassEaten, FoodWater.GrassEatenTotal, "eaten");
        SetTile(waterTileValue, waterTileFoot, latest, FoodWater.WaterDrunk, FoodWater.WaterDrunkTotal, "drunk");
        SetMeter(famineMeter, latest, FoodWater.Famine, FoodWater.FamineBand);
        SetMeter(thirstMeter, latest, FoodWater.Thirst, FoodWater.ThirstBand);
    }
    // [AI-GENERATED END]

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

    IReadOnlyList<PopulationGraph.Marker> NoteMarkers()
    {
        // Notes come from both views' lines, so they stay the same whichever view is open, and "the grass ran
        // out" can point at the Blue Sheep line (and the famine level at the grass).
        var noteSeries = FieldJournalObservations.FoodSeries != null ? FieldJournalObservations.AllSeries : (IReadOnlyList<PopulationSeries>)graph.Series;
        var all = ObservationNotes.Find(graph.History, noteSeries, FieldJournalObservations.CasesFoundInShownRun);
        // Food & Water has the tiles and meters above the text: the newest few notes keep it big enough to read.
        int max = foodView ? FoodViewNotes : ObservationNotes.MaxShown;
        shownNotes = all.Count > max ? all.GetRange(all.Count - max, max) : all;
        var markers = new List<PopulationGraph.Marker>(shownNotes.Count);
        for (int i = 0; i < shownNotes.Count; i++)
        {
            // Notes on the same day share one pin ("1,2") instead of printing over each other.
            int last = markers.Count - 1;
            if (last >= 0 && markers[last].Index == shownNotes[i].Index)
            {
                var m = markers[last];
                m.Label += $",{i + 1}";
                markers[last] = m;
                continue;
            }
            markers.Add(new PopulationGraph.Marker { Index = shownNotes[i].Index, Label = (i + 1).ToString(), Color = shownNotes[i].Color });
        }
        return markers;
    }

    void RefreshSummary()
    {
        var h = graph.History;
        bool live = FieldJournalObservations.IsLive;
        RefreshFoodWidgets(h != null && h.Count > 0 ? h.Latest : null);
        if (h == null || h.Count == 0)
        {
            obsSub.text = foodView ? "How much food and water there is, and how hungry the animals are." : "How each animal's number changes over time.";
            summary.text = "<b>Nothing observed yet</b>\n\nStart an exploration and the journal records how many animals there are every day, "
                           + "how much grass and water they use, then writes down what it notices.";
            return;
        }
        var first = h[0];
        var last = h.Latest;
        string day = graph.TimeLabel(last.Tick);
        string mode = FieldJournalObservations.WasGuided ? "Guided Exploration" : "Free Exploration";
        obsSub.text = (live ? $"This {mode} run so far." : $"Your last {mode} run, up to {day}.")
                      + " The numbers on the graph match the notes. Tap a name to show or hide its line.";

        // Right now: one line per animal (and per resource line that is switched on).
        // A long or fast run fills the history and its oldest days are dropped: compare with the first day still kept.
        string since = h.Count >= h.Capacity ? $"since {graph.TimeLabel(first.Tick)}" : "since the start";
        var sb = new StringBuilder();
        sb.Append("<b>").Append(live ? "Right now" : "At the end").Append(" · ").Append(day).Append("</b>\n");
        if (foodView) AppendFoodNow(sb, h, last, since);
        else
            foreach (var s in graph.Series)
            {
                if (s.IsResource && !s.Visible) continue;
                float now = last.Get(s.Key), change = now - first.Get(s.Key);
                string trend = Mathf.Abs(change) < 0.5f ? $"no change {since}"
                    : change > 0 ? $"up {change:0} {since}" : $"down {-change:0} {since}";
                sb.Append("<color=#").Append(Hex(s.Color)).Append("><b>").Append(s.Label).Append("</b></color>  ")
                  .Append("<b>").Append(now.ToString("0")).Append("</b>  <color=#73808A>").Append(trend).Append("</color>\n");
            }

        // What we noticed: the numbered notes, oldest first, same numbers as the pins on the graph.
        sb.Append("\n<b>What we noticed</b>\n");
        if (shownNotes.Count == 0)
            sb.Append("<color=#73808A>Nothing big yet. ")
              .Append(live ? "Keep the simulation running: notes appear here when an animal's number changes a lot."
                           : "Nothing changed much in that run.")
              .Append("</color>\n");
        for (int i = 0; i < shownNotes.Count; i++)
        {
            var note = shownNotes[i];
            sb.Append("<color=#").Append(Hex(note.Color)).Append("><b>").Append(i + 1).Append("</b></color>  ")
              .Append("<color=#73808A>").Append(graph.TimeLabel(note.Tick)).Append(" · </color>")
              .Append(note.Text).Append('\n');
        }

        // In Free Exploration the graph is how cases are found: show what to watch for next.
        var clue = InFreeRun && !foodView ? NextClue() : null;   // the clue is on the Animals view
        if (clue != null) sb.Append("\n<b>Clue to watch for</b>\n").Append(clue).Append('\n');
        summary.text = sb.ToString().TrimEnd();
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

        var holder = new GameObject("RunGraph", typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(detail, false);
        Place(holder, new Vector2(30f, -120f), new Vector2(920f, 380f));
        runGraph = PopulationGraph.Create(holder, subTemplate != null ? subTemplate.font : null);
        runGraph.labelSize = 26f;
        runGraph.textColor = Body;
        runGraph.pillSprite = RoundedSprite.Get();
        runGraph.emptyMessage = "This run has no saved days.";

        runBody = Text(detail, "", 36f, Body, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(runBody.rectTransform, new Vector2(56f, -510f), new Vector2(868f, 270f));

        starsHint = Text(detail,
            $"<b>How to earn stars</b>\n" +
            $"1 star: last {RunRewards.SeasonDays} days  ·  2 stars: both animals still alive at the end  ·  3 stars: last all {RunRewards.FullRunDays} days",
            28f, Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(starsHint.rectTransform, new Vector2(56f, -790f), new Vector2(868f, 80f));
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
            runGraph.gameObject.SetActive(false);
            return;
        }
        runGraph.gameObject.SetActive(true);
        var r = pastRuns[selectedRun];
        var before = selectedRun + 1 < pastRuns.Count ? pastRuns[selectedRun + 1] : null;   // the run just before this one

        runTitle.text = $"{RunDate(r)} · {r.mode}";
        DrawRunGraph(r);

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

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): the saved run on the same live graph as Observations, so it
    // draws itself in, can be explored with a finger, and each line can be shown / hidden with its name chip.
    void DrawRunGraph(MyRuns r)
    {
        var (history, days) = RunHistory(r);
        runGraph.Bind(history, new[]
        {
            new PopulationSeries { Key = "Trees", Label = "Trees", Color = Green, Visible = true, IsResource = true },
            new PopulationSeries { Key = "BlueSheep", Label = "Blue Sheep", Color = Blue, Visible = true },
            new PopulationSeries { Key = "SnowLeopard", Label = "Snow Leopards", Color = Orange, Visible = true },
        }, days);
    }

    /// <summary>A saved run's numbers as a history the graph can draw (one sample per saved point).</summary>
    public static (PopulationHistory history, Dictionary<int, int> days) RunHistory(MyRuns r)
    {
        int n = Mathf.Max(r.sheep?.Count ?? 0, Mathf.Max(r.leopard?.Count ?? 0, r.trees?.Count ?? 0));
        var history = new PopulationHistory(Mathf.Max(1, n));
        var days = new Dictionary<int, int>();
        static int At(List<int> list, int i) => list != null && list.Count > 0 ? list[Mathf.Min(i, list.Count - 1)] : 0;
        for (int i = 0; i < n; i++)
        {
            int tick = r.t != null && i < r.t.Count ? r.t[i] : i;
            if (i > 0 && tick <= history[history.Count - 1].Tick) tick = history[history.Count - 1].Tick + 1;   // ticks must grow
            history.Add(tick, new Dictionary<string, float> { ["BlueSheep"] = At(r.sheep, i), ["SnowLeopard"] = At(r.leopard, i), ["Trees"] = At(r.trees, i) });
            if (r.days != null && i < r.days.Count) days[tick] = r.days[i];
        }
        return (history, days);
    }

    // The clue of the first case not found yet (the one selected on the Investigations page if it is locked).
    string NextClue()
    {
        if (cases == null || cases.Cases.Count == 0) return null;
        if (selected >= 0 && selected < cases.Cases.Count && !cases.IsUnlocked(cases.Cases[selected]))
            return cases.Cases[selected].hint;
        foreach (var c in cases.Cases)
            if (!cases.IsUnlocked(c) && !string.IsNullOrWhiteSpace(c.hint)) return c.hint;
        return null;
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): Food & Water "right now": grass and water left with their
    // trend, and a warning line when animals are starving or have no water right now.
    void AppendFoodNow(StringBuilder sb, PopulationHistory h, PopulationHistory.Sample last, string since)
    {
        foreach (var s in FieldJournalObservations.FoodSeries)
        {
            if (!s.IsResource || !last.Has(s.Key)) continue;
            int from = 0;
            while (from < h.Count && !h[from].Has(s.Key)) from++;
            float now = last.Get(s.Key), change = now - h[from].Get(s.Key);
            string trend = Mathf.Abs(change) < 0.5f ? $"no change {since}"
                : change > 0 ? $"up {change:0}% {since}" : $"down {-change:0}% {since}";
            sb.Append("<color=#").Append(Hex(s.Color)).Append("><b>").Append(s.Label).Append(" left</b></color>  ")
              .Append("<b>").Append(now.ToString("0")).Append("%</b>  <color=#73808A>").Append(trend).Append("</color>\n");
        }
        int starving = Mathf.RoundToInt(last.Get(FoodWater.Starving)), parched = Mathf.RoundToInt(last.Get(FoodWater.Dehydrated));
        string warn = Hex(FoodWater.FamineColor);
        if (starving > 0) sb.Append("<color=#").Append(warn).Append("><b>").Append(starving).Append(starving == 1 ? " animal is" : " animals are").Append(" starving right now!</b></color>\n");
        if (parched > 0) sb.Append("<color=#").Append(warn).Append("><b>").Append(parched).Append(parched == 1 ? " animal is" : " animals are").Append(" out of water and getting weak!</b></color>\n");
        if (last.Has(FoodWater.Animals) && last.Get(FoodWater.Animals) < 0.5f) sb.Append("<color=#73808A>No animals are left to eat or drink.</color>\n");
    }

    static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

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
