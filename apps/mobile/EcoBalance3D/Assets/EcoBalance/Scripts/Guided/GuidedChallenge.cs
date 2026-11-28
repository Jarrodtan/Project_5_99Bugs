// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Guided Exploration (Shumin's research, "Guided Exploration" tab): a 20-simulated-day survival challenge
/// on a fixed ecosystem (10 Blue Sheep, 2 Snow Leopards). Events pause the simulation and offer three
/// choices that add temporary buffs or debuffs (<see cref="GuidedEffects"/>); the ending is chosen from the
/// ecosystem's real state at the end of Day 20, or early if both species die out. Endings are kept in an
/// Ending Gallery across restarts. Starts on its own when the simulation scene opens in Guided mode.
/// </summary>
public partial class GuidedChallenge : MonoBehaviour
{
    public const int ChallengeDays = 20;
    const float QuietSecondsBetweenEvents = 12f;   // real seconds of watching between two popups
    public const string GalleryKey = "EcoBalance.GuidedEndings";

    // ---------- Events (GE01-GE06 core, GE07-GE08 additional) ----------

    class Choice { public string label, feedback; public Action apply; }
    class GuidedEvent
    {
        public string id, title, text;
        public bool conditionBased;      // shown before scheduled ones when both are waiting
        public Func<bool> eligible;
        public Action environment;       // applied whatever the player picks
        public Choice[] choices;
        public Action<GuidedEvent> mission;   // M3 (Shumin's event library): a mission the player plays in the world instead of choices
    }

    List<GuidedEvent> events;
    readonly HashSet<string> resolved = new HashSet<string>();

    void BuildEvents()
    {
        Choice Nature() => new Choice { label = "Let nature take its course", feedback = "You let nature take its course. Let's watch what happens!", apply = () => { } };
        events = new List<GuidedEvent>
        {
            new GuidedEvent {
                id = "GE01", title = "Dry Season",
                text = "The weather has been very dry! The animals are getting thirsty faster than usual. How should we help?",
                eligible = () => Day >= 3,
                environment = () => { GuidedEffects.Add(GuidedEffects.Kind.SheepThirst, 1.25f, 3); GuidedEffects.Add(GuidedEffects.Kind.LeopardThirst, 1.25f, 3); },
                choices = new[] {
                    new Choice { label = "Help the Blue Sheep", feedback = "The Blue Sheep won't get thirsty faster for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.SheepThirst, 0.8f, 3) },
                    new Choice { label = "Help the Snow Leopards", feedback = "The Snow Leopards won't get thirsty faster for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.LeopardThirst, 0.8f, 3) },
                    Nature() } },
            new GuidedEvent {
                id = "GE02", title = "Overgrazed Grassland", conditionBased = true,
                text = "The Blue Sheep have eaten a lot of grass! There isn't much left for them to eat. What should we do?",
                eligible = () => Vegetation <= 30f && Sheep > 0,
                choices = new[] {
                    new Choice { label = "Help the grass recover", feedback = "Grass grows back 50% faster for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 1.5f, 3) },
                    new Choice { label = "Give the grass a break", feedback = "Each bite takes 40% less grass for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.Graze, 0.6f, 3) },
                    Nature() } },
            new GuidedEvent {
                id = "GE03", title = "Growing Herd",
                text = "The Blue Sheep herd is growing! More sheep need more grass to eat. How should we manage the growing herd?",
                eligible = () => Day >= 10 && Sheep >= 8,
                choices = new[] {
                    new Choice { label = "Help the grass grow", feedback = "Grass grows back 50% faster for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 1.5f, 3) },
                    new Choice { label = "Help the herd stay fed", feedback = "Blue Sheep get hungry 25% more slowly for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.SheepHunger, 0.75f, 3) },
                    Nature() } },
            new GuidedEvent {
                id = "GE04", title = "Harsh Winter",
                text = "The weather is getting colder! Grass is growing more slowly, and the Blue Sheep may struggle to find enough food. What should we do?",
                eligible = () => Day >= 11,
                environment = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 0.5f, 3),
                choices = new[] {
                    new Choice { label = "Help the grass grow", feedback = "Grass keeps growing at its normal speed for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 2f, 3) },
                    new Choice { label = "Help the Blue Sheep stay strong", feedback = "Blue Sheep get hungry 25% more slowly for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.SheepHunger, 0.75f, 3) },
                    Nature() } },
            new GuidedEvent {
                id = "GE05", title = "Hungry Snow Leopards", conditionBased = true,
                text = "The Snow Leopards are very hungry! They need to catch Blue Sheep to survive, but hunting also affects the herd. What should we do?",
                eligible = () => Sheep > 0 && AnyHungryLeopard(),
                choices = new[] {
                    new Choice { label = "Help the Snow Leopards hunt", feedback = "Snow Leopards catch prey more often for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, 0.15f, 3) },
                    new Choice { label = "Protect the Blue Sheep", feedback = "Blue Sheep escape more often for 3 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, -0.15f, 3) },
                    Nature() } },
            new GuidedEvent {
                id = "GE06", title = "Spring Recovery",
                text = "Spring has arrived! The highlands have a chance to recover as the weather becomes warmer. What would you like to do?",
                eligible = () => Day >= 16,
                choices = new[] {
                    new Choice { label = "Help the grass recover", feedback = "The grass grew back a little!", apply = () => { foreach (var p in ResourcePatch.All) if (p != null) p.Restore(15f); } },
                    new Choice { label = "Support the animals", feedback = "Every animal had a good meal and a drink!", apply = () => { foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught) a.Refresh(15f, 15f); } },
                    Nature() } },
            new GuidedEvent {
                id = "GE07", title = "Animal Illness",
                text = "Some animals are becoming sick! They may lose their strength if they don't recover. What should we do?",
                eligible = () => Day >= 12 && (Sheep > 0 || Leopards > 0),
                environment = () => { foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught) a.ChangeHealth(-20f); },
                choices = new[] {
                    new Choice { label = "Help the Blue Sheep recover", feedback = "The Blue Sheep got some of their strength back.", apply = () => { foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught && !a.IsPredatorSpecies) a.ChangeHealth(15f); } },
                    new Choice { label = "Help the Snow Leopards recover", feedback = "The Snow Leopards got some of their strength back.", apply = () => { foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught && a.IsPredatorSpecies) a.ChangeHealth(15f); } },
                    Nature() } },
            new GuidedEvent {
                id = "GE08", title = "Heavy Rainfall",
                text = "Heavy rain has arrived in the highlands! The grass may grow faster with more water. What should we do?",
                eligible = () => Day >= 18,
                environment = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 1.25f, 2),
                choices = new[] {
                    new Choice { label = "Encourage grass recovery", feedback = "Grass grows back even faster for 2 days.", apply = () => GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 1.2f, 2) },
                    new Choice { label = "Help the animals recover", feedback = "Every animal had a big drink!", apply = () => { foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught) a.Refresh(0f, 10f); } },
                    Nature() } },
        };
        AddMissionEvents();   // BS01, BS02, BS03-F (GuidedMissions.cs)
    }

    // ---------- Endings: GuidedEndings.cs (doc text, END01-END05, classification) ----------

    int PickEnding() => GuidedEndings.Classify(Sheep, Leopards, Vegetation);

    /// <summary>Miloy or Snowy (the focal animal of the chosen story).</summary>
    public static string FocalName
    {
        get
        {
            var fm = FamilyManager.Instance;
            bool sheep = FamilyManager.SelectedStory == FamilyStory.BlueSheep;
            return fm != null ? (sheep ? fm.miloName : fm.snowyName) : (sheep ? "Miloy" : "Snowy");
        }
    }

    /// <summary>
    /// The ending screen's line about the focal animal (doc, Ending Presentation: acknowledge whether Milo or Snowy
    /// survived until the last day or died earlier). Kind words, never blaming the player.
    /// </summary>
    public static string FocalOutcome(string name, bool died, int day, FamilyDeathCause cause, int lastDay)
    {
        if (!died) return $"{name} made it through all {lastDay} days!";
        return cause switch
        {
            FamilyDeathCause.Caught => $"{name} was caught by a Snow Leopard on Day {day}. The story ended early.",
            FamilyDeathCause.Hunger => $"{name} could not find enough food and died on Day {day}. The story ended early.",
            FamilyDeathCause.Thirst => $"{name} could not find enough water and died on Day {day}. The story ended early.",
            _ => $"{name} did not survive past Day {day}. The story ended early.",
        };
    }

    // ---------- State ----------

    ClockService clock;
    SimulationSpeedController speed;
    int lastDay = -1;
    bool started, finished, showing;

    /// <summary>The challenge card or an event card is open (the mascot's hints and Help wait).</summary>
    public static bool PopupOpen { get; private set; }
    /// <summary>Guided run loaded but Start not pressed yet (intro card coming or showing).</summary>
    public static bool IntroPending { get; private set; }
    bool weatherShown;
    float quietUntil;

    int Day => clock != null ? clock.Day : 1;
    static float Vegetation => ResourcePatch.VegetationPercent();
    static int Count(bool predator)
    {
        int n = 0;
        foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught && a.IsPredatorSpecies == predator) n++;
        return n;
    }
    static int Sheep => Count(false);
    static int Leopards => Count(true);
    static bool AnyHungryLeopard()
    {
        foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught && a.IsPredatorSpecies && a.curr_hunger <= 30f) return true;
        return false;
    }

    // ---------- Startup ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != ScreenFlow.Simulation) return;
        PopupOpen = false;   // a card left open by the last Guided run (Leave from its intro) must not silence the next run's mascot
        SeasonWeather.ForcedStartSeason = ScreenFlow.GuidedMode ? SeasonWeather.Season.Summer : (SeasonWeather.Season?)null;
        GuidedEffects.Clear();
        if (ScreenFlow.GuidedMode) new GameObject("[GuidedChallenge]").AddComponent<GuidedChallenge>();
    }

    void Awake()
    {
        PopupOpen = false;   // statics survive Play without a domain reload
        IntroPending = true;
        ResetChapters();
        instance = this;
        // Real animals and real deaths (Events mode), one model per animal so the counts match what children see.
        var sim = FindFirstObjectByType<EcosystemSimulator>();
        if (sim != null) sim.mode = EcosystemSimulator.SimulationMode.Events;
        foreach (var sp in FindObjectsByType<CreatureSpawner>(FindObjectsSortMode.None)) sp.populationPerVisual = 1f;
        BuildEvents();
    }

    void OnDestroy()   // leaving the run: no Guided card is open and nothing waits for Start any more
    {
        IntroPending = false;
        if (instance == this) PopupOpen = false;
    }

    void Start()
    {
        clock = FindAnyObjectByType<ClockService>();
        speed = FindAnyObjectByType<SimulationSpeedController>();
        missionEvents = MissionEvents();   // the chosen story's wording (Miloy or Snowy)
        BuildUI();
    }

    void Update()
    {
        if (finished) return;
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): until Start, keep the live weather box, name tags, ring and
        // camera buttons hidden: they flashed on load (also while the animals are still spawning) before the intro card.
        IntroPending = !introDone;
        // 28 Nov (Shumin): the weather box was drawn over event cards and popups: hide it while any card is up too.
        bool weatherWanted = introDone && !showing && !chapterOpen && !missionCardOpen && !ScreenFlow.LeaveOpen;   // no weather box behind any card or the Leave box
        if (!weatherWanted) { SimHud.SetWeatherVisible(false); weatherShown = false; }   // every frame: it can appear a moment later
        else if (!weatherShown) { SimHud.SetWeatherVisible(true); weatherShown = true; }
        int day = Day;
        if (day != lastDay) { lastDay = day; GuidedEffects.NewDay(day); }
        if (!started)   // wait for the animals to spawn
        {
            started = Sheep > 0 || Leopards > 0;
            if (started) ApplyResume();   // SCRUM-375: Continue a saved story
            UpdatePill();
            return;
        }
        UpdatePill();

        if (FamilyManager.MainAnimalDead) { Finish(); return; }     // doc: Miloy / Snowy died = early ending, evaluated now
        if (Sheep == 0 && Leopards == 0) { Finish(); return; }       // early collapse
        if (day > ChallengeDays) { Finish(); return; }               // end of Day 20

        if (!string.IsNullOrEmpty(StoryOverReason)) { Finish(); return; }   // SCRUM-374: e.g. main animal died

        // The run keeps going under the Field Journal: the intro, chapter cards, fires and decisions wait until
        // the player is back to see them (an ending still shows: Finish closes the journal first).
        if (ScreenFlow.JournalOpen) return;

        if (!introDone) { ShowIntro(); return; }
        TrackMissionSteps(day);                                      // Badges and Achievement: Day 1-3 missions
        if (UpdateMission()) return;                                 // M3: a BS/SL event mission is running (GuidedMissions.cs)
        if (Mascot.Showing) return;                                  // 28 Nov: let the mascot finish its line (e.g. the intro) before a new card
        if (UpdateChapters(day)) return;                             // SCRUM-374: new season = chapter card
        if (UpdateMissionCards()) return;                            // a mission decision, badge or question
        if (!showing) UpdateFire(day);                               // SCRUM-390: grass fire player action
        if (!showing) UpdatePlanting();                              // player action: plant grass on brown patches
        // The run keeps going under the Field Journal: decisions wait until the player is back to see them.
        if (showing || Time.unscaledTime < quietUntil || ScreenFlow.JournalOpen) return;
        var next = NextEvent(true) ?? NextEvent(false);              // condition-based first, then scheduled
        if (next != null) Show(next);
    }

    GuidedEvent NextEvent(bool conditionBased)
    {
        foreach (var e in events)
            if (e.conditionBased == conditionBased && !resolved.Contains(e.id) && e.eligible()) return e;
        return null;
    }

    // ---------- Decision popup ----------

    void Show(GuidedEvent e)
    {
        showing = true;
        PopupOpen = true;
        if (speed != null) speed.Pause();
        HideSideUiNow();
        if (e.mission != null) { Mascot.Hide(); e.mission(e); return; }   // BS missions: their own cards (GuidedMissions.cs)
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): Shumin's event card has its own mascot beside it (GE-M05);
        // the HUD mascot would sit under the card, so it closes. The code-built popup keeps the HUD line.
        if (FindShuminUI()) { Mascot.Hide(); ShowShumin(e); return; }
        Mascot.GuidedEvent(false);   // GE-M05
        popupTitle.text = e.title;
        popupText.text = e.text;
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool has = i < e.choices.Length;
            choiceButtons[i].gameObject.SetActive(has);
            if (!has) continue;
            var c = e.choices[i];
            choiceLabels[i].text = c.label;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => Choose(e, c));
        }
        popup.SetActive(true);
    }

    /// <param name="decisionSaid">The feedback card's mascot already said GE-M06: don't repeat it on the HUD.</param>
    void Choose(GuidedEvent e, Choice c, bool decisionSaid = false)
    {
        e.environment?.Invoke();
        c.apply();
        resolved.Add(e.id);
        chosenLabels[e.id] = c.label;   // Mission 2's outcome card says what was chosen
        if (popup != null) popup.SetActive(false);
        showing = false;
        PopupOpen = false;
        if (!decisionSaid) Mascot.GuidedEvent(true);    // GE-M06
        quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
        if (speed != null) speed.Play();
        DramaDirector.Announce(c.feedback);
    }

    // ---------- Ending ----------

    public void FinishForPresentation()
    {
        Finish();
    }

    void Finish()
    {
        finished = true;
        HideMissionUI();
        DeleteSave();   // SCRUM-375: an ended story can't be continued
        if (speed != null) speed.Pause();
        ScreenFlow.CloseJournalOverlay();   // the ending would be hidden under the journal
        UnlockFinishedMissions();   // a badge earned just before the end is not lost
        int i = PickEnding();
        bool isNew = GuidedEndings.Unlock(i);
        var result = new GuidedScreens.Result {
            ending = i, isNew = isNew, day = Mathf.Min(Day, ChallengeDays),
            sheep = Sheep, leopards = Leopards, vegetation = Vegetation,
            focal = FocalLine(), focalDied = FamilyManager.MainAnimalDead, newBadges = newBadgesThisRun };
        LastResult = result;

        // Shumin's Ending screen when it's in the build; otherwise the card below.
        if (GuidedScreens.ShowEnding(result)) return;

        var e = GuidedEndings.All[i];
        endTitle.text = e.title;
        endText.text = $"<b>{result.focal}</b>\n\n{e.description}\n\n<i>{e.takeaway}</i>\n\n<size=80%>Blue Sheep {Sheep} · Snow Leopards {Leopards} · Grass {Vegetation:0}%</size>"
                     + (isNew ? "\n<color=#E0A000>New ending unlocked!</color>" : "");
        endPanel.SetActive(true);
    }

    /// <summary>The last ending this session (tests read it).</summary>
    public static GuidedScreens.Result LastResult { get; private set; }

    string FocalLine()
    {
        bool died = FamilyManager.MainAnimalDead;
        int day = Mathf.Min(Day, ChallengeDays);
        var cause = FamilyDeathCause.Other;
        foreach (var loss in FamilyManager.Lost)
            if (loss.Role == FamilyRole.Main) { day = loss.Day; cause = loss.Cause; }
        return FocalOutcome(FocalName, died, day, cause, ChallengeDays);
    }

    void ShowGallery()
    {
        var sb = new System.Text.StringBuilder();
        foreach (int i in GuidedEndings.GalleryOrder)
        {
            var e = GuidedEndings.All[i];
            sb.Append(GuidedEndings.IsUnlocked(i) ? $"<b>{e.title}</b>\n<size=80%>{e.card}</size>\n\n" : "<b>???</b>\n<size=80%>Not discovered yet. Try different choices!</size>\n\n");
        }
        galleryText.text = GuidedEndings.Progress() + "\n\n" + sb;
        gallery.SetActive(true);
    }

    // ---------- Shumin's Guided Exploration HUD (GuidedExploration.unity), found by object name ----------

    Transform shuminRoot, introPanel, eventPanel, eventCard, feedbackCard;
    Transform introMascot, eventMascot, feedbackMascot;   // Shumin's mascots beside the intro, event and feedback cards
    bool introDone, introShown;

    // 28 Nov (Shumin): her Guided texts say "Miloy and his herd"; in Snowy's story use Snowy's name instead.
    static void UseStoryNames(Transform root)
    {
        if (root == null || FamilyManager.SelectedStory != FamilyStory.SnowLeopard) return;
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(t.text) || !t.text.Contains("Milo")) continue;
            t.text = t.text.Replace("Miloy and his herd", "Snowy and her family").Replace("Miloy's", "Snowy's")
                           .Replace("Miloy", "Snowy").Replace("Milo and his family", "Snowy and her family").Replace("Milo", "Snowy");
        }
    }

    // 28 Nov (Shumin): a dark see-through overlay behind the event cards and completion popup, so the busy world
    // behind doesn't distract. It is the event panel's first child, so it sits under every card in it.
    static void AddDimOverlay(Transform panel)
    {
        if (panel == null || panel.Find("[Dim Overlay]") != null) return;
        var dim = new GameObject("[Dim Overlay]", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)dim.transform;
        rt.SetParent(panel, false);
        rt.SetAsFirstSibling();
        // Cover the whole screen even if the panel itself is smaller.
        var canvas = panel.GetComponentInParent<Canvas>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (canvas != null && panel is RectTransform prt && prt.rect.width < ((RectTransform)canvas.transform).rect.width - 1f)
            rt.sizeDelta = ((RectTransform)canvas.transform).rect.size * 1.5f;
        var img = dim.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);
        img.raycastTarget = true;   // taps on the world behind are blocked while a card is up
    }

    bool FindShuminUI()
    {
        if (shuminRoot != null) return eventCard != null;
        var scene = SceneManager.GetSceneByName(SimHud.GuidedSceneName);
        if (!scene.isLoaded) return false;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<Canvas>() != null) shuminRoot = root.transform;
        if (shuminRoot == null) return false;
        introPanel = Find(shuminRoot, "IntroPanel");
        eventPanel = Find(shuminRoot, "EventPanel");
        // Shumin's 27 Nov UI renamed the cards (Decision / Investigation event, Completion popup); the old
        // names stay as a fallback so an older GuidedExploration scene still works.
        eventCard = Find(eventPanel, "EventCard-DecisionMission") ?? Find(eventPanel, "EventCard");
        feedbackCard = Find(eventPanel, "OptionFeedbackCard") ?? Find(eventPanel, "FeedbackCard");
        introMascot = Find(shuminRoot, "IntroMascotPanel");
        eventMascot = Find(eventPanel, "MascotPanel");
        feedbackMascot = Find(eventPanel, "MascotPanel(CompletionVer)") ?? Find(eventPanel, "MascotPanel(FeedbackVer)");
        // Not used yet (no investigation events in the event list): keep them hidden.
        foreach (var unused in new[] { "EventCard-InvestigationMission", "InvestigationCompleteCard" })
        {
            var t = Find(eventPanel, unused);
            if (t != null) t.gameObject.SetActive(false);
        }
        // The intro mascot panel is a see-through, screen-sized image drawn over the intro card: it ate every tap
        // (Start could not be pressed). Only its bubble and buttons should catch taps.
        foreach (var m in new[] { introMascot, eventMascot, feedbackMascot })
        {
            if (m == null) continue;
            if (m.TryGetComponent(out Image mascotBg)) mascotBg.raycastTarget = false;
            var mascotArt = Find(m, "Mascot");
            if (mascotArt != null && mascotArt.TryGetComponent(out Image art)) art.raycastTarget = false;
        }
        UseStoryNames(shuminRoot);
        AddDimOverlay(eventPanel);
        if (eventPanel != null) eventPanel.gameObject.SetActive(false);
        if (introPanel != null) introPanel.gameObject.SetActive(false);
        if (introMascot != null) introMascot.gameObject.SetActive(false);
        OnTop(introPanel);
        OnTop(introMascot);   // after IntroPanel in the hierarchy, so it draws over the card's edge
        OnTop(eventPanel);
        return eventCard != null;
    }

    // The cards are part of the HUD canvas, so the season banner and the live weather line drew over
    // them. Their own canvas puts them above everything in the run (the journal overlay is 90).
    static void OnTop(Transform panel)
    {
        if (panel == null || panel.GetComponent<Canvas>() != null) return;
        var c = panel.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 80;
        panel.gameObject.AddComponent<GraphicRaycaster>();
    }

    // The mock-up's day text is small and pale grey over the world: make it a readable dark pill.
    bool dayPanelStyled;
    void StyleDayPanel()
    {
        dayPanelStyled = true;
        DramaDirector.UpdateStatusVisibility();
        var panel = Find(shuminRoot, "DayPanel");
        if (panel == null) return;
        if (!panel.TryGetComponent(out Image bg)) bg = panel.gameObject.AddComponent<Image>();
        ScreenFlow.CreamPanel(bg);   // M3: same cream pill as the rest of Shu Min's HUD (was a dark pill)
        bg.raycastTarget = false;
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): "Day 1 / 20  Summer · Snow · -8°C" was squeezed to the
        // smallest size in the 467-wide pill; widen it into the free space before Field Journal (keeps its centre).
        // Grows leftwards only: its right edge stays a clear gap before the Field Journal button.
        if (panel is RectTransform prt && prt.sizeDelta.x < 620f)
        {
            float right = prt.anchoredPosition.x + (1f - prt.pivot.x) * prt.sizeDelta.x;
            if (Find(shuminRoot, "FieldJournalBtn") is RectTransform fj && fj.parent == prt.parent)
                right = Mathf.Min(right, fj.anchoredPosition.x - fj.pivot.x * fj.sizeDelta.x - 24f);
            prt.pivot = new Vector2(1f, prt.pivot.y);
            prt.sizeDelta = new Vector2(620f, prt.sizeDelta.y);
            prt.anchoredPosition = new Vector2(right, prt.anchoredPosition.y);
        }
        foreach (var name in new[] { "SeasonTxt", "Divider" })
        {
            var hide = Find(panel, name);
            if (hide != null) hide.gameObject.SetActive(false);
        }
        var day = Find(panel, "DayCounterTxt");
        if (day != null && day.TryGetComponent(out TextMeshProUGUI t))
        {
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 4f); rt.offsetMax = new Vector2(-16f, -4f);
            t.color = ScreenFlow.InkColor;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMax = 30f;
            t.fontSizeMin = 22f;   // readable on a 10" tablet
        }
    }

    static Transform Find(Transform under, string name)
    {
        if (under == null) return null;
        foreach (var t in under.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim().Trim('\'').Trim() == name) return t;
        return null;
    }

    static void SetText(Transform under, string name, string text)
    {
        var t = Find(under, name);
        if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = text;
    }

    static void OnClick(Transform under, string name, UnityEngine.Events.UnityAction action)
    {
        var t = Find(under, name);
        if (t == null) return;
        if (!t.TryGetComponent(out Button b)) b = t.gameObject.AddComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    // Shumin's intro card (wireframe 17): the run waits, paused, until Start. No Guided HUD after a few
    // seconds = skip the intro (the code-built popups still work).
    void ShowIntro()
    {
        if (introShown) return;
        if (!FindShuminUI())
        {
            if (Time.timeSinceLevelLoad > 5f) introDone = true;
            return;
        }
        introShown = true;
        if (introPanel == null) { introDone = true; return; }
        if (speed != null) speed.Pause();
        int card = 0;
        foreach (var t in introPanel.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("AnimalCard"))
            {
                SimHud.SetIcon(Find(t, "AnimalImage"), SimHud.AnimalIcon(card == 1));   // [AI-GENERATED] Claude Code (Claude Opus 5.5): the picture spot was empty
                SetText(t, "AmountTxt", $"x{(card++ == 0 ? Sheep : Leopards)}");
            }
        SetText(Find(introPanel, "GrassCard"), "ValueTxt", $"{Vegetation:0}%");
        introPanel.gameObject.SetActive(true);
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): only the intro card can be used until Start: dim and
        // block the run behind it (Back still works), and no mascot until the run starts.
        Mascot.Hide();
        ShowIntroMascot();   // the intro card's own mascot on the right stays
        var blocker = IntroBlocker();
        SimHud.SetWeatherVisible(false);   // it is drawn above the card
        PopupOpen = true;
        OnClick(introPanel, "StartBtn", () =>
        {
            introPanel.gameObject.SetActive(false);
            if (blocker != null) Destroy(blocker);
            SimHud.SetWeatherVisible(true);
            SetTerrainEditVisible(true);
            if (introMascot != null) { Spotlight.Hide(introMascot); introMascot.gameObject.SetActive(false); }
            introDone = true;
            PopupOpen = false;
            Mascot.PlayGuidedIntro();   // GE-M01..03
            quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
            if (speed != null) speed.Play();
        });
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5)
    // Full-screen dimmer just under the intro card (80) that eats every tap. The HUD's Back button is lifted
    // above it so a child can still leave before pressing Start.
    GameObject IntroBlocker()
    {
        // Plain dimmer in the HUD canvas, one sibling under the intro panel: the card, its mascot, Settings
        // and Back (later siblings / own canvases) stay tappable, everything else in the HUD does not.
        // The intro panel itself is screen-sized: only its card should catch taps, or it hides Back and the mascot.
        if (introPanel.TryGetComponent(out Image panelBg)) panelBg.raycastTarget = false;
        var go = new GameObject("IntroBlocker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(introPanel.parent, false);
        go.transform.SetSiblingIndex(introPanel.GetSiblingIndex());
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-4000f, -4000f); rt.offsetMax = new Vector2(4000f, 4000f);   // whole screen, whatever the parent's size
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
        SetTerrainEditVisible(false);   // its own canvas sits above the HUD
        return go;
    }

    static void SetTerrainEditVisible(bool on)
    {
        var c = GameObject.Find("TerrainEditCanvas");
        if (c != null && c.TryGetComponent(out Canvas canvas)) canvas.enabled = on;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED] Claude Code (Claude Opus 5.5)
    // Guided Exploration Introduction screen (doc "Mascot Guidance System"): one friendly line beside the
    // challenge card, every time the card opens. Got It closes only the mascot; Start closes both.
    void ShowIntroMascot()
    {
        if (introMascot == null) return;
        SetText(introMascot, "DynamicTxt", Mascot.GuidedIntroScreenLine);
        Mascot.Speak(Mascot.GuidedIntroScreenLine);
        var bubble = Find(introMascot, "SpeechBubble") as RectTransform;
        if (bubble != null && Find(bubble, "GotItBtn") == null)
        {
            // The panel has no button: borrow the HUD mascot's Next button (same art) and put it under the bubble.
            var hudNext = Find(Find(shuminRoot, "MascotPanel"), "NextBtn");
            if (hudNext != null)
            {
                var btn = (RectTransform)Instantiate(hudNext.gameObject, bubble, false).transform;
                btn.name = "GotItBtn";
                // The button's top-right corner sits just under the bubble's bottom-right corner.
                // Shumin's 27 Nov bubble is turned 180° to mirror its tail: its left edge is then the
                // screen's right, so pick that corner and keep the button itself unturned (readable).
                bool flipped = bubble.right.x < 0f;
                btn.anchorMin = btn.anchorMax = new Vector2(flipped ? 0f : 1f, 0f);
                btn.pivot = new Vector2(1f, 1f);
                btn.rotation = Quaternion.identity;
                btn.anchoredPosition = new Vector2(flipped ? 10f : -10f, -8f);
                foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true)) t.text = "Got It";
                btn.gameObject.SetActive(true);
            }
        }
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the card dims while the mascot speaks; Got It brings it back.
        OnClick(introMascot, "GotItBtn", () => { Mascot.StopVoice(); Spotlight.Hide(introMascot); introMascot.gameObject.SetActive(false); });
        introMascot.gameObject.SetActive(true);
        Spotlight.Show(introMascot);
    }

    // GE-M05 beside the event card, GE-M06 beside the feedback card (Shumin's two panels inside EventPanel).
    void ShowEventMascot(bool feedback)
    {
        if (eventMascot != null)
        {
            eventMascot.gameObject.SetActive(!feedback);
            if (!feedback) { SetText(eventMascot, "DynamicTxt", Mascot.EventLine); Mascot.Speak(Mascot.EventLine); }
        }
        if (feedbackMascot != null)
        {
            feedbackMascot.gameObject.SetActive(feedback);
            if (feedback)
            {
                SetText(feedbackMascot, "DynamicTxt", Mascot.DecisionLine);
                SetText(feedbackMascot, "DynamicTxt-Completion Feedback", Mascot.DecisionLine);   // Shumin's 27 Nov name
                Mascot.Speak(Mascot.DecisionLine);
            }
        }
    }

    // Event card (wireframes 18-19): pick one of three options, then Confirm; the feedback card follows.
    // 28 Nov: the decision card left a big empty band between its title and the description: move the description
    // (keeping its size) up to sit just under the title. Done once; a later card reuses the moved text.
    bool titleGapClosed;
    void CloseTitleGap(Transform title, Transform desc)
    {
        if (titleGapClosed || !(title is RectTransform t) || !(desc is RectTransform d) || d.parent == null) return;
        titleGapClosed = true;
        var tc = new Vector3[4]; var dc = new Vector3[4];
        t.GetWorldCorners(tc); d.GetWorldCorners(dc);
        var parent = (RectTransform)d.parent;
        float titleBottom = parent.InverseTransformPoint(tc[0]).y, descTop = parent.InverseTransformPoint(dc[1]).y;
        float gap = descTop - titleBottom;   // negative: the description starts below the title
        const float Margin = 24f;
        if (gap < -Margin * 2f) d.anchoredPosition += new Vector2(0f, -gap - Margin);
    }

    void ShowShumin(GuidedEvent e)
    {
        Choice picked = null;
        SetText(eventCard, "FeedbackTitleTxt", e.title);
        // 28 Nov: long titles ("Mission 1: Find What You Need") wrapped onto two lines: shrink to fit one line instead.
        var titleTf = Find(eventCard, "FeedbackTitleTxt");
        if (titleTf != null && titleTf.TryGetComponent(out TMP_Text titleTxt))
        {
            if (!titleTxt.enableAutoSizing) { titleTxt.fontSizeMax = titleTxt.fontSize; titleTxt.fontSizeMin = titleTxt.fontSize * 0.6f; }
            titleTxt.enableAutoSizing = true;
            titleTxt.textWrappingMode = TextWrappingModes.NoWrap;
        }
        SetText(eventCard, "EventDescriptionTxt", e.text);
        CloseTitleGap(titleTf, Find(eventCard, "EventDescriptionTxt"));
        var confirm = Find(eventCard, "ConfirmBtn");
        Button confirmButton = confirm != null ? confirm.GetComponent<Button>() : null;
        if (confirmButton != null) confirmButton.interactable = false;
        for (int i = 0; i < 3; i++)
        {
            var box = Find(eventCard, $"OptionBox0{i + 1}");
            if (box == null) continue;
            bool has = i < e.choices.Length;
            box.gameObject.SetActive(has);
            if (!has) continue;
            var c = e.choices[i];
            SetText(box, "Option Title", c.label);
            SetText(box, "EffectDescriptionTxt (1)", c.feedback);
            if (box.TryGetComponent(out Image boxImage)) boxImage.color = Color.white;
            SelectedDot.Set(Find(box, "SelectionBtn"), false);
            OnClick(box, "SelectionBtn", () =>
            {
                picked = c;
                for (int j = 1; j <= 3; j++)
                {
                    var other = Find(eventCard, $"OptionBox0{j}");
                    if (other != null && other.TryGetComponent(out Image img)) img.color = other == box ? new Color(0.85f, 0.95f, 0.85f) : Color.white;
                    if (other != null) SelectedDot.Set(Find(other, "SelectionBtn"), other == box);
                }
                if (confirmButton != null) confirmButton.interactable = true;
            });
        }
        OnClick(eventCard, "ConfirmBtn", () =>
        {
            if (picked == null) return;
            var chosen = picked;
            eventCard.gameObject.SetActive(false);
            if (feedbackCard == null) { eventPanel.gameObject.SetActive(false); Choose(e, chosen); return; }
            ShowEventMascot(true);   // GE-M06 beside the feedback card
            SetText(feedbackCard, "FeedbackTitleTxt", chosen.label);
            SetText(feedbackCard, "FeedbackDescriptionTxt", chosen.feedback);
            SetText(feedbackCard, "EffectDescriptionTxt (1)", chosen.feedback);
            SetText(feedbackCard, "ObservationDescriptionTxt (1)", "Watch the animals and the grass over the next few days. What changes?");
            feedbackCard.gameObject.SetActive(true);
            OnClick(feedbackCard, "ContinueBtn", () =>
            {
                feedbackCard.gameObject.SetActive(false);
                eventPanel.gameObject.SetActive(false);
                Choose(e, chosen, feedbackMascot != null);
            });
        });
        ShowEventMascot(false);   // GE-M05 beside the event card
        eventCard.gameObject.SetActive(true);
        if (feedbackCard != null) feedbackCard.gameObject.SetActive(false);
        eventPanel.gameObject.SetActive(true);
    }


    // ---------- UI (built in code, 1920x1200 reference like the wireframes) ----------

    GameObject popup, endPanel, gallery, pillBox;
    TextMeshProUGUI pill, popupTitle, popupText, endTitle, endText, galleryText;
    Button[] choiceButtons; TextMeshProUGUI[] choiceLabels;

    void UpdatePill()
    {
        if (pill != null) pill.text = $"Guided challenge · Day {Day}";   // 28 Nov (Shumin): no "/ 20", the story isn't a fixed day count
        if (FindShuminUI())   // Shumin's DayPanel: "Day 12 / 20 | Summer" in the mock-up
        {
            // One line across the whole panel: the season box alone was too narrow to read on a tablet.
            SetText(shuminRoot, "DayCounterTxt", $"<b>Day {Day}</b>   {SeasonWeather.CurrentSeason} · {SeasonWeather.Describe(SeasonWeather.CurrentWeather)} · {SeasonWeather.Temperature:0}°C");   // weather word dropped: the icon beside the pill shows it
            if (!dayPanelStyled) StyleDayPanel();
        }
        if (pillBox != null && pillBox.activeSelf == SimHud.Active) pillBox.SetActive(!SimHud.Active);   // Shumin's DayPanel shows it
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("Guided Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;   // above the HUD and the Watch banner
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Day counter, bottom-right (top is taken by Play/Pause/Reset, the banner and the season pill).
        pillBox = Box(root, "Day pill", new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(520f, 70f), new Color(0.08f, 0.1f, 0.14f, 0.8f), new Vector2(1f, 0f));
        pill = Label(pillBox.transform, "", 30f, Color.white);

        BuildMissionUI(root);   // tracker first, so every popup in this canvas draws over it

        // Decision popup.
        popup = Dim(root, "Decision");
        var card = Box(popup.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f), new Color(0.97f, 0.97f, 0.95f, 1f));
        popupTitle = Label(card.transform, "", 54f, new Color(0.12f, 0.16f, 0.2f), 60f, 40f, 620f, TextAlignmentOptions.Top);
        popupText = Label(card.transform, "", 36f, new Color(0.2f, 0.24f, 0.28f), 70f, 150f, 420f, TextAlignmentOptions.Top);
        choiceButtons = new Button[3]; choiceLabels = new TextMeshProUGUI[3];
        var colors = new[] { new Color(0.3f, 0.62f, 0.38f), new Color(0.28f, 0.5f, 0.75f), new Color(0.55f, 0.55f, 0.55f) };
        for (int i = 0; i < 3; i++)
        {
            var b = Box(card.transform, $"Choice {i + 1}", new Vector2(0.5f, 0f), new Vector2(0f, 300f - i * 125f), new Vector2(900f, 105f), colors[i], new Vector2(0.5f, 0f));
            choiceButtons[i] = b.AddComponent<Button>();
            choiceLabels[i] = Label(b.transform, "", 36f, Color.white);
        }
        popup.SetActive(false);

        BuildChapterUI(root);   // SCRUM-374
        BuildEventMissionUI(root);   // M3 event missions (BS/SL), under the ending screen

        // Ending screen.
        endPanel = Dim(root, "Ending");
        var endCard = Box(endPanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 820f), new Color(0.97f, 0.97f, 0.95f, 1f));
        endTitle = Label(endCard.transform, "", 64f, new Color(0.12f, 0.16f, 0.2f), 60f, 40f, 670f, TextAlignmentOptions.Top);
        endText = Label(endCard.transform, "", 36f, new Color(0.2f, 0.24f, 0.28f), 80f, 160f, 170f, TextAlignmentOptions.Top);
        Button(endCard.transform, "Play again", new Vector2(-370f, 50f), new Color(0.3f, 0.62f, 0.38f), ScreenFlow.RestartGuided);
        Button(endCard.transform, "Ending Gallery", new Vector2(0f, 50f), new Color(0.28f, 0.5f, 0.75f), ShowGallery);
        Button(endCard.transform, "Home", new Vector2(370f, 50f), new Color(0.55f, 0.55f, 0.55f), ScreenFlow.GoHome);
        endPanel.SetActive(false);

        // Ending Gallery.
        gallery = Dim(root, "Gallery");
        var galCard = Box(gallery.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 1000f), new Color(0.97f, 0.97f, 0.95f, 1f));
        Label(galCard.transform, "Ending Gallery", 56f, new Color(0.12f, 0.16f, 0.2f), 60f, 30f, 880f, TextAlignmentOptions.Top);
        galleryText = Label(galCard.transform, "", 32f, new Color(0.2f, 0.24f, 0.28f), 80f, 130f, 150f, TextAlignmentOptions.TopLeft);
        Button(galCard.transform, "Back", new Vector2(0f, 30f), new Color(0.55f, 0.55f, 0.55f), () => gallery.SetActive(false));
        gallery.SetActive(false);
    }

    static GameObject Dim(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);   // also blocks taps on the world behind
        return go;
    }

    static GameObject Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static void Button(Transform parent, string text, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var b = Box(parent, text, new Vector2(0.5f, 0f), pos, new Vector2(330f, 100f), color, new Vector2(0.5f, 0f));
        b.AddComponent<Button>().onClick.AddListener(onClick);
        Label(b.transform, text, 36f, Color.white);
    }

    // Text filling its parent, inset from each side, the top and the bottom; wraps and shrinks to fit.
    static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
                                 float side = 0f, float top = 0f, float bottom = 0f, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(side, bottom);
        rt.offsetMax = new Vector2(-side, -top);
        t.text = text;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = size * 0.5f;
        t.raycastTarget = false;
        return t;
    }
}
