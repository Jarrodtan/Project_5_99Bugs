// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Building the Himalayas..." card over the simulation while the world is generated (terrain, meadows,
/// river, NavMesh bake). On the bigger M3 map this takes a few seconds and the screen stutters, so players
/// should know the game has not frozen. Fades out once HimalayaWorldBuilder.IsReady (or after 30 s, to be safe).
/// </summary>
public class LoadingOverlay : MonoBehaviour
{
    const float MaxSeconds = 30f, FadeSeconds = 0.4f, ShowAfterSeconds = 1f;
    static readonly bool ShowCard = false;   // readonly, not const: no "unreachable code" warning

    HimalayaWorldBuilder builder;
    CanvasGroup group;
    TextMeshProUGUI dots;
    float started, fadeFrom = -1f;
    TextMeshProUGUI title;
    RectTransform bar;
    float shown;

    /// <summary>True while the loading card is up (the mascot waits until it is gone).</summary>
    public static bool Active { get; private set; }

    void OnEnable() { Active = true; Mascot.HoldForLoading(); }
    void OnDestroy() { Active = false; Mascot.ShowPendingMenuLine(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnLoaded(Scene scene, LoadSceneMode _)
    {
        // Turned off 26 Nov: the NavMesh is pre-baked now, so the map loads fast enough without a card.
        // Set ShowCard back to true to bring "Building the Himalayas" back.
        if (!ShowCard) return;
        if (scene.name != ScreenFlow.Simulation) return;
        var b = FindAnyObjectByType<HimalayaWorldBuilder>();
        if (b == null || b.IsReady) return;
        var go = new GameObject("[Loading Overlay]");
        SceneManager.MoveGameObjectToScene(go, scene);   // goes away with the scene
        go.AddComponent<LoadingOverlay>().builder = b;
    }

    void Start()
    {
        started = Time.unscaledTime;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;   // above the HUD, the Guided intro card and the mascot
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();   // swallows taps while loading
        group = gameObject.AddComponent<CanvasGroup>();

        // Same look as the Leave / Continue boxes: dimmed screen, cream card, brown edge, Chewy font, dark ink.
        var dim = Panel(transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero, new Color(0f, 0f, 0f, 0.8f));
        // A filling bar says "wait" without reading (ages 7-9); the line under the title is short and changes as it builds.
        var card = Panel(dim.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000f, 380f), Color.white);
        title = Label(card.transform, "Building the Himalayas", 56f, new Vector2(0f, 85f));
        dots = Label(card.transform, "Growing grass", 34f, new Vector2(0f, 5f));
        var track = Panel(card.transform, "BarTrack", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(760f, 36f), new Color(0.85f, 0.80f, 0.68f));
        ((RectTransform)track.transform).anchoredPosition = new Vector2(0f, 80f);
        var fill = Panel(track.transform, "BarFill", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Color(0.24f, 0.51f, 0.30f));
        bar = (RectTransform)fill.transform;
        bar.pivot = new Vector2(0f, 0.5f);
        bar.anchoredPosition = Vector2.zero;
        ScreenFlow.StyleLikeShumin(card, null);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (fadeFrom < 0f)
        {
            int n = (int)(t * 2f) % 4;
            float target = builder != null ? builder.Progress : 0f;
            // The build moves in big steps (the terrain is one long step), so creep slowly past the last one: never looks frozen.
            shown = Mathf.Max(shown, Mathf.Min(target + 0.15f, shown + Time.unscaledDeltaTime * 0.02f));
            shown = Mathf.MoveTowards(shown, Mathf.Max(shown, target), Time.unscaledDeltaTime * 0.8f);
            shown = Mathf.Min(shown, 0.99f);
            dots.text = (shown < 0.5f ? "Growing grass" : shown < 0.8f ? "Filling the rivers" : "Waking up the animals") + new string('.', n);
            if (bar != null) bar.sizeDelta = new Vector2(760f * shown, 0f);
            // Quick builds: don't flash the card at all; only show it if the map takes longer than this.
            group.alpha = t - started < ShowAfterSeconds ? 0f : 1f;
            if (builder == null || builder.IsReady || t - started > MaxSeconds)
            {
                if (t - started < ShowAfterSeconds) { Destroy(gameObject); return; }   // never shown: just go
                fadeFrom = t;
            }
            return;
        }
        group.alpha = 1f - (t - fadeFrom) / FadeSeconds;
        if (group.alpha <= 0f) Destroy(gameObject);
    }

    static GameObject Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 pos)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        t.rectTransform.sizeDelta = new Vector2(1100f, 90f);
        t.rectTransform.anchoredPosition = pos;
        t.text = text;
        t.fontSize = size;
        t.color = ScreenFlow.InkColor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }
}
