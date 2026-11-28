// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shumin's Ending screen (EndingScreen.unity) and Ending Gallery (EndingGallery.unity), filled in by
/// object name like <see cref="SimHud"/>. The ending screen replaces the run; the gallery always opens on
/// top of whatever is showing (the Guided HUD or the ending screen) and Back closes it.
/// </summary>
public static class GuidedScreens
{
    public const string EndingScene = "EndingScreen", GalleryScene = "EndingGallery";

    public struct Result
    {
        public int ending; public bool isNew; public int day, sheep, leopards; public float vegetation;
        /// <summary>Whether Miloy / Snowy survived (doc, Ending Presentation step 3).</summary>
        public string focal;
        public bool focalDied;   // Miloy / Snowy died before the last day (the story ended early)
        /// <summary>Badges unlocked during this run (shown on the ending screen).</summary>
        public string[] newBadges;
    }
    public static Result Last => last;
    static Result last;

    // Gallery order: best to worst, as the children would read it (indexes into GuidedEndings.All).
    static int[] GalleryOrder => GuidedEndings.GalleryOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        AudioListener.pause = false;   // Play mode may start with it left on from a stopped session
    }

    /// <summary>Opens the ending screen for this result. False when the scene is not in the build.</summary>
    public static bool ShowEnding(Result result)
    {
        if (!Application.CanStreamedLevelBeLoaded(EndingScene)) return false;
        last = result;
        Time.timeScale = 1f;
        SceneManager.LoadScene(EndingScene);
        return true;
    }

    public static void OpenGallery()
    {
        if (SceneManager.GetSceneByName(GalleryScene).isLoaded || !Application.CanStreamedLevelBeLoaded(GalleryScene)) return;
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        galleryPausedRun = speed != null && speed.IsRunning;
        if (galleryPausedRun) speed.Pause();
        // The river and animal sounds kept playing behind the gallery. Pause the world audio; the music, the click sound
        // and the mascot voice ignore this pause.
        OverlayAudio.Hush();
        SceneManager.LoadSceneAsync(GalleryScene, LoadSceneMode.Additive);
    }

    static bool galleryPausedRun;

    static void CloseGallery()
    {
        SceneManager.UnloadSceneAsync(GalleryScene);
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        if (galleryPausedRun && speed != null) speed.Play();
    }

    // However the gallery closes (Back to the run, or Home from the ending), world sound comes back.
    static void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == GalleryScene || scene.name == ScreenFlow.Journal) OverlayAudio.Restore();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == EndingScene) FillEnding(FindCanvas(scene, mode));
        else if (scene.name == GalleryScene) FillGallery(FindCanvas(scene, mode));
    }

    // The scenes are standalone mock-ups: on top of another scene, drop their camera, light and EventSystem.
    static Transform FindCanvas(Scene scene, LoadSceneMode mode)
    {
        Transform canvas = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            bool extra = root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null || root.GetComponent<EventSystem>() != null;
            if (extra && mode == LoadSceneMode.Additive) { root.SetActive(false); continue; }
            if (root.TryGetComponent(out Canvas c))
            {
                canvas = root.transform;
                if (mode == LoadSceneMode.Additive) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 95; }
            }
        }
        return canvas;
    }

    // ---------- Ending screen (wireframe 24) ----------

    static void FillEnding(Transform root)
    {
        if (root == null) return;
        var e = GuidedEndings.All[last.ending];
        SetText(root, "EndingTitleTxt", e.title);
        // Doc, Ending Presentation: say whether Miloy / Snowy survived to the end or died earlier.
        string focal = !string.IsNullOrEmpty(last.focal) ? last.focal
            : last.day >= GuidedChallenge.ChallengeDays ? $"You finished all {GuidedChallenge.ChallengeDays} days!" : $"The challenge ended on Day {last.day}.";
        SetText(root, "CompletionSubtitleTxt", focal);
        SetText(root, "OutcomeDescriptionTxt", e.description);
        SetText(root, "LearningDescriptionTxt", e.takeaway);
        FitBox(Find(root, "OutcomeDescriptionTxt"));
        FitBox(Find(root, "LearningDescriptionTxt"));
        FitOneLine(Find(root, "CompletionSubtitleTxt"));
        SetText(Find(root, "GrassCard"), "ValueTxt", $"{last.vegetation:0}%");
        SetText(Find(root, "AnimalCard01"), "ValueTxt", last.sheep.ToString());
        SetText(Find(root, "AnimalCard02"), "ValueTxt", last.leopards.ToString());
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): animal pictures (were empty green squares) and a one-line
        // title, so "Thriving Highlands" no longer wraps onto the subtitle below it.
        SimHud.SetIcon(Find(Find(root, "AnimalCard01"), "AnimalImage"), SimHud.AnimalIcon(false));
        SimHud.SetIcon(Find(Find(root, "AnimalCard02"), "AnimalImage"), SimHud.AnimalIcon(true));
        FitOneLine(Find(root, "EndingTitleTxt"));
        SetText(root, "ProgressTxt", Progress());
        EndingCelebration.Play(root, last, e);   // illustration, badges, "New ending!" burst, counters
        OnClick(root, "ExploreAgainBtn", ScreenFlow.RestartGuided);
        OnClick(root, "ViewEndingGalleryBtn", OpenGallery);
        OnClick(root, "BackToMenuBtn", ScreenFlow.GoHome);
    }

    // ---------- Ending Gallery (wireframe 25): five cards, tap one for its detail page ----------

    static void FillGallery(Transform root)
    {
        if (root == null) return;
        var overview = Find(root, "GalleryOverviewPanel");
        var detail = Find(root, "GalleryDetailPanel");
        int shown = 0;

        // Doc, Ending Gallery: only unlocked endings open; the arrows step between unlocked ones.
        void ShowDetail(int slot)
        {
            int n = GalleryOrder.Length;
            shown = (slot % n + n) % n;
            int i = GalleryOrder[shown];
            if (!GuidedEndings.IsUnlocked(i)) return;
            var e = GuidedEndings.All[i];
            var box = Find(detail, "GalleryDetailBox");
            SetText(box, "EndingTitleTxt", e.title);
            SetText(box, "EndingDescriptionTxt", e.description);
            SetText(Find(box, "ExplanationBox01"), "WhatThisMeansTxt (1)", e.meaning);     // What this means
            SetText(Find(box, "ExplanationBox02"), "WhatThisMeansTxt (1)", e.takeaway);    // What did we learn?
            FitBox(Find(Find(box, "ExplanationBox02"), "WhatThisMeansTxt (1)"));
            EndingCelebration.Illustrate(box, i, true);
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): "Ecosystem Collapse" wrapped onto two lines over the
            // description, and long descriptions were cut off: title on one line, text shrinks to fit its box.
            FitOneLine(Find(box, "EndingTitleTxt"));
            FitBox(Find(box, "EndingDescriptionTxt"));
            FitBox(Find(Find(box, "ExplanationBox01"), "WhatThisMeansTxt (1)"));
            overview.gameObject.SetActive(false);
            detail.gameObject.SetActive(true);
        }

        // Arrows on the detail page: the next unlocked ending that way (locked ones can't be opened).
        void Step(int dir)
        {
            for (int k = 1; k <= GalleryOrder.Length; k++)
            {
                int slot = shown + dir * k;
                int n = GalleryOrder.Length;
                if (GuidedEndings.IsUnlocked(GalleryOrder[(slot % n + n) % n])) { ShowDetail(slot); return; }
            }
        }

        void ShowOverview()
        {
            if (detail != null) detail.gameObject.SetActive(false);
            if (overview != null) overview.gameObject.SetActive(true);
        }

        if (overview != null)
        {
            SetText(overview, "ProgressTxt", Progress());
            for (int slot = 0; slot < GalleryOrder.Length; slot++)
            {
                var card = Find(overview, $"EndingCard0{slot + 1}");
                if (card == null) continue;
                int i = GalleryOrder[slot];
                bool got = GuidedEndings.IsUnlocked(i);
                SetText(card, "EndingTitleTxt", got ? GuidedEndings.All[i].title : "???");
                SetText(card, "CardDescriptionTxt", got ? GuidedEndings.All[i].card : "Not discovered yet. Keep exploring!");
                EndingCelebration.Illustrate(card, i, got);
                FitOneLine(Find(card, "EndingTitleTxt"));   // "Ecosystem Collapse" wrapped onto the description
                FitBox(Find(card, "CardDescriptionTxt"));    // long lessons were cut off
                var unlocked = Find(card, "UnlockedStatus"); if (unlocked != null) unlocked.gameObject.SetActive(got);
                var locked = Find(card, "LockedStatus"); if (locked != null) locked.gameObject.SetActive(!got);
                int s = slot;
                var cardRect = (RectTransform)card;
                OnClick(card, null, () => { if (GuidedEndings.IsUnlocked(GalleryOrder[s])) ShowDetail(s); else EndingCelebration.Wiggle(cardRect); });
            }
            OnClick(overview, "BackBtn", CloseOrLeave);
            // Same as the detail page: keep Back and the arrows above the cards so every tap reaches them.
            foreach (var n in new[] { "BackBtn", "LArrowBtn", "RArrowBtn" }) OnTop(Find(overview, n));
            SetUpScroll(overview);
        }
        if (detail != null)
        {
            OnClick(detail, "BackBtn", ShowOverview);
            // The big detail box was drawn over the Back / arrow buttons, so taps never reached them: bring them to the front.
            foreach (var n in new[] { "BackBtn", "LArrowBtn", "RArrowBtn" }) OnTop(Find(detail, n));
            // Something else on screen swallowed taps on the left arrow (the right one worked). The arrows now check the
            // tap position themselves, so nothing drawn on top can block them. Their Button clicks are cleared so a tap
            // never counts twice.
            OnClick(detail, "LArrowBtn", () => { });
            OnClick(detail, "RArrowBtn", () => { });
            var tap = detail.gameObject.GetComponent<TapZones>() ?? detail.gameObject.AddComponent<TapZones>();
            tap.zones.Clear();
            tap.zones.Add((Find(detail, "LArrowBtn") as RectTransform, () => { UIClickSound.PlayClick(); Step(-1); }));
            tap.zones.Add((Find(detail, "RArrowBtn") as RectTransform, () => { UIClickSound.PlayClick(); Step(1); }));
            foreach (var n in new[] { "LArrowBtn", "RArrowBtn" })   // their own click above, so the automatic one stays quiet
            { var a = Find(detail, n); if (a != null && !a.TryGetComponent(out NoAutoClick _)) a.gameObject.AddComponent<NoAutoClick>(); }
        }
        ShowOverview();
    }

    // The overview fits 3 whole cards on a 16:10 tablet (the 4th was cut off, the 5th off-screen). Show 3 at a
    // time in the first three cards' places; a swipe or the arrows move the window one card, and one dot per
    // scroll position (3 for 5 cards) shows where you are.
    const int VisibleCards = 3;
    const float SwipeDistance = 60f;   // screen pixels a finger must travel sideways to count as a swipe

    // Sideways swipe anywhere on the overview (cards are buttons, so their drags bubble up to here).
    class Swipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public System.Action<int> onSwipe;   // +1 = next cards (finger moved left), -1 = previous
        Vector2 start;
        public void OnBeginDrag(PointerEventData e) => start = e.position;
        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e)
        {
            var d = e.position - start;
            if (Mathf.Abs(d.x) >= SwipeDistance && Mathf.Abs(d.x) > Mathf.Abs(d.y)) onSwipe?.Invoke(d.x < 0f ? 1 : -1);
        }
    }

    static void SetUpScroll(Transform overview)
    {
        var cards = new List<RectTransform>();
        for (int n = 1; n <= GalleryOrder.Length; n++)
            if (Find(overview, $"EndingCard0{n}") is RectTransform rt) cards.Add(rt);
        if (cards.Count <= VisibleCards) return;

        var places = new Vector2[VisibleCards];
        for (int n = 0; n < VisibleCards; n++) places[n] = cards[n].anchoredPosition;
        int last = cards.Count - VisibleCards;
        var dots = new List<Image>();
        for (int n = 1; n <= 9; n++)
        {
            if (!(Find(overview, $"ScrollBall0{n}") is Transform d)) continue;
            bool used = n - 1 <= last;          // 5 cards, 3 in view: positions 1-3, 2-4, 3-5 = 3 dots
            d.gameObject.SetActive(used);
            if (used && d.TryGetComponent(out Image img)) dots.Add(img);
        }

        int first = 0;
        void Show()
        {
            for (int n = 0; n < cards.Count; n++)
            {
                int place = n - first;
                bool inView = place >= 0 && place < VisibleCards;
                cards[n].gameObject.SetActive(inView);
                if (inView) cards[n].anchoredPosition = places[place];
            }
            for (int n = 0; n < dots.Count; n++)
                dots[n].color = n == first ? new Color(0.3f, 0.62f, 0.38f) : Color.white;
            // At the start / end there is nothing more that way: hide that arrow so it does not look broken.
            var la = Find(overview, "LArrowBtn"); if (la != null) la.gameObject.SetActive(first > 0);
            var ra = Find(overview, "RArrowBtn"); if (ra != null) ra.gameObject.SetActive(first < last);
        }
        void Move(int step) { first = Mathf.Clamp(first + step, 0, last); Show(); }
        OnClick(overview, "LArrowBtn", () => Move(-1));
        OnClick(overview, "RArrowBtn", () => Move(1));

        // Swiping needs something on the panel the touch can land on; a clear image if the mock-up has none.
        if (!overview.TryGetComponent(out Graphic bg)) { bg = overview.gameObject.AddComponent<Image>(); bg.color = Color.clear; }
        bg.raycastTarget = true;
        overview.gameObject.AddComponent<Swipe>().onSwipe = Move;
        Show();
    }

    // Last sibling was not enough for the detail page's left arrow (still under the big box). Its own sorted canvas
    // + raycaster puts it above everything in the gallery, so taps always reach it.
    static void OnTop(Transform b)
    {
        if (b == null) return;
        b.SetAsLastSibling();
        var root = b.parent != null ? b.parent.GetComponentInParent<Canvas>() : null;
        int above = root != null ? root.rootCanvas.sortingOrder + 1 : 1;   // just above the gallery's own canvas
        if (!b.TryGetComponent(out Canvas c)) c = b.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = above;
        if (!b.TryGetComponent(out GraphicRaycaster _)) b.gameObject.AddComponent<GraphicRaycaster>();
    }

    static void CloseOrLeave()
    {
        if (SceneManager.sceneCount > 1) CloseGallery();
        else ScreenFlow.GoHome();
    }

    static string Progress() => GuidedEndings.Progress();

    // ---------- Helpers (names in the mock-ups sometimes carry quotes or trailing spaces) ----------

    // Card text from the mock-up has fixed sizes for short placeholder words; shrink real text to fit.
    static void FitOneLine(Transform t)
    {
        if (t == null || !t.TryGetComponent(out TextMeshProUGUI tmp)) return;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (!tmp.enableAutoSizing) tmp.fontSizeMax = tmp.fontSize;   // never bigger than the design
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
    }

    internal static void FitBox(Transform t)
    {
        if (t == null || !t.TryGetComponent(out TextMeshProUGUI tmp)) return;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        if (!tmp.enableAutoSizing) tmp.fontSizeMax = tmp.fontSize;   // never bigger than the design
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    internal static Transform Find(Transform under, string name)
    {
        if (under == null) return null;
        foreach (var t in under.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim().Trim('\'').Trim() == name) return t;
        return null;
    }

    static void SetText(Transform under, string name, string text)
    {
        var t = Find(under, name);
        if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = text;
    }

    // name == null: the "under" object itself becomes the button (the gallery cards are plain images).
    static void OnClick(Transform under, string name, UnityEngine.Events.UnityAction action)
    {
        var t = name == null ? under : Find(under, name);
        if (t == null) return;
        if (!t.TryGetComponent(out Button b)) b = t.gameObject.AddComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }
}

/// <summary>
/// Full-screen pages over the run (Ending Gallery, Field Journal): the world (river, animals, wind) goes quiet,
/// the music keeps playing. The click sound and the mascot voice ignore the pause, so buttons still click.
/// </summary>
public static class OverlayAudio
{
    public static void Hush()
    {
        AudioListener.pause = true;
        var am = AudioManager.Instance;
        if (am != null && am.bgm != null) am.bgm.ignoreListenerPause = true;
    }

    public static void Restore()
    {
        AudioListener.pause = false;
        var am = AudioManager.Instance;
        if (am != null && am.bgm != null) am.bgm.ignoreListenerPause = false;
    }
}

/// <summary>Marks a button whose click sound is played by its own code, so UIClickSound does not play a second one.</summary>
public class NoAutoClick : MonoBehaviour { }

/// <summary>Calls an action when a tap lands inside a RectTransform, whatever UI is drawn over it (gallery arrows).</summary>
public class TapZones : MonoBehaviour
{
    public readonly List<(RectTransform rect, System.Action action)> zones = new List<(RectTransform, System.Action)>();

    void Update()
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        Vector2 pos = pointer.position.ReadValue();
        foreach (var (rect, action) in zones)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) continue;
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, pos, cam)) { action?.Invoke(); return; }
        }
    }
}
