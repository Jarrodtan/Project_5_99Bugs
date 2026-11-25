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

    public struct Result { public int ending; public bool isNew; public int day, sheep, leopards; public float vegetation; }
    static Result last;

    // Gallery order: best to worst, as the children would read it (indexes into GuidedChallenge.Endings).
    static readonly int[] GalleryOrder = { 3, 4, 1, 2, 0 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
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
        SceneManager.LoadSceneAsync(GalleryScene, LoadSceneMode.Additive);
    }

    static bool galleryPausedRun;

    static void CloseGallery()
    {
        SceneManager.UnloadSceneAsync(GalleryScene);
        var speed = Object.FindFirstObjectByType<SimulationSpeedController>();
        if (galleryPausedRun && speed != null) speed.Play();
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
        var (title, text, lesson) = GuidedChallenge.Endings[last.ending];
        SetText(root, "EndingTitleTxt", title);
        SetText(root, "CompletionSubtitleTxt", last.day >= GuidedChallenge.ChallengeDays
            ? $"You finished all {GuidedChallenge.ChallengeDays} days!" + (last.isNew ? " New ending unlocked!" : "")
            : $"The challenge ended on Day {last.day}." + (last.isNew ? " New ending unlocked!" : ""));
        SetText(root, "OutcomeDescriptionTxt", text);
        SetText(root, "LearningDescriptionTxt", lesson);
        SetText(Find(root, "GrassCard"), "ValueTxt", $"{last.vegetation:0}%");
        SetText(Find(root, "AnimalCard01"), "ValueTxt", last.sheep.ToString());
        SetText(Find(root, "AnimalCard02"), "ValueTxt", last.leopards.ToString());
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): animal pictures (were empty green squares) and a one-line
        // title, so "Thriving Highlands" no longer wraps onto the subtitle below it.
        SimHud.SetIcon(Find(Find(root, "AnimalCard01"), "AnimalImage"), SimHud.AnimalIcon(false));
        SimHud.SetIcon(Find(Find(root, "AnimalCard02"), "AnimalImage"), SimHud.AnimalIcon(true));
        FitOneLine(Find(root, "EndingTitleTxt"));
        SetText(root, "ProgressTxt", Progress());
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
        int mask = PlayerPrefs.GetInt(GuidedChallenge.GalleryKey, 0);
        int shown = 0;

        void ShowDetail(int slot)
        {
            shown = (slot + GalleryOrder.Length) % GalleryOrder.Length;
            int i = GalleryOrder[shown];
            bool got = (mask & (1 << i)) != 0;
            var box = Find(detail, "GalleryDetailBox");
            SetText(box, "EndingTitleTxt", got ? GuidedChallenge.Endings[i].title : "???");
            SetText(box, "EndingDescriptionTxt", got ? GuidedChallenge.Endings[i].text : "Not discovered yet. Try different choices!");
            SetText(Find(box, "ExplanationBox01"), "WhatThisMeansTxt (1)", got ? GuidedChallenge.Endings[i].lesson : "");
            SetText(Find(box, "ExplanationBox02"), "WhatThisMeansTxt (1)", got ? "Play again and try different choices to find the other endings." : "");
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): "Ecosystem Collapse" wrapped onto two lines over the
            // description, and long descriptions were cut off: title on one line, text shrinks to fit its box.
            FitOneLine(Find(box, "EndingTitleTxt"));
            FitBox(Find(box, "EndingDescriptionTxt"));
            FitBox(Find(Find(box, "ExplanationBox01"), "WhatThisMeansTxt (1)"));
            overview.gameObject.SetActive(false);
            detail.gameObject.SetActive(true);
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
                bool got = (mask & (1 << i)) != 0;
                SetText(card, "EndingTitleTxt", got ? GuidedChallenge.Endings[i].title : "???");
                SetText(card, "CardDescriptionTxt", got ? GuidedChallenge.Endings[i].lesson : "Not discovered yet");
                FitOneLine(Find(card, "EndingTitleTxt"));   // "Ecosystem Collapse" wrapped onto the description
                FitBox(Find(card, "CardDescriptionTxt"));    // long lessons were cut off
                var unlocked = Find(card, "UnlockedStatus"); if (unlocked != null) unlocked.gameObject.SetActive(got);
                var locked = Find(card, "LockedStatus"); if (locked != null) locked.gameObject.SetActive(!got);
                int s = slot;
                OnClick(card, null, () => ShowDetail(s));
            }
            OnClick(overview, "BackBtn", CloseOrLeave);
            SetUpScroll(overview);
        }
        if (detail != null)
        {
            OnClick(detail, "BackBtn", ShowOverview);
            OnClick(detail, "LArrowBtn", () => ShowDetail(shown - 1));
            OnClick(detail, "RArrowBtn", () => ShowDetail(shown + 1));
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

    static void CloseOrLeave()
    {
        if (SceneManager.sceneCount > 1) CloseGallery();
        else ScreenFlow.GoHome();
    }

    static string Progress()
    {
        int mask = PlayerPrefs.GetInt(GuidedChallenge.GalleryKey, 0), found = 0;
        for (int i = 0; i < GuidedChallenge.Endings.Length; i++) if ((mask & (1 << i)) != 0) found++;
        return $"{found} / {GuidedChallenge.Endings.Length} endings found";
    }

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

    static void FitBox(Transform t)
    {
        if (t == null || !t.TryGetComponent(out TextMeshProUGUI tmp)) return;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        if (!tmp.enableAutoSizing) tmp.fontSizeMax = tmp.fontSize;   // never bigger than the design
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    static Transform Find(Transform under, string name)
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
