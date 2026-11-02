// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes the ecosystem's dramatic moments visible. The animal AI reports a moment (a hunt starting,
/// a catch, mating, a birth) with <see cref="Report"/>; a banner appears at the top of the screen
/// with a child-friendly caption and a "Watch" button that flies the camera to it and follows.
///
/// One banner at a time, with a short quiet gap between banners, and the most exciting moment wins
/// (hunt > birth > mating > catch), so the screen never floods. Builds its own UI the first time
/// something happens, so no scene setup is needed.
/// </summary>
public class DramaDirector : MonoBehaviour
{
    public enum Moment { Catch = 0, Mating = 1, Birth = 2, Hunt = 3 }   // higher = more exciting

    const float ShowSeconds = 7f;     // banner stays this long (real time)
    const float QuietSeconds = 6f;    // gap after a banner before the next one
    const float WatchSeconds = 12f;   // after Watch, no new banners while the player is watching

    static DramaDirector instance;

    GameObject banner, watchButton;
    TextMeshProUGUI caption, status;
    Transform subject;
    Moment current;
    float hideAt, quietUntil;
    bool showing;

    /// <summary>Report a moment. Ignored while a more exciting banner is showing or during the quiet gap.</summary>
    public static void Report(Moment moment, Transform subjectToWatch, string text)
    {
        if (subjectToWatch == null || !Application.isPlaying) return;
        if (instance == null) instance = new GameObject("[DramaDirector]").AddComponent<DramaDirector>();
        instance.Offer(moment, subjectToWatch, text);
    }

    /// <summary>A news line with no Watch button (season or weather change). Shown unless a hunt is on screen.</summary>
    public static void Announce(string text)
    {
        if (!Application.isPlaying) return;
        if (instance == null) instance = new GameObject("[DramaDirector]").AddComponent<DramaDirector>();
        var d = instance;
        if (d.showing && d.subject != null && d.current == Moment.Hunt) return;
        if (d.banner == null) d.BuildUI();
        d.subject = null;
        d.caption.text = text;
        d.watchButton.SetActive(false);
        d.banner.SetActive(true);
        d.showing = true;
        d.hideAt = Time.unscaledTime + ShowSeconds;
    }

    /// <summary>Small always-visible pill in the top-right corner, e.g. "Spring · Day 3 · Rain".</summary>
    public static void SetStatus(string text)
    {
        if (!Application.isPlaying) return;
        if (instance == null) instance = new GameObject("[DramaDirector]").AddComponent<DramaDirector>();
        if (instance.banner == null) instance.BuildUI();
        instance.status.text = text;
        UpdateStatusVisibility();
    }

    /// <summary>Guided with Shumin's HUD: her day panel shows the season and weather instead (one day count, not two).</summary>
    public static void UpdateStatusVisibility()
    {
        if (instance == null || instance.status == null) return;
        instance.status.transform.parent.gameObject.SetActive(!(ScreenFlow.GuidedMode && SimHud.Active));
    }

    void Offer(Moment moment, Transform target, string text)
    {
        float now = Time.unscaledTime;
        bool announcing = showing && subject == null;
        if (!announcing && (showing ? moment <= current : now < quietUntil)) return;

        if (banner == null) BuildUI();
        subject = target;
        current = moment;
        caption.text = text;
        watchButton.SetActive(true);
        banner.SetActive(true);
        showing = true;
        hideAt = now + ShowSeconds;
    }

    void Update()
    {
        if (showing && Time.unscaledTime >= hideAt) Hide(QuietSeconds);
        else if (showing && watchButton.activeSelf && subject == null) Hide(QuietSeconds);   // the animal is gone
    }

    void Hide(float quiet)
    {
        if (banner != null) banner.SetActive(false);
        showing = false;
        quietUntil = Time.unscaledTime + quiet;
    }

    void Watch()
    {
        var cam = FindFirstObjectByType<TerrainCameraController>();
        if (cam != null && subject != null) cam.Follow(subject);
        Hide(WatchSeconds);
    }

    // Banner: rounded dark bar at the top centre, caption on the left, Watch button on the right.
    void BuildUI()
    {
        var canvasGo = new GameObject("Drama Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1600);   // same as the game's HUD
        scaler.matchWidthOrHeight = 0.5f;

        banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)banner.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);   // pinned to the top centre on any screen
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -280f);          // below Play / Pause / Reset and the season pill, so they never overlap
        rt.sizeDelta = new Vector2(1300f, 130f);
        banner.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

        caption = new GameObject("Caption", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        caption.transform.SetParent(banner.transform, false);
        var crt = caption.rectTransform;
        crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 1f);
        crt.offsetMin = new Vector2(40f, 10f); crt.offsetMax = new Vector2(-330f, -10f);
        caption.fontSize = 46f;
        caption.color = Color.white;
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.textWrappingMode = TextWrappingModes.Normal;

        var buttonGo = new GameObject("Watch", typeof(RectTransform), typeof(Image), typeof(Button));
        watchButton = buttonGo;
        buttonGo.transform.SetParent(banner.transform, false);
        var brt = (RectTransform)buttonGo.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-24f, 0f);
        brt.sizeDelta = new Vector2(280f, 96f);               // big enough for small fingers
        buttonGo.GetComponent<Image>().color = new Color(0.98f, 0.72f, 0.2f, 1f);
        buttonGo.GetComponent<Button>().onClick.AddListener(Watch);

        var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(buttonGo.transform, false);
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        label.text = "Watch";
        label.fontSize = 50f;
        label.color = new Color(0.2f, 0.12f, 0.02f, 1f);
        label.alignment = TextAlignmentOptions.Center;

        banner.SetActive(false);

        // Status pill, top-right corner, above the animal card (bottom-right ran into the speed buttons).
        var pill = new GameObject("Status", typeof(RectTransform), typeof(Image));
        pill.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)pill.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = new Vector2(-40f, -170f);   // below the Play/Pause/Reset row, so narrow tablets don't overlap
        prt.sizeDelta = new Vector2(620f, 84f);
        pill.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.75f);
        // display only; on 4:3 tablets the pill overlaps the HUD's Field Journal button and swallowed its taps
        pill.GetComponent<Image>().raycastTarget = false;
        status = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        status.raycastTarget = false;
        status.transform.SetParent(pill.transform, false);
        var srt = status.rectTransform;
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
        status.enableAutoSizing = true;   // shrink rather than spill out of the pill
        status.fontSizeMax = 40f;
        status.fontSizeMin = 24f;
        status.color = Color.white;
        status.alignment = TextAlignmentOptions.Center;
    }
}
