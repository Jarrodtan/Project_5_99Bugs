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
    public const string EventLine = "Something is happening in the highlands! Let's see what we can do!";         // GE-M05
    public const string DecisionLine = "Let's see how your choice affects the highlands!";                        // GE-M06
    const string GuidedHint = "Can you find a Blue Sheep or Snow Leopard? Click on one to learn more!";           // GE-M07

    public const string ModeSelectLine = "Ready to explore the highlands? Choose how you'd like to explore today!";
    public const string ConfigureLine = "Let's build your ecosystem! Choose how many Blue Sheep and Snow Leopards you'd like to start with!";
    public const string GuidedIntroScreenLine = "Ready for an ecosystem adventure? Let's help the highlands stay in balance!";

    // [AI-GENERATED] Claude Code (Claude Opus 5.5) - Discovery activity (doc "Interactive Discovery Activities"), offered from Free Help
    const string FindButton = "Explore!";
    const string FoundLine = "You found one! Let's see how it's doing!";
    static string FindLine(bool leopard) => $"Can you find a {(leopard ? "Snow Leopard" : "Blue Sheep")}? Click on one when you spot it!";

    const float HintAfterSeconds = 45f;     // no meaningful interaction for this long
    const float HintCooldown = 90f;         // shared by all hints
    const float ReactionSeconds = 5f;       // animal reactions close by themselves (doc: 4-6 real-time seconds)

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
    string altLabel;                         // second button on the last line instead of Skip (e.g. "Explore!")
    UnityEngine.Events.UnityAction altAction;
    bool? findLeopard;                       // discovery activity running: which species to find

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
        Speak(text);
    }

    // ---------- Voice ----------
    // [AI-GENERATED] Claude Code (Claude Opus 5.5): pre-recorded voice lines. tools/voice/generate_mascot_voice.py
    // makes one clip per line into Resources/MascotVoice, named by ClipName(text). A line without a clip
    // (or with the voice switched off) just shows its text, so new lines never break anything.

    public const string VoicePref = "EcoBalance.MascotVoice";
    static AudioSource voice;

    /// <summary>Mascot voice on/off (saved). Off stops the current line.</summary>
    public static bool VoiceOn
    {
        get => PlayerPrefs.GetInt(VoicePref, 1) == 1;
        set { PlayerPrefs.SetInt(VoicePref, value ? 1 : 0); if (!value) StopVoice(); }
    }

    /// <summary>Clip file name for a line: FNV-1a 32-bit of the trimmed UTF-8 text (same as the Python script).</summary>
    public static string ClipName(string text)
    {
        uint h = 2166136261;
        foreach (byte b in System.Text.Encoding.UTF8.GetBytes(text.Trim())) { h ^= b; h *= 16777619; }
        return h.ToString("x8");
    }

    // Leaving a screen (Mode Selection -> Guided intro, closing the journal...) cuts the line that was playing.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookScenes()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;   // Enter Play Mode skips domain reload: never hook twice
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    static void OnSceneUnloaded(Scene _) => StopVoice();

    /// <summary>Plays the voice clip for a line shown outside the mascot panel (Guided intro / event / feedback cards).</summary>
    public static void Speak(string text)
    {
        StopVoice();
        if (!VoiceOn || string.IsNullOrEmpty(text)) return;
        var clip = Resources.Load<AudioClip>("MascotVoice/" + ClipName(text));
        if (clip == null) return;
        if (voice == null)
        {
            var go = new GameObject("MascotVoice");
            DontDestroyOnLoad(go);
            voice = go.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voice.ignoreListenerPause = true;   // keeps talking while the run is paused
        }
        voice.clip = clip;
        voice.Play();
    }

    public static void StopVoice() { if (voice != null) voice.Stop(); }

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
        Wire(skipBtn, OnSkip);
        // Longer labels ("Dismiss", "Explore!") shrink to fit the button instead of spilling out.
        foreach (var l in new[] { nextLabel, skipLabel })
            if (l != null) { if (!l.enableAutoSizing) l.fontSizeMax = l.fontSize; l.fontSizeMin = Mathf.Min(l.fontSizeMax * 0.6f, 18f); l.enableAutoSizing = true; l.textWrappingMode = TextWrappingModes.NoWrap; }   // "Explore!" broke onto two lines
        panel.SetActive(false);
        lastActivity = Time.unscaledTime;
        hintReadyAt = Time.unscaledTime + HintAfterSeconds;
        AddHelpButton();

        // Free starts straight away; Guided waits for the challenge card (GuidedChallenge calls PlayGuidedIntro).
        if (!ScreenFlow.GuidedMode) Invoke(nameof(PlayFreeIntro), 1f);
    }

    void OnDestroy() { if (panel != null) Spotlight.Hide(panel.transform); if (current == this) { current = null; StopVoice(); } }

    // ---------- Showing lines ----------

    void Say(string[] lines, bool intro, Transform watch = null, float autoHide = -1f,
             string alt = null, UnityEngine.Events.UnityAction altDo = null)
    {
        if (line == null) return;
        sequence = lines;
        index = 0;
        watchTarget = watch;
        altLabel = alt;
        altAction = altDo;
        hideAt = autoHide > 0f ? Time.unscaledTime + autoHide : -1f;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        BehindControlBar();
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the introduction dims the rest of the screen until Got It / Skip.
        if (intro) Spotlight.Show(panel.transform, ControlBar()); else Spotlight.Hide(panel.transform);
        Refresh(intro);
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): the mascot stands bottom-left where the Play / speed / Reset bar is:
    // draw it just under that bar so the buttons stay visible and tappable.
    void BehindControlBar()
    {
        var bar = ControlBar();
        if (bar != null) panel.transform.SetSiblingIndex(Mathf.Max(0, bar.GetSiblingIndex()));
    }

    // The sibling of the mascot panel that holds the Play/Pause button (the control bar), or null.
    Transform ControlBar()
    {
        var parent = panel.transform.parent;
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var sib = parent.GetChild(i);
            if (sib == panel.transform || !sib.gameObject.activeInHierarchy) continue;
            if (sib.name == "Play/PauseBtn" || Find(sib, "Play/PauseBtn") != null) return sib;
        }
        return null;
    }

    bool intro;
    void Refresh(bool isIntro)
    {
        intro = isIntro;
        line.text = sequence[index];
        Speak(sequence[index]);
        // A line that closes by itself stays up until its voice has finished.
        if (hideAt > 0f && voice != null && voice.isPlaying)
            hideAt = Mathf.Max(hideAt, Time.unscaledTime + voice.clip.length + 0.5f);
        bool last = index == sequence.Length - 1;
        if (watchTarget != null) { SetLabel(nextLabel, "Watch"); SetLabel(skipLabel, "Dismiss"); skipBtn?.SetActive(true); }
        else if (altLabel != null && last) { SetLabel(nextLabel, "Got It"); SetLabel(skipLabel, altLabel); skipBtn?.SetActive(true); }
        else { SetLabel(nextLabel, last ? "Got It" : "Next"); SetLabel(skipLabel, "Skip"); skipBtn?.SetActive(intro && !last); }
        nextBtn?.SetActive(true);
    }

    void OnSkip()
    {
        var act = altAction;
        if (act != null && index == sequence.Length - 1) { act(); return; }
        Dismiss();
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
        Spotlight.Hide(panel.transform);
        StopVoice();
        panel.SetActive(false);
        watchTarget = null;
        altLabel = null;
        altAction = null;
        hideAt = -1f;
        lastActivity = Time.unscaledTime;
    }

    string queuedNews;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): season/weather line waiting for the mascot to be free

    /// <summary>True once a mascot is in the HUD and can talk.</summary>
    public static bool Ready => current != null && current.line != null;

    void Update()
    {
        if (Showing && hideAt > 0f && Time.unscaledTime >= hideAt) Dismiss();
        if (queuedNews != null && !Showing && !ModalOpen()) { string n = queuedNews; queuedNews = null; Say(new[] { n }, false, null, ReactionSeconds); }

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
    public static void Hide() { StopVoice(); if (current != null && Showing) current.Dismiss(); }

    public static void NoteJournalOpened() { if (current != null) current.journalOpened = true; }
    /// <summary>An animal was selected (Animal Inspection panel opens). Also completes a running discovery activity.</summary>
    public static void NoteInspected(AnimalBrain animal = null)
    {
        if (current == null) return;
        current.inspected = true;
        var want = current.findLeopard;
        if (want == null || animal == null || animal.IsPredatorSpecies != want.Value) return;
        current.findLeopard = null;
        if (Showing && current.intro) return;
        current.Say(new[] { FoundLine }, false, null, ReactionSeconds);
    }

    // ---------- Discovery activity (Free) ----------

    // "Can you find a Snow Leopard?": picks a species that is alive right now, then waits for the player to
    // select one with the normal animal click (NoteInspected). Got It hides the bubble but keeps the
    // activity going; Skip ends it. No score, no penalty.
    void StartDiscovery()
    {
        bool sheep = false, leopards = false;
        foreach (var a in AnimalBrain.All)
        {
            if (a == null || a.IsCaught) continue;
            if (a.IsPredatorSpecies) leopards = true; else sheep = true;
        }
        if (!sheep && !leopards) { Dismiss(); return; }
        bool leopard = sheep && leopards ? Random.value < 0.5f : leopards;
        findLeopard = leopard;
        Say(new[] { FindLine(leopard) }, false, null, -1f, "Skip", () => { findLeopard = null; Dismiss(); });
    }

    /// <summary>[AI-GENERATED] Claude Code (Claude Opus 5.5): season and weather news, said by the mascot instead of the
    /// centre banner. False = no mascot here (the banner shows it). Never cuts into the intro or a card.</summary>
    public static bool Announce(string text)
    {
        if (current == null || current.line == null) return false;
        // Busy (welcome, a card, Watch, the discovery game): keep the newest line and say it when free.
        if (ModalOpen() || (Showing && (current.intro || current.watchTarget != null || current.findLeopard != null))) { current.queuedNews = text; return true; }
        current.Say(new[] { text }, false, null, ReactionSeconds);
        return true;
    }

    /// <summary>[AI-GENERATED] Claude Code (Claude Opus 5.5): SCRUM-390 urgent line (fire) with Watch when a subject is given.
    /// Waits in the queue while a card is open. Falls back to the top banner when there is no mascot.</summary>
    public static void Alert(string text, Transform subject)
    {
        if (current == null || current.line == null)
        {
            if (subject != null) DramaDirector.Report(DramaDirector.Moment.Hunt, subject, text); else DramaDirector.Announce(text);
            return;
        }
        if (ModalOpen()) { current.queuedNews = text; return; }
        current.Say(new[] { text }, false, subject, ReactionSeconds * 2f);
    }

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
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): both modes; all news goes through the mascot. While a card,
        // question or the intro is up the moment is skipped (it is over by the time they close).
        if (current == null) return false;
        if (ModalOpen() || (Showing && current.intro)) return true;
        string text = null;
        switch (moment)
        {
            case DramaDirector.Moment.Birth:
                bool leopard = subject != null && subject.TryGetComponent(out AnimalBrain b) && b.IsPredatorSpecies;
                text = leopard ? LeopardBorn : SheepBorn;
                break;
            case DramaDirector.Moment.Hunt: text = Stalking; break;
            case DramaDirector.Moment.Catch: text = bannerText != null && bannerText.Contains("got away") ? Escaped : CaughtLine; break;
            default: text = bannerText; break;   // mating and anything new: the line the banner would have shown
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
            if (ScreenFlow.GuidedMode) Say(new[] { GuidedHelp }, false);
            else Say(new[] { FreeHelp }, false, null, -1f, FindButton, StartDiscovery);   // FE-M05 + discovery activity
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
