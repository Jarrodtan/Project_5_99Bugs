using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Makes sure a simulation scene records its history and, in Free Exploration, detects investigation
/// cases, in every build (the dev overlay only does this in development builds). Called by
/// <see cref="ScreenFlow"/> when HimalayaSimulation loads; no scene file is edited.
/// </summary>
public static class FieldJournalInstaller
{
    public static void InstallInSimulation(Scene scene)
    {
        EcosystemSimulator sim = null;
        foreach (var root in scene.GetRootGameObjects())
            if ((sim = root.GetComponentInChildren<EcosystemSimulator>(true)) != null) break;
        if (sim == null) return;

        var go = sim.gameObject;
        var recorder = go.GetComponent<PopulationHistoryRecorder>();
        if (recorder == null) recorder = go.AddComponent<PopulationHistoryRecorder>();
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): grass / water eaten, famine level. The starting state may
        // already be recorded without them; the graph and the notes start those lines from the next sample.
        var meter = go.GetComponent<ResourceUseMeter>();
        if (meter == null) go.AddComponent<ResourceUseMeter>().recorder = recorder;
        var days = go.GetComponent<PopulationHistoryDays>();
        if (days == null) (days = go.AddComponent<PopulationHistoryDays>()).recorder = recorder;
        FieldJournalObservations.Track(recorder, days, sim, foodAndWater: true);

        // Investigations are a Free Exploration feature; Guided has its own events and endings.
        if (ScreenFlow.GuidedMode) return;

        var detector = go.GetComponent<CaseDetector>();
        if (detector == null) detector = go.AddComponent<CaseDetector>();
        if (detector.recorder == null) detector.recorder = recorder;
        if (detector.cases == null || detector.cases.Count == 0) detector.cases = FieldJournalCases.LoadAll();
        detector.CaseUnlocked -= FieldJournalObservations.NoteUnlocked;
        detector.CaseUnlocked += FieldJournalObservations.NoteUnlocked;

        CaseDiscoveryToast.Attach(detector);
    }
}

/// <summary>
/// The "New case discovered!" popup (wireframe 08). Slides in at the top of the simulation when
/// <see cref="CaseDetector.CaseUnlocked"/> fires, stays a few seconds, and its button opens the Field Journal
/// on that case. Several unlocks queue up; nothing shows while the journal is open. Doesn't pause the run.
/// </summary>
public class CaseDiscoveryToast : MonoBehaviour
{
    /// <summary>True while a popup is installed (the dev overlay then skips its own banner).</summary>
    public static bool Active => instance != null;
    static CaseDiscoveryToast instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;   // domain reload is off in this project

    public float showSeconds = 6f;
    public float slideSeconds = 0.35f;

    CaseDetector detector;
    readonly Queue<InvestigationCase> queue = new();
    InvestigationCase showing;
    float shownAt;
    RectTransform card;
    CanvasGroup group;
    TextMeshProUGUI titleText;

    const float ShownY = -28f, HiddenY = 240f;
    static readonly Color CardColor = new(0.97f, 0.97f, 0.95f, 1f);
    static readonly Color TitleColor = new(0.12f, 0.16f, 0.2f);
    static readonly Color BodyColor = new(0.2f, 0.24f, 0.28f);
    static readonly Color Green = new(0.3f, 0.62f, 0.38f);
    static readonly Color Blue = new(0.28f, 0.5f, 0.75f);

    public static CaseDiscoveryToast Attach(CaseDetector detector)
    {
        if (detector == null) return null;
        if (instance != null)
        {
            if (instance.detector == detector) return instance;
            Destroy(instance.gameObject);
        }
        var go = new GameObject("[Case Discovery Popup]");
        SceneManager.MoveGameObjectToScene(go, detector.gameObject.scene);   // unloads with the simulation
        var toast = go.AddComponent<CaseDiscoveryToast>();
        toast.detector = detector;
        detector.CaseUnlocked += toast.OnUnlocked;
        instance = toast;
        return toast;
    }

    void OnDestroy()
    {
        if (detector != null) detector.CaseUnlocked -= OnUnlocked;
        if (instance == this) instance = null;
    }

    void OnUnlocked(InvestigationCase c)
    {
        if (c != null) queue.Enqueue(c);
    }

    static bool JournalOpen => SceneManager.GetSceneByName(ScreenFlow.Journal).isLoaded;

    void Update()
    {
        if (showing == null)
        {
            if (queue.Count == 0 || JournalOpen) return;
            var next = queue.Dequeue();
            // Already read in the journal (opened from an earlier popup): no need to announce it again.
            if (!CaseSeenTracker.ForDevice().IsNew(detector.Journal, next.Key)) return;
            Show(next);
            return;
        }

        // Unscaled time: the popup still finishes if the player pauses right after it appears.
        float t = Time.unscaledTime - shownAt;
        float k = t < slideSeconds ? t / slideSeconds
                : t > showSeconds - slideSeconds ? (showSeconds - t) / slideSeconds : 1f;
        k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
        card.anchoredPosition = new Vector2(0f, Mathf.Lerp(HiddenY, ShownY, k));
        group.alpha = k;
        if (t >= showSeconds || JournalOpen) Hide();
    }

    void Show(InvestigationCase c)
    {
        Build();
        showing = c;
        shownAt = Time.unscaledTime;
        titleText.text = string.IsNullOrWhiteSpace(c.title) ? c.Key : c.title;
        card.gameObject.SetActive(true);
        group.alpha = 0f;
        card.anchoredPosition = new Vector2(0f, HiddenY);
    }

    void Hide()
    {
        showing = null;
        if (card != null) card.gameObject.SetActive(false);
    }

    void OpenJournal()
    {
        if (showing != null) FieldJournalScreen.Request(FieldJournalScreen.Page.Investigations, showing.Key);
        Hide();
        ScreenFlow.OpenJournalOverlay();
    }

    // Built on first use, in the same style as the Guided popups (1920x1200 reference, plain panels).
    void Build()
    {
        if (card != null) return;
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;   // above the HUD and Guided popups (60), under the Leave box (70) and the journal (90)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;

        card = Panel(canvasGo.transform, "Card", CardColor);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(1000f, 180f);
        group = card.gameObject.AddComponent<CanvasGroup>();

        var accent = Panel(card, "Accent", Green);
        accent.anchorMin = Vector2.zero; accent.anchorMax = new Vector2(0f, 1f); accent.pivot = new Vector2(0f, 0.5f);
        accent.sizeDelta = new Vector2(16f, 0f);

        Text(card, "NEW CASE DISCOVERED!", 26f, Green, FontStyles.Bold, new Vector2(48f, -22f), new Vector2(640f, 36f));
        titleText = Text(card, "", 44f, TitleColor, FontStyles.Bold, new Vector2(48f, -58f), new Vector2(640f, 60f));
        Text(card, "Open your Field Journal to find out what happened.", 26f, BodyColor, FontStyles.Normal, new Vector2(48f, -122f), new Vector2(640f, 40f));

        var open = Panel(card, "OpenJournalBtn", Blue);
        open.anchorMin = open.anchorMax = open.pivot = new Vector2(1f, 0.5f);
        open.anchoredPosition = new Vector2(-84f, 0f);
        open.sizeDelta = new Vector2(220f, 96f);
        open.gameObject.AddComponent<Button>().onClick.AddListener(OpenJournal);
        var openLabel = Text(open, "Open\nJournal", 30f, Color.white, FontStyles.Bold, Vector2.zero, Vector2.zero);
        Fill(openLabel.rectTransform);
        openLabel.alignment = TextAlignmentOptions.Center;

        var close = Panel(card, "CloseBtn", new Color(0f, 0f, 0f, 0f));
        close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
        close.sizeDelta = new Vector2(64f, 64f);
        close.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
        var x = Text(close, "×", 44f, BodyColor, FontStyles.Normal, Vector2.zero, Vector2.zero);
        Fill(x.rectTransform);
        x.alignment = TextAlignmentOptions.Center;

        card.gameObject.SetActive(false);
    }

    static RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, FontStyles style, Vector2 topLeft, Vector2 box)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeft;
        rt.sizeDelta = box;
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = size * 0.6f;
        return t;
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
