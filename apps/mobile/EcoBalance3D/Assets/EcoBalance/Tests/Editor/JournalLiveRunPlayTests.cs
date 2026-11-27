using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Plays the real game flow (menus -> simulation -> HUD -> Field Journal over the run) and checks the
// Observations graph keeps getting new days while the journal is open. Any error or exception logged
// during the run fails the test (Unity Test Framework does that by default).
public class JournalLiveRunPlayTests
{
    static readonly string[] StringKeys = { "EcoBalance.CaseJournal", CaseSeenTracker.PrefsKey, GuidedSaveKey };
    const string GuidedSaveKey = "EcoBalance.GuidedSave";   // a saved story would open "Continue?" instead of the family picker
    const string GalleryKey = "EcoBalance.GuidedEndings";
    string[] savedStrings;
    int savedGallery;
    bool hadGallery;

    // The run unlocks cases and endings for real: keep this machine's saved progress as it was.
    [SetUp]
    public void SavePrefs()
    {
        savedStrings = new string[StringKeys.Length];
        for (int i = 0; i < StringKeys.Length; i++) savedStrings[i] = PlayerPrefs.HasKey(StringKeys[i]) ? PlayerPrefs.GetString(StringKeys[i]) : null;
        hadGallery = PlayerPrefs.HasKey(GalleryKey);
        savedGallery = PlayerPrefs.GetInt(GalleryKey, 0);
        PlayerPrefs.DeleteKey(GuidedSaveKey);
    }

    [UnityTearDown]
    public IEnumerator RestorePrefs()
    {
        Time.timeScale = 1f;
        if (Application.isPlaying) yield return new ExitPlayMode();
        for (int i = 0; i < StringKeys.Length; i++)
            if (savedStrings[i] == null) PlayerPrefs.DeleteKey(StringKeys[i]); else PlayerPrefs.SetString(StringKeys[i], savedStrings[i]);
        if (hadGallery) PlayerPrefs.SetInt(GalleryKey, savedGallery); else PlayerPrefs.DeleteKey(GalleryKey);
        PlayerPrefs.Save();
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator FreeRun_GraphKeepsMovingWhileTheJournalIsOpen()
    {
        yield return new EnterPlayMode();
        SceneManager.LoadScene(ScreenFlow.Modes);
        yield return Until(() => Btn(ScreenFlow.Modes, "StartFreeBtn") != null, 20f, "mode selection screen");
        Btn(ScreenFlow.Modes, "StartFreeBtn").onClick.Invoke();
        yield return Until(() => Object.FindAnyObjectByType<ConfigurationScreen>() != null
                                 && SceneManager.GetSceneByName(ScreenFlow.Simulation).isLoaded, 30f, "configuration screen + simulation");
        yield return Wait(1f);
        Object.FindAnyObjectByType<ConfigurationScreen>().StartSimulation();
        yield return Until(() => SimHud.Active, 30f, "simulation HUD");

        // M3: after the loading screen the mascot's welcome pauses the world until the child taps through it.
        // Wait for it and tap through it like a child would.
        yield return Until(() => Mascot.Showing, 30f, "the mascot's welcome");
        Mascot.Hide();
        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play();
        speed.SetSpeed(8f);
        Time.timeScale = 4f;   // 32x: a new day on the graph every ~0.06 s of real time

        yield return OpenJournalFromHud();
        Assert.IsTrue(speed.IsRunning, "opening the journal must not pause the run (even if the mascot was talking)");
        Assert.IsFalse(FieldJournalObservations.WasGuided);

        var history = FieldJournalObservations.History;
        int before = history.Count;
        yield return Wait(15f);
        Assert.Greater(history.Count, before + 20, "the graph should keep getting new days while the journal is open");

        string summary = SummaryText();
        Debug.Log($"[JournalLiveRunPlayTests] Free run, {history.Count} days recorded. Summary card:\n{summary}");
        StringAssert.Contains("What we noticed", summary);
        StringAssert.Contains("Right now", summary);

        // The journal's own Play / Pause still stops and restarts the run.
        Btn(ScreenFlow.Journal, "GraphPlayBtn").onClick.Invoke();
        Assert.IsFalse(speed.IsRunning);
        yield return Wait(0.5f);
        int paused = history.Count;
        yield return Wait(2f);
        Assert.AreEqual(paused, history.Count, "paused from the journal: no new days");
        Btn(ScreenFlow.Journal, "GraphPlayBtn").onClick.Invoke();
        Assert.IsTrue(speed.IsRunning);

        // Back returns to the run, still running.
        Btn(ScreenFlow.Journal, "BackBtn").onClick.Invoke();
        yield return Until(() => !ScreenFlow.JournalOpen, 20f, "journal to close");
        Assert.IsTrue(speed.IsRunning, "Back keeps the run going");
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator GuidedRun_KeepsMoving_PopupsWaitForTheJournal_EndingClosesIt()
    {
        yield return new EnterPlayMode();
        SceneManager.LoadScene(ScreenFlow.Modes);
        yield return Until(() => Btn(ScreenFlow.Modes, "StartGuidedBtn") != null, 20f, "mode selection screen");
        Btn(ScreenFlow.Modes, "StartGuidedBtn").onClick.Invoke();

        // M3: the child picks whose story to follow first.
        yield return Until(() => AnyButton("Choose Miloy") != null, 20f, "family picker");
        AnyButton("Choose Miloy").onClick.Invoke();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null, 30f, "Guided simulation + HUD");

        var challenge = Object.FindAnyObjectByType<GuidedChallenge>();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var introDone = typeof(GuidedChallenge).GetField("introDone", flags);
        var showing = typeof(GuidedChallenge).GetField("showing", flags);
        var chapterOpen = typeof(GuidedChallenge).GetField("chapterOpen", flags);
        bool CardShown() => (bool)showing.GetValue(challenge) || (bool)chapterOpen.GetValue(challenge);

        // The intro card blocks the run (and the journal button) until Start.
        yield return Until(() => (bool)introDone.GetValue(challenge) || AnyButton("StartBtn") != null, 30f, "Guided intro card");
        if (!(bool)introDone.GetValue(challenge)) AnyButton("StartBtn").onClick.Invoke();
        yield return Until(() => (bool)introDone.GetValue(challenge), 10f, "intro to close");
        Mascot.Hide();   // the welcome lines (they pause the run while the mascot talks)

        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play();
        speed.SetSpeed(8f);
        Time.timeScale = 4f;

        yield return OpenJournalFromHud();
        Assert.IsTrue(FieldJournalObservations.WasGuided);
        var history = FieldJournalObservations.History;
        int before = history.Count;
        yield return Wait(15f);   // several simulated days: Guided events / a chapter card are due by now
        Assert.Greater(history.Count, before + 20, "the graph should keep getting new days while the journal is open");
        Assert.IsFalse(CardShown(), "Guided cards must wait while the journal is open");
        Assert.IsTrue(speed.IsRunning);
        Debug.Log($"[JournalLiveRunPlayTests] Guided run, {history.Count} days recorded. Summary card:\n{SummaryText()}");

        Btn(ScreenFlow.Journal, "BackBtn").onClick.Invoke();
        yield return Until(() => !ScreenFlow.JournalOpen, 20f, "journal to close");
        yield return Until(CardShown, 40f, "the waiting Guided card to show after Back");
        Assert.IsFalse(speed.IsRunning, "the card pauses the run as before");

        // The ending must not stay hidden under an open journal.
        yield return OpenJournalFromHud();
        typeof(GuidedChallenge).GetMethod("Finish", flags).Invoke(challenge, null);
        yield return Until(() => !ScreenFlow.JournalOpen, 20f, "the ending to close the journal");
    }

    // ---------- Helpers ----------

    static IEnumerator OpenJournalFromHud()
    {
        var open = Btn(SimHud.SceneName, "FieldJournalBtn");
        Assert.IsNotNull(open, "HUD has no FieldJournalBtn");
        open.onClick.Invoke();
        yield return Until(() => Object.FindAnyObjectByType<FieldJournalScreen>() != null && GameObject.Find("ObservationsPage") != null,
            30f, "Field Journal open on Observations");
        yield return null;
    }

    static string SummaryText()
    {
        var card = GameObject.Find("SummaryCard");
        Assert.IsNotNull(card, "no SummaryCard on the Observations page");
        return card.GetComponentInChildren<TextMeshProUGUI>().text;
    }

    // A button in any loaded scene or overlay canvas (the family picker and the intro card are not in one fixed scene).
    static Button AnyButton(string name)
    {
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            if (b.name == name && b.gameObject.activeInHierarchy && b.interactable) return b;
        return null;
    }

    static Button Btn(string sceneName, string name)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name.Trim().Trim('\'').Trim() == name && b.gameObject.activeInHierarchy) return b;
        return null;
    }

    static IEnumerator Wait(float realSeconds)
    {
        float end = Time.realtimeSinceStartup + realSeconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    static IEnumerator Until(System.Func<bool> done, float realSeconds, string what)
    {
        float end = Time.realtimeSinceStartup + realSeconds;
        while (!done() && Time.realtimeSinceStartup < end) yield return null;
        Assert.IsTrue(done(), $"timed out waiting for {what}");
    }
}
