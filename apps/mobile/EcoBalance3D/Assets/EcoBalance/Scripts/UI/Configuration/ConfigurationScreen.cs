using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigurationScreen : MonoBehaviour
{
    public SpeciesCounter[] counters;

    [Tooltip("Scene containing the ecosystem.")]
    public string simulationSceneName = "HimalayaSimulation";

    [Tooltip("The whole configuration menu that disappears after Start.")]
    public GameObject configurationUI;

    [Tooltip("Round arrow art of the other screens' Back buttons.")]
    public Sprite backSprite;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
    public Sprite backArrowSprite;   // the arrow drawn on top of the round button

    /// <summary>[AI-GENERATED] Claude Code (Claude Opus 5.5): true while the Free setup menu is on screen. The world behind it
    /// keeps its live weather line, season pill, season message and Edit Terrain hidden until Start (the run shows them).</summary>
    public static bool Open { get; private set; }

    private void Start()
    {
        Open = configurationUI == null || configurationUI.activeSelf;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.simu.TransitionTo(0f);
        }

        AddBackButton();

        // Pause the game while the player is configuring the ecosystem.
        Time.timeScale = 0f;

        // Load the ecosystem behind the configuration screen.
        if (!SceneManager.GetSceneByName(simulationSceneName).isLoaded)
        {
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): remove duplicates once it has loaded.
            SceneManager.sceneLoaded += OnSimulationSceneLoaded;
            SceneManager.LoadScene(
                simulationSceneName,
                LoadSceneMode.Additive
            );
        }
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Both scenes have their own EventSystem and camera AudioListener. With the simulation
    // scene loaded on top, Unity warns every frame about having two of each, so keep one:
    // this scene's EventSystem (it drives the menu) and the simulation camera's listener.
    void OnSimulationSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != simulationSceneName) return;
        SceneManager.sceneLoaded -= OnSimulationSceneLoaded;

        foreach (var root in scene.GetRootGameObjects())
            foreach (var es in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                es.gameObject.SetActive(false);

        foreach (var listener in gameObject.scene.GetRootGameObjects())
            foreach (var al in listener.GetComponentsInChildren<AudioListener>(true))
                al.enabled = false;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSimulationSceneLoaded; Open = false; }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5): Back to Mode Selection before Start, like the
    // Guided intro card. Sits in the setup menu, so it goes away with it on Start (the HUD's Back takes over).
    void AddBackButton()
    {
        var parent = configurationUI != null ? configurationUI.transform : transform;
        var canvas = parent.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (parent.GetComponent<RectTransform>() == null) parent = canvas.transform;
        var go = new GameObject("BackBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -30f);
        rt.sizeDelta = new Vector2(120f, 128f);
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = backSprite;
        img.preserveAspect = true;
        if (backSprite == null) img.color = new Color(0.98f, 0.94f, 0.84f);
        if (backArrowSprite != null)
        {
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            arrow.transform.SetParent(go.transform, false);
            var art = (RectTransform)arrow.transform;
            art.anchorMin = art.anchorMax = art.pivot = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(72f, 58f);   // same share of the circle as on Mode Selection (81 x 65 in 135 x 144)
            var ai = arrow.GetComponent<UnityEngine.UI.Image>();
            ai.sprite = backArrowSprite;
            ai.preserveAspect = true;
            ai.raycastTarget = false;
        }
        go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;   // Open stays true until this scene is gone, so the run's pills don't flash
            SceneManager.LoadScene(ScreenFlow.Modes);
        });
    }
    // [AI-GENERATED END]

    public void StartSimulation()
    {
        // --------------------------------
        // 1. Save chosen populations
        // --------------------------------

        Open = false;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): the run's HUD bits may show now
        EcosystemSimulator.StartingPopulationOverrides.Clear();

        foreach (var counter in counters)
        {
            if (counter != null && counter.species != null)
            {
                EcosystemSimulator.StartingPopulationOverrides[
                    counter.species.name
                ] = counter.Count;
            }
        }

        // --------------------------------
        // 2. Reset ecosystem
        // --------------------------------

        EcosystemSimulator simulator =
            FindAnyObjectByType<EcosystemSimulator>();

        if (simulator != null)
        {
            simulator.ResetSimulation();
        }

        // --------------------------------
        // 3. Resume the game
        // --------------------------------

        Time.timeScale = 1f;

        // --------------------------------
        // 4. Hide configuration UI
        // --------------------------------

        if (configurationUI != null)
        {
            configurationUI.SetActive(false);
        }
        SimHud.Enter();   // [AI-GENERATED] Claude Code (Claude Opus 5.5): Shumin's simulation HUD (with Back) on top

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SwitchToSimu();
        }
    }
}
