// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - to be reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shumin's updated Blue Sheep event library (research doc, "Blue Sheep Event &amp; Decision Library"), M3 subset:
/// BS01 Miloy's First Summer (Investigation), BS02 A Season of Plenty (Decision + planting) and
/// BS03-F A Predator's Home (Investigation). The player does something in the 3D world instead of only
/// picking a button: find and tap animals, grass and water, or plant grass.
/// Days are scaled to the game's 20-day story (the doc's 96 days): BS01 Day 2, BS02 Day 3, BS03-F Day 4.
/// All four are Summer events (chapter 1, Days 1-5): they are skipped if Summer ends first (GuidedChallenge.Due).
/// </summary>
public partial class GuidedChallenge
{
    const float MissionTapRadiusPx = 110f;   // at 1200 px screen height (animals move: a bit kinder than planting)
    const float MissionSkipAfterSeconds = 90f;
    const int MissionPlantsNeeded = 3;

    class Objective
    {
        public string label, feedback;
        public bool done;
        public Func<Vector2, Camera, Vector3?> tryTap;   // returns where the valid target is, or null
    }

    GuidedEvent mission;
    readonly List<Objective> objectives = new List<Objective>();
    bool missionPlanting;      // BS02 "Grow More Grass" in progress
    int missionPlanted;
    float missionStartedAt;
    string missionCompletion, missionChallenge, lastFound;

    GameObject eventCardGo, missionTracker, missionDone, trackerBack, trackerSkip;
    TextMeshProUGUI eventCardTitle, missionText, trackerTitle, trackerList, doneTitle, doneText;
    GameObject missionGoBtn, missionGrowBtn, missionNatureBtn;

    /// <summary>A mission's small tracker is up and the player is looking around the world (camera buttons stay usable).</summary>
    public static bool MissionSearching => instance != null && instance.missionTracker != null && instance.missionTracker.activeInHierarchy;

    /// <summary>A card is opening: hide the side UI (Dillon's mission tracker, the live weather box) this frame,
    /// not one frame later, so nothing is drawn over the card's edge (28 Nov visual check).</summary>
    void HideSideUiNow()
    {
        if (trackerBox != null) trackerBox.SetActive(false);
        SimHud.SetWeatherVisible(false);
        weatherShown = false;
    }

    static bool BlueSheepStory => FamilyManager.SelectedStory == FamilyStory.BlueSheep;

    void AddMissionEvents()
    {
        var list = new List<GuidedEvent>
        {
            new GuidedEvent {
                id = "BS01", title = "Miloy's First Summer", mission = StartBS01,
                text = "Summer has arrived in the highlands! Miloy and his herd are surrounded by fresh grass and open mountain landscapes. " +
                       "As Miloy explores his home, he needs food and water to grow and stay healthy. Let's discover where he can find them!",
                eligible = () => BlueSheepStory && Day >= 2, chapter = 1 },
            new GuidedEvent {
                id = "BS02", title = "A Season of Plenty", mission = StartBS02,
                text = "Summer brings plenty of fresh grass to the highlands. As the sheep eat, the grass slowly grows back. " +
                       "Should we help more grass grow, or let nature take care of itself?",
                eligible = () => BlueSheepStory && Day >= 3 && resolved.Contains("BS01"), chapter = 1 },
            new GuidedEvent {
                id = "BS03-F", title = "A Predator's Home", mission = StartBS03F,
                text = "Snow Leopards share the highlands with Miloy's herd. They can be dangerous to Blue Sheep, " +
                       "but they also play an important role in the ecosystem.",
                eligible = () => BlueSheepStory && Day >= 4 && resolved.Contains("BS02"), chapter = 1 },   // doc: Day 22 of 24, late Summer (was Day 6 = Autumn)
            // Snow Leopard library (Snowy's story).
            new GuidedEvent {
                id = "SL01", title = "Snowy's First Summer", mission = StartSL01,
                text = "Summer has arrived in the highlands! Snowy is a young Snow Leopard living with her mother and sibling. " +
                       "While the cubs grow and explore, their mother searches for Blue Sheep to keep the family fed. Let's discover Snowy's family and the animals they depend on.",
                eligible = () => !BlueSheepStory && Day >= 2, chapter = 1 },
        };
        events.InsertRange(0, list);   // before the older GE events, so a mission wins when both are due
    }

    // ---------- The three missions ----------

    void StartBS01(GuidedEvent e)
    {
        objectives.Clear();
        var milo = FamilyManager.MainAnimal;
        objectives.Add(new Objective {
            label = "Meet Miloy: find and tap Miloy", feedback = "You found Miloy!",
            done = milo == null,
            tryTap = (tap, cam) => milo != null && milo.Brain != null && Near(cam, tap, milo.Brain.transform.position) ? milo.Brain.transform.position : (Vector3?)null });
        bool anyGrass = false;
        foreach (var p in ResourcePatch.All) if (p != null && !p.IsWater && p.Amount > p.MaxAmount * 0.1f) { anyGrass = true; break; }
        objectives.Add(new Objective {
            label = "Discover Food: tap a patch of grass",
            feedback = anyGrass ? "Blue Sheep eat grass to get energy." : "There's no green grass right now, but Blue Sheep eat grass to get energy.",
            done = !anyGrass,
            tryTap = (tap, cam) => NearestPatch(cam, tap, false) });
        objectives.Add(new Objective {
            label = "Discover Water: tap a water pool", feedback = "Miloy needs water to stay healthy.",
            tryTap = (tap, cam) => NearestPatch(cam, tap, true) });
        missionCompletion = "Great job! You've discovered what Miloy needs to grow and thrive in the highlands!";
        ShowMissionCard(e, "Can you discover what Miloy needs to grow and stay healthy?", false);
    }

    void StartBS02(GuidedEvent e)
    {
        objectives.Clear();
        missionPlanted = 0;
        ShowMissionCard(e, "Can you help Miloy's herd make the most of Summer while keeping the grassland healthy?", true);
    }

    void StartBS03F(GuidedEvent e)
    {
        objectives.Clear();
        bool leopardAlive = AnyAlive(true);
        objectives.Add(new Objective {
            label = "Discover the Predator: tap a Snow Leopard",
            feedback = leopardAlive ? "Snow Leopards need to hunt other animals for food." : "No Snow Leopards are left, but they need to hunt other animals for food.",
            done = !leopardAlive,
            tryTap = (tap, cam) => NearestAnimal(cam, tap, true) });
        objectives.Add(new Objective {
            label = "Discover the Prey: tap a Blue Sheep", feedback = "Blue Sheep are an important food source for Snow Leopards.",
            done = !AnyAlive(false),
            tryTap = (tap, cam) => NearestAnimal(cam, tap, false) });
        missionCompletion = "Blue Sheep and Snow Leopards are connected through the food chain. Both play important roles in the highlands.";
        ShowMissionCard(e, "Can you discover how Blue Sheep and Snow Leopards depend on the highlands?", false);
    }

    void StartSL01(GuidedEvent e)
    {
        objectives.Clear();
        objectives.Add(MemberObjective(FamilyRole.Main, "Meet Snowy: find and tap Snowy",
            "You found Snowy! She is still growing and depends on her mother for food."));
        objectives.Add(MemberObjective(FamilyRole.Mother, "Meet the Family: tap Snowy's mother",
            "Snowy's mother hunts to feed the family."));
        objectives.Add(MemberObjective(FamilyRole.Sibling, "Meet the Family: tap Snowy's sibling",
            "Snowy and her sibling depend on their mother as they grow up together."));
        objectives.Add(new Objective {
            label = "Discover the Food Source: tap a Blue Sheep",
            feedback = "Snow Leopards hunt Blue Sheep for food. Both animals are important parts of the highlands.",
            done = !AnyAlive(false),
            tryTap = (tap, cam) => NearestAnimal(cam, tap, false) });
        missionCompletion = "Snowy and her sibling depend on their mother for food. Their mother depends on the Blue Sheep living in the highlands. " +
                            "Let's follow Snowy's family and see how they survive together.";
        ShowMissionCard(e, "Can you find Snowy's family and discover where they get their food?", false);
    }

    // A family member to find; already done (with a note) if that member is no longer alive.
    static Objective MemberObjective(FamilyRole role, string label, string feedback)
    {
        var m = FamilyManager.Get(role);
        return new Objective {
            label = label,
            feedback = m != null ? feedback : "This family member is no longer with Snowy, but the family keeps going.",
            done = m == null,
            tryTap = (tap, cam) => m != null && m.IsAlive && m.Brain != null && Near(cam, tap, m.Brain.transform.position) ? m.Brain.transform.position : (Vector3?)null };
    }

    // ---------- Flow: card -> tracker (tap in the world) -> completion card ----------

    void ShowMissionCard(GuidedEvent e, string challenge, bool decision)
    {
        mission = e;
        missionPlanting = false;
        eventCardTitle.text = e.title;
        string goals = "";
        foreach (var o in objectives) goals += $"\n-  {o.label}";
        missionText.text = $"{e.text}\n\n<b>{challenge}</b>" + (decision ? "" : $"\n<size=85%>{goals}</size>");
        missionGoBtn.SetActive(!decision);
        missionGrowBtn.SetActive(decision);
        missionNatureBtn.SetActive(decision);
        missionTracker.SetActive(false);
        missionDone.SetActive(false);
        HideSideUiNow();
        eventCardGo.SetActive(true);
        missionChallenge = challenge;
        lastFound = null;
    }

    void BeginInvestigation()
    {
        eventCardGo.SetActive(false);
        missionStartedAt = Time.unscaledTime;
        trackerBack.SetActive(false);
        RefreshTracker();
        missionTracker.SetActive(true);
        if (AllObjectivesDone()) CompleteMission(missionCompletion);   // nothing left to find (e.g. no animals alive)
    }

    void ChooseGrowGrass()
    {
        eventCardGo.SetActive(false);
        missionPlanting = true;
        missionStartedAt = Time.unscaledTime;
        trackerBack.SetActive(true);
        RefreshTracker();
        missionTracker.SetActive(true);
    }

    void ChooseLetNatureGrow()
    {
        missionPlanting = false;
        eventCardGo.SetActive(false);
        missionTracker.SetActive(false);
        CompleteMission(missionPlanted > 0
            ? $"You planted {missionPlanted} patch{(missionPlanted == 1 ? "" : "es")} of grass, and now nature can do the rest. Let's watch how the grass grows!"
            : "Summer already provides plenty of grass. Let's watch how nature takes care of itself!");
    }

    void BackToChoices()
    {
        missionPlanting = false;
        missionTracker.SetActive(false);
        eventCardGo.SetActive(true);
    }

    /// <summary>Called every frame from Update; true while a mission is running (nothing else happens).</summary>
    bool UpdateMission()
    {
        // Demo / test key: M starts the next mission now (BS01 -> BS02 -> BS03-F), whatever the day (M3 presentation).
        if (mission == null && !showing && !missionCardOpen && !ScreenFlow.JournalOpen
            && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            string lib = BlueSheepStory ? "BS" : "SL";   // only this story's missions
            foreach (var e in events)
                if (e.mission != null && e.id.StartsWith(lib) && !resolved.Contains(e.id)) { Show(e); break; }
        }
        if (mission == null) return false;
        if (!missionTracker.activeSelf) return true;   // a card is up: its buttons drive the flow

        trackerSkip.SetActive(Time.unscaledTime - missionStartedAt > MissionSkipAfterSeconds);
        FitTracker();

        var cam = Camera.main;
        var pointer = Pointer.current;
        if (cam == null || pointer == null || !pointer.press.wasPressedThisFrame || PointerOverUI.IsOver()) return true;
        Vector2 tap = pointer.position.ReadValue();

        if (missionPlanting)
        {
            var p = NearestPlantable(cam, tap);
            if (p == null) return true;
            p.Restore(PlantAmount);
            PlantSparkle(p.transform.position);
            missionPlanted++;
            GrassPlanted++;
            RefreshTracker();
            if (missionPlanted >= MissionPlantsNeeded)
            {
                missionPlanting = false;
                missionTracker.SetActive(false);
                GuidedEffects.Add(GuidedEffects.Kind.PlantRegen, 1.5f, 5);
                CompleteMission("You've helped more grass grow! Let's see how Miloy and his herd use it.\n<size=80%>Grass grows back 50% faster for 5 days.</size>");
            }
            return true;
        }

        foreach (var o in objectives)
        {
            if (o.done) continue;
            var at = o.tryTap(tap, cam);
            if (at == null) continue;
            o.done = true;
            PlantSparkle(at.Value);
            lastFound = o.feedback;   // shown in the tracker as text (Shumin's VO script has no mission lines: no voice)
            RefreshTracker();
            break;
        }
        if (AllObjectivesDone())
        {
            missionTracker.SetActive(false);
            CompleteMission(missionCompletion);
        }
        return true;
    }

    void SkipMission()
    {
        missionPlanting = false;
        missionTracker.SetActive(false);
        CompleteMission(missionCompletion ?? "Let's keep watching the highlands!");
    }

    void CompleteMission(string text)
    {
        doneTitle.text = mission != null ? $"{mission.title}: complete!" : "Mission complete!";
        string found = "";
        foreach (var o in objectives) if (o.done) found += $"\n- {o.feedback}";
        doneText.text = text + (found.Length > 0 ? $"\n<size=80%>{found}</size>" : "");
        missionDone.SetActive(true);
        Mascot.Hide();   // the card is read, not voiced (same as the mission card); the HUD mascot would sit under its dim layer
    }

    void CloseMission()
    {
        missionDone.SetActive(false);
        eventCardGo.SetActive(false);
        missionTracker.SetActive(false);
        missionPlanting = false;
        if (mission != null) resolved.Add(mission.id);
        mission = null;
        objectives.Clear();
        missionCompletion = null;
        showing = false;
        PopupOpen = false;
        quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
        if (speed != null) speed.Play();
    }

    void HideMissionUI()
    {
        if (eventCardGo != null) eventCardGo.SetActive(false);
        if (missionTracker != null) missionTracker.SetActive(false);
        if (missionDone != null) missionDone.SetActive(false);
        mission = null;
    }

    bool AllObjectivesDone()
    {
        foreach (var o in objectives) if (!o.done) return false;
        return true;
    }

    // 28 Nov visual check: no empty band under the goals when the tracker has no buttons (Back / Skip).
    void FitTracker()
    {
        var rt = (RectTransform)missionTracker.transform;
        bool buttons = trackerBack.activeSelf || trackerSkip.activeSelf;
        float h = buttons ? 330f : 230f;
        var lr = trackerList.rectTransform;
        lr.offsetMin = new Vector2(lr.offsetMin.x, buttons ? 110f : 20f);   // the goal list uses the space the buttons would take
        var tr = trackerTitle.rectTransform;
        tr.offsetMin = new Vector2(tr.offsetMin.x, h - 70f);   // title keeps its 52 px line under an 18 px top margin
        if (!Mathf.Approximately(rt.sizeDelta.y, h)) rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
    }

    void RefreshTracker()
    {
        FitTracker();
        trackerTitle.text = mission != null ? mission.title : "";
        if (missionPlanting)
        {
            trackerList.text = $"Tap the grass to plant new grass:  <b>{missionPlanted} / {MissionPlantsNeeded}</b>";
            return;
        }
        string s = "";
        foreach (var o in objectives) s += (o.done ? $"<color=#2E8B57><b>Done!</b></color>  <s>{o.label}</s>" : $"-  {o.label}") + "\n";
        if (!string.IsNullOrEmpty(lastFound)) s += $"<color=#2E8B57><i>{lastFound}</i></color>";   // the doc's narrative feedback for the last find
        trackerList.text = s.TrimEnd();
    }

    // ---------- Finding what was tapped ----------

    static float TapLimit => MissionTapRadiusPx * Screen.height / 1200f;

    static bool Near(Camera cam, Vector2 tap, Vector3 world)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        return sp.z > 0f && Vector2.Distance(tap, sp) < TapLimit;
    }

    static Vector3? NearestAnimal(Camera cam, Vector2 tap, bool predator)
    {
        foreach (var a in AnimalBrain.All)
            if (a != null && !a.IsCaught && a.IsPredatorSpecies == predator && a.gameObject.activeInHierarchy && Near(cam, tap, a.transform.position))
                return a.transform.position;
        return null;
    }

    static bool AnyAlive(bool predator)
    {
        foreach (var a in AnimalBrain.All)
            if (a != null && !a.IsCaught && a.IsPredatorSpecies == predator && a.gameObject.activeInHierarchy) return true;
        return false;
    }

    static Vector3? NearestPatch(Camera cam, Vector2 tap, bool water)
    {
        foreach (var p in ResourcePatch.All)
            if (p != null && p.IsWater == water && (water || p.Amount > p.MaxAmount * 0.1f) && Near(cam, tap, p.transform.position))
                return p.transform.position;
        return null;
    }

    static ResourcePatch NearestPlantable(Camera cam, Vector2 tap)
    {
        float limit = TapLimit;
        ResourcePatch best = null;
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.IsWater || GrassFire.IsBurning(p)) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(tap, sp);
            if (d < limit) { limit = d; best = p; }
        }
        return best;
    }

    // ---------- UI (code-built, styled like Shumin's cards) ----------

    void BuildEventMissionUI(Transform root)
    {
        var ink = new Color(0.12f, 0.16f, 0.2f);
        var body = new Color(0.2f, 0.24f, 0.28f);
        var green = new Color(0.3f, 0.62f, 0.38f);
        var blue = new Color(0.28f, 0.5f, 0.75f);
        var grey = new Color(0.55f, 0.55f, 0.55f);

        // Mission card: what is happening, the challenge, and how to start.
        eventCardGo = Dim(root, "Mission");
        var card = Box(eventCardGo.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 820f), new Color(0.97f, 0.97f, 0.95f, 1f));
        eventCardTitle = Label(card.transform, "", 56f, ink, 60f, 40f, 700f, TextAlignmentOptions.Top);
        missionText = Label(card.transform, "", 34f, body, 70f, 140f, 190f, TextAlignmentOptions.Top);
        missionGoBtn = ButtonGo(card.transform, "Let's go!", new Vector2(0f, 50f), green, BeginInvestigation);
        missionGrowBtn = ButtonGo(card.transform, "Grow More Grass", new Vector2(-230f, 50f), green, ChooseGrowGrass, 420f);
        missionNatureBtn = ButtonGo(card.transform, "Let Nature Grow", new Vector2(230f, 50f), blue, ChooseLetNatureGrow, 420f);
        ScreenFlow.StyleLikeShumin(card, null);
        eventCardGo.SetActive(false);

        // Tracker: a small card at the top while the player looks around and taps (no dim: the world must be tappable).
        missionTracker = Box(root, "Mission tracker", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(860f, 330f), new Color(0.97f, 0.97f, 0.95f, 0.95f), new Vector2(0.5f, 1f));
        trackerTitle = Label(missionTracker.transform, "", 38f, ink, 30f, 18f, 260f, TextAlignmentOptions.Top);
        trackerList = Label(missionTracker.transform, "", 28f, body, 40f, 80f, 110f, TextAlignmentOptions.TopLeft);
        trackerBack = ButtonGo(missionTracker.transform, "Back to choices", new Vector2(-200f, 14f), grey, BackToChoices, 340f, 80f);
        trackerSkip = ButtonGo(missionTracker.transform, "Skip", new Vector2(220f, 14f), grey, SkipMission, 220f, 80f);
        ScreenFlow.StyleLikeShumin(missionTracker, null);
        missionTracker.SetActive(false);
        trackerSkip.SetActive(false);

        // Completion card.
        missionDone = Dim(root, "Mission complete");
        var doneCard = Box(missionDone.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 700f), new Color(0.97f, 0.97f, 0.95f, 1f));
        doneTitle = Label(doneCard.transform, "", 54f, ink, 60f, 40f, 580f, TextAlignmentOptions.Top);
        doneText = Label(doneCard.transform, "", 34f, body, 70f, 150f, 180f, TextAlignmentOptions.Top);
        ButtonGo(doneCard.transform, "Continue", new Vector2(0f, 50f), green, CloseMission);
        ScreenFlow.StyleLikeShumin(doneCard, null);
        missionDone.SetActive(false);
    }

    static GameObject ButtonGo(Transform parent, string text, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick, float width = 330f, float height = 100f)
    {
        var b = Box(parent, text, new Vector2(0.5f, 0f), pos, new Vector2(width, height), color, new Vector2(0.5f, 0f));
        b.AddComponent<Button>().onClick.AddListener(onClick);
        Label(b.transform, text, height * 0.36f, Color.white, 12f);
        return b;
    }
}
