// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Plays a real Free Exploration run, opens the Field Journal over it and switches the graph to Food & Water:
// the grass / water lines, the "eaten in the last day" tiles and the famine / thirst meters must follow what
// the animals really eat and drink. Any error or exception logged during the run fails the test.
public class FoodWaterGraphPlayTests
{
    static readonly string[] StringKeys = { "EcoBalance.CaseJournal", CaseSeenTracker.PrefsKey };
    string[] savedStrings;

    [SetUp]
    public void SavePrefs()
    {
        savedStrings = new string[StringKeys.Length];
        for (int i = 0; i < StringKeys.Length; i++) savedStrings[i] = PlayerPrefs.HasKey(StringKeys[i]) ? PlayerPrefs.GetString(StringKeys[i]) : null;
    }

    [UnityTearDown]
    public IEnumerator RestorePrefs()
    {
        Time.timeScale = 1f;
        if (Application.isPlaying) yield return new ExitPlayMode();
        for (int i = 0; i < StringKeys.Length; i++)
            if (savedStrings[i] == null) PlayerPrefs.DeleteKey(StringKeys[i]); else PlayerPrefs.SetString(StringKeys[i], savedStrings[i]);
        PlayerPrefs.Save();
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator FreeRun_FoodAndWaterView_FollowsWhatTheAnimalsEatAndDrink()
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
        Time.timeScale = 4f;

        Assert.IsNotNull(FieldJournalObservations.FoodSeries, "the run has a Food & Water view");
        var meter = Object.FindAnyObjectByType<ResourceUseMeter>();
        Assert.IsNotNull(meter, "the Food & Water meter is installed with the recorder");

        // Open the journal on Observations and switch to Food & Water.
        Btn(SimHud.SceneName, "FieldJournalBtn").onClick.Invoke();
        yield return Until(() => Object.FindAnyObjectByType<FieldJournalScreen>() != null && GameObject.Find("ObservationsPage") != null, 30f, "Field Journal");
        yield return null;
        var screen = Object.FindAnyObjectByType<FieldJournalScreen>();
        var graph = Object.FindAnyObjectByType<PopulationGraph>();
        Assert.IsFalse(screen.FoodViewShown, "opens on the Animals view");
        StringAssert.Contains("Right now", SummaryText(), "the Animals summary is unchanged");
        Btn(ScreenFlow.Journal, "FoodWaterViewBtn").onClick.Invoke();
        Assert.IsTrue(screen.FoodViewShown);
        Assert.AreEqual(FoodWater.GrassLeft, graph.Series[0].Key, "the graph now draws the Food & Water lines");
        Assert.AreEqual("%", graph.valueSuffix);

        // Animals get hungry over a couple of days, then graze and drink: the totals and lines follow.
        var history = FieldJournalObservations.History;
        yield return Until(() => meter.GrassEatenTotal > 0f || meter.WaterDrunkTotal > 0f, 90f, "the animals to eat or drink");
        yield return Until(() => history.Latest.Has(FoodWater.GrassLeft) && history.Latest.Has(FoodWater.Famine), 10f, "a day with Food & Water values");
        yield return Wait(1f);
        var last = history.Latest;
        Assert.That(last.Get(FoodWater.GrassLeft), Is.InRange(0f, 100f));
        Assert.That(last.Get(FoodWater.Famine), Is.InRange(0f, 100f));
        Assert.That(last.Get(FoodWater.GrassEatenTotal) + last.Get(FoodWater.WaterDrunkTotal), Is.GreaterThan(0f), "eating is recorded on the graph's days");

        string summary = SummaryText();
        Debug.Log($"[FoodWaterGraphPlayTests] {history.Count} days. Food & Water summary:\n{summary}\nFamine: {Text("FamineMeterBand")}  Thirst: {Text("ThirstMeterBand")}\n" +
                  $"Eaten: {meter.GrassEatenTotal:0.00} grass patches ({last.Get(FoodWater.GrassEaten):0.0}% in the last day), drunk: {meter.WaterDrunkTotal:0.00} water pools ({last.Get(FoodWater.WaterDrunk):0.0}% in the last day)");
        StringAssert.Contains("Grass left", summary);
        StringAssert.Contains("What we noticed", summary);
        StringAssert.DoesNotContain("Clue to watch for", summary, "the Food & Water card stays short enough to read");
        StringAssert.Contains("%", Text("FamineMeterBand"), "the famine meter shows a level");
        StringAssert.Contains("%", Text("ThirstMeterBand"));
        var tile = GameObject.Find("GrassTile");
        Assert.IsNotNull(tile);
        StringAssert.EndsWith("%", tile.GetComponentsInChildren<TMP_Text>()[0].text, "the grass tile shows how much was eaten");

        // Back to Animals: the counts come back.
        Btn(ScreenFlow.Journal, "AnimalsViewBtn").onClick.Invoke();
        Assert.IsFalse(screen.FoodViewShown);
        Assert.IsFalse(FoodWater.IsKey(graph.Series[0].Key));
        Assert.IsFalse(GameObject.Find("FamineMeter") != null && GameObject.Find("FamineMeter").activeInHierarchy, "the meters belong to Food & Water");

        Btn(ScreenFlow.Journal, "BackBtn").onClick.Invoke();
        yield return Until(() => !ScreenFlow.JournalOpen, 20f, "journal to close");
    }

    // ---------- Helpers ----------

    static string SummaryText() => GameObject.Find("SummaryCard").GetComponentInChildren<TextMeshProUGUI>().text;

    static string Text(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<TMP_Text>().text : "";
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
