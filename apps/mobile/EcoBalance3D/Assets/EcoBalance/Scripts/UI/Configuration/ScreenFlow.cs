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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

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
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    b.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
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
        if (AudioManager.Instance != null) AudioManager.Instance.SwitchToSimu();
        Load(guided ? Simulation : Configuration);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
