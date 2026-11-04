// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mascot Guidance System (Shu Min's research doc, "Mascot Guidance System"): a friendly guide that speaks
/// short lines in a speech bubble above the bottom-left controls. It never changes the simulation.
/// Uses Shu Min's MascotPanel (Mascot, SpeechBubble/DynamicTxt, NextBtn, SkipBtn) in the HUD scenes, found
/// by name, and her IntroMascotPanel on the Mode Selection and Configuration screens.
///   Free (FE-M01..14): intro (Next / Skip / Got It), Help, 45 s exploration hints, animal reactions
///   (birth, stalking, catch, escape) that replace the old top banner, first Terrain Editor visit.
///   Guided (GE-M01..07): intro after the challenge card, Help, event and decision lines, hint.
/// </summary>
public class Mascot : MonoBehaviour
{
    // ---------- Lines (text from the doc) ----------

    static readonly string[] FreeIntro =
    {
        "Welcome to your highlands! Let's see what happens to the animals!",              // FE-M01
        "Click on a Blue Sheep or Snow Leopard to see what it's doing!",                   // FE-M02
        "Use the play and speed buttons to watch your ecosystem change!",                  // FE-M03
        "Keep an eye on the grass and animal populations. What do you think will happen?", // FE-M04
    };
    const string FreeHelp = "Need help? Try clicking an animal, checking your Field Journal, or editing the terrain!";   // FE-M05
    const string HintAny = "Not sure what to explore? Try clicking on an animal!";                                         // FE-M06
    const string HintJournal = "Don't forget to check your Field Journal! You might discover something new!";              // FE-M07
    const string HintInspect = "Can you spot a Blue Sheep or a Snow Leopard? Click on one when you spot it!";              // FE-M08
    const string TerrainIntro = "You can change the highlands too! Try making hills or even add grass!";                   // FE-M09
    const string SheepBorn = "A new Blue Sheep was born! Our herd has grown! Want to take a closer look?";                 // FE-M10
    const string LeopardBorn = "A new Snow Leopard was born! Want to take a closer look?";                                 // FE-M11
    const string Stalking = "Look! A Snow Leopard is stalking a Blue Sheep! Want to see where it is?";                     // FE-M12
    const string CaughtLine = "A Snow Leopard caught a Blue Sheep! Predators need food to survive too!";                   // FE-M13
    const string Escaped = "The Blue Sheep escaped! Not every hunt is successful!";                                        // FE-M14

    static readonly string[] GuidedIntro =
    {
        "Our adventure has begun! Let's see how the highlands change!",                // GE-M01
        "Watch the grass, Blue Sheep, and Snow Leopards. Can you spot any changes?",  // GE-M02
        "Try clicking on an animal to see how it's doing!",                           // GE-M03
    };
    const string GuidedHelp = "Need help? Try inspecting an animal or watching how the ecosystem changes!";       // GE-M04
    const string EventLine = "Something is happening in the highlands! Let's see what we can do!";                // GE-M05
    const string DecisionLine = "Let's see how your choice affects the highlands!";                               // GE-M06
    const string GuidedHint = "Can you find a Blue Sheep or Snow Leopard? Click on one to learn more!";           // GE-M07

    public const string ModeSelectLine = "Ready to explore the highlands? Choose how you'd like to explore today!";
    public const string ConfigureLine = "Let's build your ecosystem! Choose how many Blue Sheep and Snow Leopards you'd like to start with!";

    const float HintAfterSeconds = 45f;     // no meaningful interaction for this long
    const float HintCooldown = 90f;         // shared by all hints
    const float ReactionSeconds = 8f;       // animal reactions close by themselves

    // ---------- State ----------

    static Mascot current;
    GameObject panel;
    TMP_Text line, nextLabel, skipLabel;
    GameObject nextBtn, skipBtn;
    string[] sequence;
    int index;
    Transform watchTarget;
    float hideAt = -1f;
    float lastActivity, hintReadyAt;
    bool journalOpened, inspected, terrainSeen, introPlayed;

    /// <summary>A mascot line is on screen.</summary>
    public static bool Showing => current != null && current.panel != null && current.panel.activeSelf;

    /// <summary>Adds the mascot to the simulation HUD (does nothing if the HUD has no MascotPanel).</summary>
    public static void Attach(Transform hud)
    {
        var p = Find(hud, "MascotPanel");
        if (p == null) return;
        current = hud.gameObject.AddComponent<Mascot>();
        current.panel = p.gameObject;
    }

    /// <summary>Static mascot line on a menu screen (Mode Selection, Configuration).</summary>
    public static void SetMenuLine(Scene scene, string text)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var p = Find(root.transform, "IntroMascotPanel");
            if (p == null) continue;
            p.gameObject.SetActive(true);
            var t = Find(p, "DynamicTxt");
            if (t != null && t.TryGetComponent(out TMP_Text tmp)) tmp.text = text;
        }
    }

    void Start()
    {
        var t = Find(panel.transform, "DynamicTxt");
        if (t != null) line = t.GetComponent<TMP_Text>();
        var n = Find(panel.transform, "NextBtn");
        var s = Find(panel.transform, "SkipBtn");
        nextBtn = n != null ? n.gameObject : null;
        skipBtn = s != null ? s.gameObject : null;
        nextLabel = n != null ? n.GetComponentInChildren<TMP_Text>(true) : null;
        skipLabel = s != null ? s.GetComponentInChildren<TMP_Text>(true) : null;
        Wire(nextBtn, OnNext);
        Wire(skipBtn, Dismiss);
        panel.SetActive(false);
        lastActivity = Time.unscaledTime;
        hintReadyAt = Time.unscaledTime + HintAfterSeconds;
        AddHelpButton();

        // Free starts straight away; Guided waits for the challenge card (GuidedChallenge calls PlayGuidedIntro).
        if (!ScreenFlow.GuidedMode) Invoke(nameof(PlayFreeIntro), 1f);
    }

    void OnDestroy() { if (current == this) current = null; }

    // ---------- Showing lines ----------

    void Say(string[] lines, bool intro, Transform watch = null, float autoHide = -1f)
    {
        if (line == null) return;
        sequence = lines;
        index = 0;
        watchTarget = watch;
        hideAt = autoHide > 0f ? Time.unscaledTime + autoHide : -1f;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh(intro);
    }

    bool intro;
    void Refresh(bool isIntro)
    {
        intro = isIntro;
        line.text = sequence[index];
        bool last = index == sequence.Length - 1;
        if (watchTarget != null) { SetLabel(nextLabel, "Watch"); SetLabel(skipLabel, "Dismiss"); skipBtn?.SetActive(true); }
        else { SetLabel(nextLabel, last ? "Got It" : "Next"); SetLabel(skipLabel, "Skip"); skipBtn?.SetActive(intro && !last); }
        nextBtn?.SetActive(true);
    }

    void OnNext()
    {
        if (watchTarget != null)
        {
            var cam = FindFirstObjectByType<TerrainCameraController>();
            if (cam != null) cam.Follow(watchTarget);
            Dismiss();
            return;
        }
        if (index < sequence.Length - 1) { index++; Refresh(intro); return; }
        Dismiss();
    }

    void Dismiss()
    {
        panel.SetActive(false);
        watchTarget = null;
        hideAt = -1f;
        lastActivity = Time.unscaledTime;
    }

    void Update()
    {
        if (Showing && hideAt > 0f && Time.unscaledTime >= hideAt) Dismiss();

        // Any tap or click counts as activity (inspecting, playback, camera moves...).
        var touch = Touchscreen.current;
        var mouse = Mouse.current;
        if ((touch != null && touch.primaryTouch.press.isPressed) || (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)))
            lastActivity = Time.unscaledTime;

        if (!Showing && !ModalOpen() && Time.unscaledTime - lastActivity >= HintAfterSeconds && Time.unscaledTime >= hintReadyAt)
        {
            string hint = ScreenFlow.GuidedMode ? GuidedHint : !journalOpened ? HintJournal : !inspected ? HintInspect : HintAny;
            Say(new[] { hint }, false);
            hintReadyAt = Time.unscaledTime + HintCooldown;
            lastActivity = Time.unscaledTime;
        }
    }

    // Other popups come first: no mascot over the Guided intro/event cards, a question, or the journal.
    static bool ModalOpen() =>
        ContextualQuestions.Showing || GuidedChallenge.PopupOpen ||
        SceneManager.GetSceneByName(ScreenFlow.Journal).isLoaded ||
        SceneManager.GetSceneByName(GuidedScreens.GalleryScene).isLoaded;

    // ---------- Triggers called from the rest of the game ----------

    void PlayFreeIntro()
    {
        if (introPlayed) return;
        introPlayed = true;
        Say(FreeIntro, true);
    }

    /// <summary>GE-M01..03, once the Guided challenge card is closed.</summary>
    public static void PlayGuidedIntro()
    {
        if (current == null || current.introPlayed) return;
        current.introPlayed = true;
        current.Say(GuidedIntro, true);
    }

    /// <summary>GE-M05 while a Guided event card is open, GE-M06 after a choice.</summary>
    public static void GuidedEvent(bool decided)
    {
        if (current == null) return;
        current.Say(new[] { decided ? DecisionLine : EventLine }, false, null, decided ? ReactionSeconds : -1f);
    }

    /// <summary>Closes the GE-M05 line when the event card closes.</summary>
    public static void Hide() { if (current != null && Showing) current.Dismiss(); }

    public static void NoteJournalOpened() { if (current != null) current.journalOpened = true; }
    public static void NoteInspected() { if (current != null) current.inspected = true; }

    /// <summary>FE-M09, the first time the Terrain Editor opens in a run.</summary>
    public static void NoteTerrainEditor()
    {
        if (current == null || current.terrainSeen || ScreenFlow.GuidedMode) return;
        current.terrainSeen = true;
        if (!Showing && !ModalOpen()) current.Say(new[] { TerrainIntro }, false);
    }

    /// <summary>
    /// Free Exploration: animal moments (birth, stalking, catch, escape) are spoken by the mascot instead of
    /// the top banner, with Watch / Dismiss. False = not handled (Guided, no mascot, or a popup is open).
    /// </summary>
    public static bool React(DramaDirector.Moment moment, Transform subject, string bannerText)
    {
        if (current == null || ScreenFlow.GuidedMode || ModalOpen()) return false;
        if (Showing && current.intro) return false;   // never cut into the introduction
        string text = null;
        switch (moment)
        {
            case DramaDirector.Moment.Birth:
                bool leopard = subject != null && subject.TryGetComponent(out AnimalBrain b) && b.IsPredatorSpecies;
                text = leopard ? LeopardBorn : SheepBorn;
                break;
            case DramaDirector.Moment.Hunt: text = Stalking; break;
            case DramaDirector.Moment.Catch: text = bannerText != null && bannerText.Contains("got away") ? Escaped : CaughtLine; break;
            default: return false;   // mating keeps the old banner
        }
        bool watch = moment != DramaDirector.Moment.Catch;
        current.Say(new[] { text }, false, watch ? subject : null, ReactionSeconds);
        return true;
    }

    // ---------- Help button ----------

    // The HUDs have no Help button yet (GuidedExploration's is switched off): use it, or copy the settings
    // square beside it and label it Help.
    void AddHelpButton()
    {
        var hud = transform;
        var help = Find(hud, "HelpBtn");
        if (help == null)
        {
            var settings = Find(hud, "SettingBtn");
            if (settings == null) return;
            help = Instantiate(settings.gameObject, settings.parent).transform;
            help.name = "HelpBtn";
            var rt = (RectTransform)help;
            rt.anchoredPosition -= new Vector2(rt.rect.width + 16f, 0f);
        }
        help.gameObject.SetActive(true);
        foreach (var t in help.GetComponentsInChildren<TMP_Text>(true)) t.text = "Help";
        if (help.GetComponentInChildren<TMP_Text>(true) == null && help.TryGetComponent(out Button hb)) ButtonCaption.Ensure(hb, "Help", 20f);
        Wire(help.gameObject, () =>
        {
            if (ModalOpen()) return;
            Say(new[] { ScreenFlow.GuidedMode ? GuidedHelp : FreeHelp }, false);
        });
    }

    // ---------- Helpers ----------

    static void SetLabel(TMP_Text t, string s) { if (t != null) t.text = s; }

    static void Wire(GameObject go, UnityEngine.Events.UnityAction action)
    {
        if (go == null) return;
        if (!go.TryGetComponent(out Button b)) b = go.AddComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    static Transform Find(Transform under, string name)
    {
        if (under == null) return null;
        foreach (var t in under.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim().Trim('\'').Trim() == name) return t;
        return null;
    }
}
