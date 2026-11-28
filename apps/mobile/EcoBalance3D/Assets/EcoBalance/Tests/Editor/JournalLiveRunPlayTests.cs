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
    static readonly string[] StringKeys = { "EcoBalance.CaseJournal", CaseSeenTracker.PrefsKey };
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

        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play();
        speed.SetSpeed(8f);
        Time.timeScale = 4f;   // 32x: a new day on the graph every ~0.06 s of real time

        yield return OpenJournalFromHud();
        Assert.IsTrue(speed.IsRunning, "opening the journal must not pause the run");
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
        // The Guided button now opens the family picker (or Continue / New Story): start a fresh story directly.
        ScreenFlow.RestartGuided();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null, 30f, "Guided simulation + HUD");
        // The run waits, paused, on the intro card until Start.
        yield return Until(() => Btn(SimHud.SceneName, "StartBtn") != null || !GuidedChallenge.IntroPending, 30f, "Guided intro card");
        Btn(SimHud.SceneName, "StartBtn")?.onClick.Invoke();
        yield return Until(() => !GuidedChallenge.IntroPending, 10f, "the Guided story to start");

        var challenge = Object.FindAnyObjectByType<GuidedChallenge>();
        var popup = (GameObject)typeof(GuidedChallenge).GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(challenge);
        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play();
        speed.SetSpeed(8f);
        Time.timeScale = 4f;

        yield return OpenJournalFromHud();
        Assert.IsTrue(FieldJournalObservations.WasGuided);
        var history = FieldJournalObservations.History;
        int before = history.Count;
        yield return Wait(15f);   // several simulated days: Guided events are due by now
        Assert.Greater(history.Count, before + 20, "the graph should keep getting new days while the journal is open");
        Assert.IsFalse(popup.activeSelf, "a Guided decision must wait while the journal is open");
        Assert.IsTrue(speed.IsRunning);
        Debug.Log($"[JournalLiveRunPlayTests] Guided run, {history.Count} days recorded. Summary card:\n{SummaryText()}");

        Btn(ScreenFlow.Journal, "BackBtn").onClick.Invoke();
        yield return Until(() => !ScreenFlow.JournalOpen, 20f, "journal to close");
        yield return Until(() => popup.activeSelf, 40f, "the waiting Guided decision to show after Back");
        Assert.IsFalse(speed.IsRunning, "the decision pauses the run as before");

        // The ending must not stay hidden under an open journal.
        popup.SetActive(false);
        yield return OpenJournalFromHud();
        typeof(GuidedChallenge).GetMethod("Finish", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(challenge, null);
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

    static Button Btn(string sceneName, string name)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name.Trim().Trim('\'').Trim() == name && b.gameObject.activeInHierarchy) return b;
        return null;
    }

    // The mascot pauses the run while it talks (M3 feedback) and waits for Got It: tap it away like a player would.
    static void DismissMascot() { if (Mascot.Showing) Mascot.Hide(); }

    static IEnumerator Wait(float realSeconds)
    {
        float end = Time.realtimeSinceStartup + realSeconds;
        while (Time.realtimeSinceStartup < end) { DismissMascot(); yield return null; }
    }

    static IEnumerator Until(System.Func<bool> done, float realSeconds, string what)
    {
        float end = Time.realtimeSinceStartup + realSeconds;
        while (!done() && Time.realtimeSinceStartup < end) { DismissMascot(); yield return null; }
        Assert.IsTrue(done(), $"timed out waiting for {what}");
    }
}
