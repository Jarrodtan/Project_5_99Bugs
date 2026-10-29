// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Connects the menu screens from the wireframes:
///   HomeScreen -> EcosystemSelectionScreen -> ModeSelectionScreen -> Guided (HimalayaSimulation)
///                                                                 -> Free (ConfigurationScreen)
///   Field Journal button on any menu -> FieldJournalScene, its Back button returns.
/// Buttons are found by their object names in the scenes (StartBtn, ExploreBtn, StartGuidedBtn...),
/// so the scenes need no setup; keep those names when restyling. Runs on its own at startup.
/// </summary>
public static class ScreenFlow
{
    public const string Home = "HomeScreen", Ecosystems = "EcosystemSelectionScreen", Modes = "ModeSelectionScreen",
        Journal = "FieldJournalScene", Configuration = "ConfigurationScreen", Simulation = "HimalayaSimulation";

    /// <summary>True when the player picked Guided Exploration (the simulation can ask for predictions).</summary>
    public static bool GuidedMode { get; private set; }

    static string journalReturn = Home;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // The project skips domain and scene reload on Play (Enter Play Mode Options), so statics survive
        // from the last run: start fresh.
        GuidedMode = false;
        journalReturn = Home;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // With scene reload skipped, Unity sends no sceneLoaded for the scene already open when Play starts,
    // so wire that one here (otherwise Start kept its old "go to Configuration" action).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void WireOpenScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var buttons = new Dictionary<string, List<Button>>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                if (!buttons.TryGetValue(b.name, out var list)) buttons[b.name] = list = new List<Button>();
                list.Add(b);
            }

        void On(string name, UnityAction action)
        {
            if (!buttons.TryGetValue(name, out var list)) return;
            foreach (var b in list)
            {
                // Replace whatever the button did before (e.g. Start used to open the configuration screen).
                // A brand-new click list, because switching the old Inspector actions off didn't always stick.
                // Keep the click sound: replay any AudioSource.Play the old list had.
                var sounds = new List<AudioSource>();
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    if (b.onClick.GetPersistentMethodName(i) == "Play" && b.onClick.GetPersistentTarget(i) is AudioSource src) sounds.Add(src);
                b.onClick = new Button.ButtonClickedEvent();
                foreach (var src in sounds) b.onClick.AddListener(src.Play);
                b.onClick.AddListener(action);
            }
        }

        switch (scene.name)
        {
            case Home:
                On("StartBtn", () => Load(Ecosystems));
                On("QuitBtn", Quit);
                On("FieldJournalBtn", OpenJournal);
                break;
            case Ecosystems:
                On("BackBtn", () => Load(Home));
                On("ExploreBtn", () => Load(Modes));
                On("FieldJournalBtn", OpenJournal);
                break;
            case Modes:
                On("BackBtn", () => Load(Ecosystems));
                On("StartGuidedBtn", () => StartSimulation(true));
                On("StartFreeBtn", () => StartSimulation(false));
                On("FieldJournalBtn", OpenJournal);
                break;
            case Journal:
                On("BackBtn", () => Load(journalReturn));
                break;
        }
    }

    static void Load(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    static void OpenJournal()
    {
        journalReturn = SceneManager.GetActiveScene().name;
        Load(Journal);
    }

    // Guided starts straight in the world; Free goes through the population setup first.
    static void StartSimulation(bool guided)
    {
        GuidedMode = guided;
        if (guided) UseGuidedStart();
        if (AudioManager.Instance != null) AudioManager.Instance.SwitchToSimu();
        Load(guided ? Simulation : Configuration);
    }

    // Guided Exploration always starts from the same ecosystem (Shumin's research): 10 Blue Sheep, 2 Snow Leopards.
    static void UseGuidedStart()
    {
        EcosystemSimulator.StartingPopulationOverrides.Clear();
        foreach (var n in new[] { "BlueSheep", "Blue Sheep" }) EcosystemSimulator.StartingPopulationOverrides[n] = 10;
        foreach (var n in new[] { "SnowLeopard", "Snow Leopard" }) EcosystemSimulator.StartingPopulationOverrides[n] = 2;
    }

    /// <summary>Ending screen: start a fresh Guided challenge (unlocked endings stay in the gallery).</summary>
    public static void RestartGuided()
    {
        GuidedMode = true;
        UseGuidedStart();
        Load(Simulation);
    }

    public static void GoHome() => Load(Home);

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
