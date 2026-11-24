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
    const float MaxSeconds = 30f, FadeSeconds = 0.4f;

    HimalayaWorldBuilder builder;
    CanvasGroup group;
    TextMeshProUGUI dots;
    float started, fadeFrom = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.name != ScreenFlow.Simulation) return;
        var b = FindFirstObjectByType<HimalayaWorldBuilder>();
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
        var card = Panel(dim.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1160f, 340f), Color.white);
        Label(card.transform, "Building the Himalayas", 56f, new Vector2(0f, 50f));
        dots = Label(card.transform, "Growing grass and finding paths for the animals", 32f, new Vector2(0f, -55f));
        ScreenFlow.StyleLikeShumin(card, null);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (fadeFrom < 0f)
        {
            int n = (int)(t * 2f) % 4;
            dots.text = "Growing grass and finding paths for the animals" + new string('.', n);
            if (builder == null || builder.IsReady || t - started > MaxSeconds) fadeFrom = t;
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
