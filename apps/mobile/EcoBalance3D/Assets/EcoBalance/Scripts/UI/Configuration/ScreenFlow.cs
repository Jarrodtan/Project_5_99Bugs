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
                if (mode == LoadSceneMode.Additive) { SetUpJournalOverlay(scene); On("BackBtn", CloseJournalOverlay); }
                else On("BackBtn", () => Load(journalReturn));
                FieldJournalScreen.Attach(scene);   // tabs, Investigations and Observations pages
                break;
            case Simulation:
                FieldJournalInstaller.InstallInSimulation(scene);   // history graph + "New case discovered" popup
                if (mode == LoadSceneMode.Single) SimHud.Enter();   // Shumin's HUD on top; not when Configuration loads it behind itself
                else SimHud.HideOldHud(false);                    // Free: hide the old buttons while choosing animals
                break;
        }

        // the Back buttons are blank white squares in every scene (no icon yet)
        if (buttons.TryGetValue("BackBtn", out var backs))
            foreach (var b in backs) ButtonCaption.Ensure(b, "‹ Back", 26f);
    }

    // Back button on the simulation (both modes), bottom-left above the weather text. Asks first, so a
    // child doesn't lose a Guided challenge by accident, and pauses while asking.
    /// <summary>Adds the simulation's Back button (once per scene). Free mode calls it when Configuration starts the run.</summary>
    public static void AddLeaveButton(bool ownButton = true)
    {
        if (Object.FindFirstObjectByType<LeaveMarker>() != null) return;
        var canvasGo = new GameObject("[Leave Canvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LeaveMarker));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;

        var confirm = Rect(canvasGo.transform, "Confirm", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        var card = Rect(confirm.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 420f), new Color(0.97f, 0.97f, 0.95f, 1f));
        Text(card.transform, GuidedMode ? "Leave the challenge?\n<size=70%>Your progress in this run will be lost.</size>" : "Leave the simulation?", 48f, new Color(0.12f, 0.16f, 0.2f), new Vector2(0f, 60f), new Vector2(820f, 200f));
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        bool wasRunning = false;
        Btn(card.transform, "Stay", new Vector2(-200f, -110f), new Color(0.3f, 0.62f, 0.38f), () => { confirm.SetActive(false); if (wasRunning && speed != null) speed.Play(); });
        Btn(card.transform, "Leave", new Vector2(200f, -110f), new Color(0.55f, 0.55f, 0.55f), () => Load(Modes));
        confirm.SetActive(false);
        openConfirm = () =>
        {
            wasRunning = speed != null && speed.IsRunning;
            if (speed != null) speed.Pause();
            confirm.SetActive(true);
            confirm.transform.SetAsLastSibling();
        };
        if (!ownButton) return;   // Shumin's HUD has its own BackBtn, which calls ShowLeaveConfirm

        var back = Btn(canvasGo.transform, "Back", Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.8f), () =>
        {
            wasRunning = speed != null && speed.IsRunning;
            if (speed != null) speed.Pause();
            confirm.SetActive(true);
            confirm.transform.SetAsLastSibling();
        });
        var brt = (RectTransform)back.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 0f);
        brt.anchoredPosition = new Vector2(30f, 150f);
        brt.sizeDelta = new Vector2(220f, 80f);
    }

    class LeaveMarker : MonoBehaviour { }
    static System.Action openConfirm;

    /// <summary>Opens the "Leave?" box (Back, Field Journal and View More in the simulation HUD).</summary>
    public static void ShowLeaveConfirm() { if (openConfirm != null) openConfirm(); else Load(Modes); }

    static GameObject Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject Btn(Transform parent, string label, Vector2 pos, Color color, UnityAction onClick)
    {
        var go = Rect(parent, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(320f, 100f), color);
        go.AddComponent<Button>().onClick.AddListener(onClick);
        Text(go.transform, label, 38f, Color.white, Vector2.zero, Vector2.zero, true);
        return go;
    }

    static void Text(Transform parent, string text, float size, Color color, Vector2 pos, Vector2 box, bool fill = false)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var rt = t.rectTransform;
        if (fill) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        else { rt.anchoredPosition = pos; rt.sizeDelta = box; }
        t.text = text; t.fontSize = size; t.color = color; t.raycastTarget = false;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
    }

    // ---------- Field Journal on top of a running simulation ----------
    static bool journalWasRunning;

    /// <summary>Opens the Field Journal over the simulation, paused; its Back button returns to the same run.</summary>
    public static void OpenJournalOverlay()
    {
        if (SceneManager.GetSceneByName(Journal).isLoaded) return;
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        journalWasRunning = speed != null && speed.IsRunning;
        if (speed != null) speed.Pause();
        SceneManager.LoadSceneAsync(Journal, LoadSceneMode.Additive);
    }

    // The journal scene is a full screen of its own: drop its camera, light and extra EventSystem, draw it on top.
    static void SetUpJournalOverlay(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null) { root.SetActive(false); continue; }
            if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) { root.SetActive(false); continue; }
            foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                if (c.isRootCanvas) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 90; }
        }
    }

    static void CloseJournalOverlay()
    {
        SceneManager.UnloadSceneAsync(Journal);
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        if (journalWasRunning && speed != null) speed.Play();
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
