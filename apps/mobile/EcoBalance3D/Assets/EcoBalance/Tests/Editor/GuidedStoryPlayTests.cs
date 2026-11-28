// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Plays the real game like a child would (taps through the real screens) and checks the task end to end:
//   Guided: three daily missions -> three badges, the ending screen (focal animal outcome, doc text, badges),
//           the Ending Gallery (locked entries can't open, unlocked ones show meaning + takeaway), and Retry
//           (Start over / Explore again keep the same story, the endings and the badges).
//   Graphs: the Observations graph draws in, can be explored with a finger, has the Food & Water view, and the
//           My Runs graph is the same interactive graph.
// With the ECOBALANCE_SHOTS environment variable set, every step is also saved as a screenshot (ScreenshotUtil).
public class GuidedStoryPlayTests
{
    static readonly string[] StringKeys = { "EcoBalance.CaseJournal", CaseSeenTracker.PrefsKey, "EcoBalance.GuidedSave" };
    static readonly string[] IntKeys = { GuidedChallenge.GalleryKey, GuidedAchievements.Key };
    string[] savedStrings; int?[] savedInts;
    string runsPath, savedRuns;

    [SetUp]
    public void SavePrefs()
    {
        savedStrings = new string[StringKeys.Length];
        for (int i = 0; i < StringKeys.Length; i++) savedStrings[i] = PlayerPrefs.HasKey(StringKeys[i]) ? PlayerPrefs.GetString(StringKeys[i]) : null;
        savedInts = new int?[IntKeys.Length];
        for (int i = 0; i < IntKeys.Length; i++) savedInts[i] = PlayerPrefs.HasKey(IntKeys[i]) ? PlayerPrefs.GetInt(IntKeys[i]) : (int?)null;
        foreach (var k in IntKeys) PlayerPrefs.DeleteKey(k);   // fresh: nothing found yet
        PlayerPrefs.DeleteKey("EcoBalance.GuidedSave");
        runsPath = Path.Combine(Application.persistentDataPath, "runs.json");
        savedRuns = File.Exists(runsPath) ? File.ReadAllText(runsPath) : null;
    }

    [UnityTearDown]
    public IEnumerator RestorePrefs()
    {
        Time.timeScale = 1f;
        if (Application.isPlaying) yield return new ExitPlayMode();
        for (int i = 0; i < StringKeys.Length; i++)
            if (savedStrings[i] == null) PlayerPrefs.DeleteKey(StringKeys[i]); else PlayerPrefs.SetString(StringKeys[i], savedStrings[i]);
        for (int i = 0; i < IntKeys.Length; i++)
            if (savedInts[i] == null) PlayerPrefs.DeleteKey(IntKeys[i]); else PlayerPrefs.SetInt(IntKeys[i], savedInts[i].Value);
        PlayerPrefs.Save();
        if (savedRuns == null) { if (File.Exists(runsPath)) File.Delete(runsPath); } else File.WriteAllText(runsPath, savedRuns);
    }

    // ---------- Guided: missions, badges, ending, gallery, retry ----------

    [UnityTest, Timeout(420000)]
    public IEnumerator GuidedStory_ThreeBadges_Ending_Gallery_Retry()
    {
        yield return new EnterPlayMode();   // must be in the test itself, not a helper
        yield return StartGuided();
        var challenge = Object.FindAnyObjectByType<GuidedChallenge>();
        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        var resolved = (HashSet<string>)Field(challenge, "resolved");
        Shot("10_guided_running");

        // Mission 1: its decision comes on Day 1, through Shu Min's event card.
        yield return Until(() => (bool)Field(challenge, "showing"), 30f, "Mission 1 decision");
        StringAssert.Contains("Mission 1", ShownEventTitle());
        Shot("11_mission1_decision");
        yield return AnswerEventCard();
        resolved.Add(GuidedAchievements.InspectStep);   // "tap an animal" (a real tap needs a screen position)
        Shot("12_mission_tracker");

        // Run on; answer whatever comes (GE events, chapter cards, Mission 2, the Day 3 question) until 3 badges.
        bool askedWrong = false, sawBadgeCard = false;
        float end = Time.realtimeSinceStartup + 240f;
        while (GuidedAchievements.UnlockedCount < 3 && Time.realtimeSinceStartup < end)
        {
            if ((bool)Field(challenge, "missionCardOpen"))
            {
                var title = Text("Mission", "Text", 1);
                if (title.Contains("How does one species"))
                {
                    if (!askedWrong)
                    {
                        yield return Wait(0.6f);   // the card pops in
                        Shot("14_question");
                        AnyButton("Answer 2").onClick.Invoke();
                        StringAssert.Contains("Not quite", MissionBody(challenge), "a wrong answer gets a hint, never a fail");
                        yield return Wait(0.2f);
                        Shot("15_question_hint");
                        askedWrong = true;
                        yield return null;
                    }
                    AnyButton("Answer 1").onClick.Invoke();
                    yield return null;
                    StringAssert.Contains("Each species in the food web", MissionBody(challenge));
                }
                else
                {
                    if (!sawBadgeCard) { yield return Wait(0.6f); Shot("13_badge_card"); sawBadgeCard = true; }
                    StringAssert.Contains("MISSION", Text("Mission", "Text", 0));
                }
                AnyButton("Answer 1").onClick.Invoke();   // Continue / Yay! Continue
                yield return null;
                continue;
            }
            if ((bool)Field(challenge, "showing")) { yield return AnswerEventCard(); continue; }
            if ((bool)Field(challenge, "chapterOpen")) { TapChapterContinue(); yield return null; continue; }
            if (Mascot.Showing) Mascot.Hide();
            if (!speed.IsRunning && !GuidedChallenge.PopupOpen) speed.Play();
            yield return null;
        }
        Assert.AreEqual(3, GuidedAchievements.UnlockedCount, "all three daily missions unlock their badges");
        Assert.IsTrue(askedWrong, "the Day 3 question was asked");
        Assert.IsTrue(resolved.Contains(GuidedAchievements.SafetyDecision), "Mission 2's survival decision was made");

        // The ending (evaluated now, as at the last day): Shu Min's ending screen with the doc's text.
        typeof(GuidedChallenge).GetMethod("Finish", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(challenge, null);
        yield return Until(() => SceneManager.GetSceneByName(GuidedScreens.EndingScene).isLoaded, 20f, "ending screen");
        yield return Wait(0.6f);
        Shot("20_ending_confetti");
        yield return Wait(2.5f);
        Shot("21_ending");
        var result = GuidedChallenge.LastResult;
        var ending = GuidedEndings.All[result.ending];
        Assert.AreEqual(GuidedEndings.Classify(result.sheep, result.leopards, result.vegetation), result.ending, "one ending, by the doc's priority rules");
        StringAssert.Contains("Miloy", SceneText(GuidedScreens.EndingScene, "CompletionSubtitleTxt"), "says how Miloy did");
        Assert.AreEqual(ending.description, SceneText(GuidedScreens.EndingScene, "OutcomeDescriptionTxt"));
        Assert.AreEqual(ending.takeaway, SceneText(GuidedScreens.EndingScene, "LearningDescriptionTxt"));
        StringAssert.Contains("1 / 5 Endings Discovered", SceneText(GuidedScreens.EndingScene, "ProgressTxt"));
        Assert.AreEqual(3, GameObject.Find("BadgeRow").transform.childCount, "the three badges on the ending screen");
        Assert.AreEqual(3, result.newBadges.Length, "all three are new in this run");

        // Gallery: open it from the ending screen; a locked card doesn't open, the unlocked one shows its details.
        SceneButton(GuidedScreens.EndingScene, "ViewEndingGalleryBtn").onClick.Invoke();
        yield return Until(() => SceneManager.GetSceneByName(GuidedScreens.GalleryScene).isLoaded, 20f, "gallery");
        yield return Wait(0.5f);
        Shot("30_gallery");
        var detail = GameObject.Find("GalleryDetailPanel");
        int unlockedSlot = System.Array.IndexOf(GuidedEndings.GalleryOrder, result.ending);
        int lockedSlot = unlockedSlot == 0 ? 1 : 0;
        CardButton(lockedSlot).onClick.Invoke();
        yield return null;
        Assert.IsTrue(detail == null || !detail.activeInHierarchy, "a locked ending can't be opened");
        if (unlockedSlot >= 3) { var r = SceneButton(GuidedScreens.GalleryScene, "RArrowBtn"); for (int i = 0; i < unlockedSlot - 2 && r != null; i++) r.onClick.Invoke(); yield return null; }
        CardButton(unlockedSlot).onClick.Invoke();
        yield return Wait(0.3f);
        detail = FindInScene(GuidedScreens.GalleryScene, "GalleryDetailPanel");
        Assert.IsTrue(detail.activeInHierarchy, "an unlocked ending opens");
        Assert.AreEqual(ending.meaning, TextUnder(detail.transform, "ExplanationBox01", "WhatThisMeansTxt (1)"), "What this means");
        Assert.AreEqual(ending.takeaway, TextUnder(detail.transform, "ExplanationBox02", "WhatThisMeansTxt (1)"), "What did we learn");
        Shot("31_gallery_detail");

        // Retry: Explore again = the same story from Day 1; endings and badges stay.
        SceneManager.UnloadSceneAsync(GuidedScreens.GalleryScene);
        yield return Until(() => !SceneManager.GetSceneByName(GuidedScreens.GalleryScene).isLoaded, 10f, "gallery to close");
        SceneButton(GuidedScreens.EndingScene, "ExploreAgainBtn").onClick.Invoke();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null && Object.FindAnyObjectByType<GuidedChallenge>() != challenge, 40f, "the story to start again");
        Assert.AreEqual(FamilyStory.BlueSheep, FamilyManager.SelectedStory, "same story");
        Assert.AreEqual(3, GuidedAchievements.UnlockedCount, "badges kept after Replay");
        Assert.IsTrue(GuidedEndings.IsUnlocked(result.ending), "endings kept after Replay");
        Assert.AreEqual(1, GuidedChapters_CurrentChapter(), "back to the first chapter");
        var clock = Object.FindAnyObjectByType<ClockService>();
        Assert.AreEqual(1, clock.Day, "back to Day 1");

        // Start over is also in the Leave box once the story is under way (before Start, Back just leaves).
        var replay = Object.FindAnyObjectByType<GuidedChallenge>();
        yield return Until(() => (bool)Field(replay, "introDone") || AnyButton("StartBtn") != null, 40f, "intro card again");
        if (!(bool)Field(replay, "introDone")) AnyButton("StartBtn").onClick.Invoke();
        yield return Until(() => (bool)Field(replay, "introDone"), 10f, "intro to close again");
        yield return Wait(0.5f);
        Mascot.Hide();
        ScreenFlow.ShowLeaveConfirm();
        yield return Wait(0.3f);
        Assert.IsTrue(ScreenFlow.LeaveOpen, "Leave box open");
        Shot("40_leave_box");
        var startOver = AnyButton("Start over");
        Assert.IsNotNull(startOver, "Restart Story in the Leave box");
        startOver.onClick.Invoke();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null && Object.FindAnyObjectByType<GuidedChallenge>() != replay, 40f, "Start over to restart the story");
        Assert.AreEqual(FamilyStory.BlueSheep, FamilyManager.SelectedStory, "Start over keeps the story");
        Assert.AreEqual(1, Object.FindAnyObjectByType<ClockService>().Day, "Start over goes back to Day 1");
        Assert.AreEqual(3, GuidedAchievements.UnlockedCount, "badges kept after Start over");
        Assert.IsTrue(GuidedEndings.IsUnlocked(result.ending), "endings kept after Start over");
    }

    // ---------- Guided: Snowy's story, the focal animal dies early ----------

    [UnityTest, Timeout(300000)]
    public IEnumerator SnowyStory_FocalDeath_EndsEarly_KindLine_Snow_StartOver()
    {
        yield return new EnterPlayMode();
        yield return StartGuided("Snowy");
        var challenge = Object.FindAnyObjectByType<GuidedChallenge>();
        Assert.AreEqual(FamilyStory.SnowLeopard, FamilyManager.SelectedStory);
        Assert.AreEqual("Snowy", GuidedChallenge.FocalName);

        // Mission 1 talks about Snowy's family (her story's wording, not Miloy's herd).
        yield return Until(() => (bool)Field(challenge, "showing"), 30f, "Mission 1 decision");
        StringAssert.Contains("Mission 1", ShownEventTitle());
        var card = (Transform)Field(challenge, "eventCard");
        StringAssert.Contains("Snowy and her family", TextUnder(card, "EventDescriptionTxt", "EventDescriptionTxt"));
        yield return AnswerEventCard();
        yield return Wait(0.5f);

        // Snowy runs out of food: the story ends now, by itself (doc: evaluated when the focal animal dies).
        var snowy = FamilyManager.MainAnimal;
        Assert.IsNotNull(snowy, "Snowy is alive at the start");
        snowy.Brain.curr_hunger = 0f;
        typeof(FamilyManager).GetMethod("OnStarved", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(FamilyManager.Instance, new object[] { snowy.Brain });
        Assert.IsTrue(FamilyManager.MainAnimalDead);
        yield return Until(() => SceneManager.GetSceneByName(GuidedScreens.EndingScene).isLoaded, 20f, "the early ending");
        yield return Wait(0.6f);
        Shot("60_snowy_early_ending");
        var result = GuidedChallenge.LastResult;
        Assert.LessOrEqual(result.day, 2, "ended early, not at the last day");
        string line = SceneText(GuidedScreens.EndingScene, "CompletionSubtitleTxt");
        StringAssert.Contains("Snowy could not find enough food", line);
        StringAssert.Contains("ended early", line);
        StringAssert.DoesNotContain("you", line.ToLowerInvariant(), "never blames the player");
        Assert.AreEqual(GuidedEndings.All[result.ending].description, SceneText(GuidedScreens.EndingScene, "OutcomeDescriptionTxt"));
        Assert.IsTrue(GuidedEndings.IsUnlocked(result.ending), "the ending is in the gallery");
        Assert.IsTrue(result.focalDied);
        Assert.IsNotNull(GameObject.Find("Snowfall"), "Snowy died: gentle snow, never a confetti party");
        Assert.IsNull(GameObject.Find("Confetti"));
        yield return Wait(2f);
        Shot("61_snowy_early_ending");

        // Explore again: Snowy's story from Day 1 again, her ending kept.
        SceneButton(GuidedScreens.EndingScene, "ExploreAgainBtn").onClick.Invoke();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null && Object.FindAnyObjectByType<GuidedChallenge>() != challenge, 40f, "the story to start again");
        Assert.AreEqual(FamilyStory.SnowLeopard, FamilyManager.SelectedStory, "same story");
        Assert.AreEqual(1, Object.FindAnyObjectByType<ClockService>().Day, "back to Day 1");
        Assert.IsTrue(GuidedEndings.IsUnlocked(result.ending), "ending kept after Replay");
        yield return Until(() => FamilyManager.MainAnimal != null, 40f, "a new, living Snowy");
        Assert.IsFalse(FamilyManager.MainAnimalDead, "Snowy is back");
    }

    // ---------- Graphs ----------

    [UnityTest, Timeout(300000)]
    public IEnumerator Graphs_DrawIn_Explore_FoodAndWater_MyRuns()
    {
        // A saved run for the My Runs page.
        var run = new MyRuns { id = "test", start = "2026-11-28T10:00:00", mode = "Free Exploration", days_survived = 12, outcome = "BothSurvived", stars = 2,
                               peaksheep = 16, peakleopard = 3, endsheep = 11, endleopard = 2, endtrees = 40 };
        for (int i = 0; i < 12; i++) { run.t.Add(i * 30); run.days.Add(i + 1); run.sheep.Add(10 + (int)(6 * Mathf.Sin(i * 0.5f))); run.leopard.Add(2 + i % 2); run.trees.Add(40 - i); }
        RunHist.Add(run);

        yield return new EnterPlayMode();
        SceneManager.LoadScene(ScreenFlow.Modes);
        yield return Until(() => SceneButton(ScreenFlow.Modes, "StartFreeBtn") != null, 20f, "mode selection");
        SceneButton(ScreenFlow.Modes, "StartFreeBtn").onClick.Invoke();
        yield return Until(() => Object.FindAnyObjectByType<ConfigurationScreen>() != null && SceneManager.GetSceneByName(ScreenFlow.Simulation).isLoaded, 30f, "configuration");
        yield return Wait(1f);
        Object.FindAnyObjectByType<ConfigurationScreen>().StartSimulation();
        yield return Until(() => SimHud.Active, 30f, "HUD");
        yield return Until(() => Mascot.Showing, 30f, "mascot welcome");
        Mascot.Hide();
        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play(); speed.SetSpeed(8f); Time.timeScale = 4f;
        yield return RunFor(14f, speed);   // a few days on the graph
        Shot("50_free_run");

        SceneButton(SimHud.SceneName, "FieldJournalBtn").onClick.Invoke();
        yield return Until(() => Object.FindAnyObjectByType<FieldJournalScreen>() != null && GameObject.Find("ObservationsPage") != null, 30f, "journal");
        var screen = Object.FindAnyObjectByType<FieldJournalScreen>();
        screen.Show(FieldJournalScreen.Page.Observations);
        var graph = GameObject.Find("ObservationsPage").GetComponentInChildren<PopulationGraph>();
        yield return null;
        Assert.Less(graph.Reveal, 1f, "the lines draw themselves in");
        yield return Wait(0.3f);
        Shot("51_graph_drawing_in");
        yield return Wait(1.5f);
        Assert.AreEqual(1f, graph.Reveal);
        Shot("52_graph_animals");

        graph.ScrubAt(0.5f);
        Assert.GreaterOrEqual(graph.ScrubIndex, 0);
        StringAssert.Contains("Blue Sheep", graph.ScrubText, "the explore card lists the lines on that day");
        yield return null;
        Shot("53_graph_explore");
        graph.HideScrub();

        screen.ShowView(true);
        yield return Wait(1.6f);
        Shot("54_graph_food_water");
        graph.ScrubAt(1f);
        StringAssert.Contains("%", graph.ScrubText);
        yield return null;
        Shot("55_graph_food_explore");
        graph.HideScrub();

        screen.Show(FieldJournalScreen.Page.Runs);
        yield return Wait(1.5f);
        var runGraph = GameObject.Find("RunGraph").GetComponentInChildren<PopulationGraph>();
        Assert.AreEqual(3, runGraph.Series.Count, "Trees, Blue Sheep, Snow Leopards");
        Assert.AreEqual(12, runGraph.History.Count);
        runGraph.ScrubAt(0.25f);
        StringAssert.Contains("Day", runGraph.ScrubText);
        yield return null;
        Shot("56_my_runs_graph");
    }

    // ---------- Flow helpers ----------

    IEnumerator StartGuided(string animal = "Miloy")
    {
        SceneManager.LoadScene(ScreenFlow.Modes);
        yield return Until(() => SceneButton(ScreenFlow.Modes, "StartGuidedBtn") != null, 20f, "mode selection");
        SceneButton(ScreenFlow.Modes, "StartGuidedBtn").onClick.Invoke();
        yield return Until(() => AnyButton("Choose " + animal) != null, 20f, "family picker");
        yield return Wait(0.5f);
        Shot("01_family_picker");
        AnyButton("Choose " + animal).onClick.Invoke();
        yield return Until(() => SimHud.Active && Object.FindAnyObjectByType<GuidedChallenge>() != null, 40f, "Guided run");
        var challenge = Object.FindAnyObjectByType<GuidedChallenge>();
        yield return Until(() => (bool)Field(challenge, "introDone") || AnyButton("StartBtn") != null, 40f, "intro card");
        yield return Wait(0.5f);
        Shot("02_intro_card");
        if (!(bool)Field(challenge, "introDone")) AnyButton("StartBtn").onClick.Invoke();
        yield return Until(() => (bool)Field(challenge, "introDone"), 10f, "intro to close");
        yield return Wait(0.5f);
        Mascot.Hide();
        var speed = Object.FindAnyObjectByType<SimulationSpeedController>();
        speed.Play(); speed.SetSpeed(8f); Time.timeScale = 4f;
    }

    // Shu Min's event card: pick the first option, Confirm, then Continue on the feedback card (or the code-built popup).
    IEnumerator AnswerEventCard()
    {
        var select = AnyButton("SelectionBtn");
        if (select != null)
        {
            select.onClick.Invoke();
            yield return null;
            AnyButton("ConfirmBtn")?.onClick.Invoke();
            yield return null;
            var cont = AnyButton("ContinueBtn");
            if (cont != null) cont.onClick.Invoke();
        }
        else AnyButton("Choice 1")?.onClick.Invoke();
        yield return null;
    }

    static void TapChapterContinue()
    {
        foreach (var name in new[] { "ContinueBtn", "Continue", "NextBtn", "StartBtn" })
        {
            var b = AnyButton(name);
            if (b != null) { b.onClick.Invoke(); return; }
        }
    }

    static int GuidedChapters_CurrentChapter() => GuidedChallenge.CurrentChapter;

    string ShownEventTitle()
    {
        // The card the challenge fills (Shu Min renamed it to "EventCard-DecisionMission" on 27 Nov).
        var t = (Transform)Field(Object.FindAnyObjectByType<GuidedChallenge>(), "eventCard");
        if (t != null && t.gameObject.activeInHierarchy) foreach (var tmp in t.GetComponentsInChildren<TMP_Text>()) if (tmp.name.StartsWith("FeedbackTitleTxt")) return tmp.text;
        var popup = GameObject.Find("Decision");
        return popup != null ? popup.GetComponentInChildren<TMP_Text>().text : "";
    }

    static string MissionBody(GuidedChallenge c) => ((TextMeshProUGUI)Field(c, "missionBody")).text;

    static object Field(object o, string name) =>
        o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);

    static Button CardButton(int slot) => FindInScene(GuidedScreens.GalleryScene, $"EndingCard0{slot + 1}").GetComponent<Button>();

    static void Shot(string name) => ScreenshotUtil.Capture(name);

    // ---------- Finding things ----------

    static Button AnyButton(string name)
    {
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            if (b.name.Trim().Trim('\'').Trim() == name && b.gameObject.activeInHierarchy && b.interactable) return b;
        return null;
    }

    static Button SceneButton(string sceneName, string name)
    {
        var go = FindInScene(sceneName, name);
        return go != null && go.activeInHierarchy ? go.GetComponent<Button>() : null;
    }

    static GameObject FindInScene(string sceneName, string name)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Trim().Trim('\'').Trim() == name) return t.gameObject;
        return null;
    }

    static string SceneText(string sceneName, string name)
    {
        var go = FindInScene(sceneName, name);
        return go != null && go.TryGetComponent(out TMP_Text t) ? t.text : "";
    }

    static string TextUnder(Transform root, string box, string name)
    {
        foreach (var b in root.GetComponentsInChildren<Transform>(true))
            if (b.name == box)
                foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name == name) return t.text;
        return "";
    }

    // The n-th text directly under the mission card ("Mission" panel > Card).
    static string Text(string panel, string childName, int index)
    {
        var p = GameObject.Find(panel);
        if (p == null) return "";
        var card = p.transform.Find("Card");
        int k = 0;
        foreach (Transform t in card)
            if (t.name == childName && t.TryGetComponent(out TMP_Text tmp)) { if (k++ == index) return tmp.text; }
        return "";
    }

    // Lets the run play (a mascot line pauses the world until tapped: tap through it like a child would).
    static IEnumerator RunFor(float realSeconds, SimulationSpeedController speed)
    {
        float end = Time.realtimeSinceStartup + realSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            if (Mascot.Showing) Mascot.Hide();
            if (!speed.IsRunning) speed.Play();
            yield return null;
        }
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
