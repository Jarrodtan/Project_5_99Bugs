// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives Shumin's simulation HUD (Scenes/UI.unity) on top of HimalayaSimulation. The UI scene is loaded
/// additively and its objects are found by NAME, so Shumin can restyle it freely as long as the names
/// stay (DayPanel, ResetBtn, Play/PauseBtn, SpeedBtn, EcosystemOverviewPanel, AnimalInspectionPanel...).
/// The old HUD (white Play/Pause/Reset and speed buttons, stats tabs, grey animal card) is hidden; its
/// logic keeps running underneath, so nothing else changes.
/// </summary>
public class SimHud : MonoBehaviour
{
    // Guided mode uses Shumin's Guided Exploration HUD (same panels plus the intro and event cards).
    public const string FreeSceneName = "UI", GuidedSceneName = "GuidedExploration";
    public static string SceneName => ScreenFlow.GuidedMode ? GuidedSceneName : FreeSceneName;

    /// <summary>Loads the HUD scene on top of the simulation (once). Call when the simulation starts running.</summary>
    public static void Enter()
    {
        if (SceneManager.GetSceneByName(SceneName).isLoaded || loading) return;
        loading = true;
        SceneManager.sceneLoaded += OnLoaded;
        SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
    }

    static bool loading;

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;
        SceneManager.sceneLoaded -= OnLoaded;
        loading = false;

        GameObject canvas = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            // The HUD scene was built as a standalone mock-up: drop its own camera, light and extra EventSystem.
            if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null) { root.SetActive(false); continue; }
            if (root.GetComponent<EventSystem>() != null)
            {
                bool another = false;
                foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) if (es.gameObject != root) another = true;
                if (another) root.SetActive(false);
                continue;
            }
            if (root.GetComponent<Canvas>() != null) canvas = root;
        }
        if (canvas != null) canvas.AddComponent<SimHud>();
        else ScreenFlow.AddLeaveButton();   // no HUD found: at least keep a way back
    }

    // ---------- Wiring ----------

    /// <summary>True while Shumin's HUD is on screen (Guided hides its own day pill then).</summary>
    public static bool Active { get; private set; }
    void OnDestroy() { Active = false; }

    readonly Dictionary<string, List<Transform>> byName = new Dictionary<string, List<Transform>>();
    SimulationSpeedController speed;
    ClockService clock;

    GameObject inspection, expanded, overview;
    AnimalBrain shown;
    AnimalBrain factAnimal;   // the animal the current fun fact was picked for
    string fact;
    float nextRefresh;
    bool weatherMoved;

    Transform Find(string name, Transform under = null)
    {
        if (under != null)
        {
            foreach (var t in under.GetComponentsInChildren<Transform>(true)) if (t.name.Trim().Trim('\'').Trim() == name) return t;
            return null;
        }
        return byName.TryGetValue(name, out var l) ? l[0] : null;
    }

    TextMeshProUGUI Txt(string name, Transform under = null)
    {
        var t = Find(name, under);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    void Button(string name, UnityEngine.Events.UnityAction action, Transform under = null)
    {
        var t = Find(name, under);
        if (t == null || !t.TryGetComponent(out Button b)) return;
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string key = t.name.Trim().Trim('\'').Trim();   // e.g. "'HydrationTxt '" in the mock-up
            if (!byName.TryGetValue(key, out var l)) byName[key] = l = new List<Transform>();
            l.Add(t);
        }
        speed = FindFirstObjectByType<SimulationSpeedController>();
        clock = FindFirstObjectByType<ClockService>();
        Active = true;
        SeeThrough();

        inspection = Find("AnimalInspectionPanel")?.gameObject;
        expanded = Find("ExpandedOverviewPanel")?.gameObject;
        overview = Find("EcosystemOverviewPanel")?.gameObject;
        if (inspection != null) inspection.SetActive(false);
        if (expanded != null) expanded.SetActive(false);
        var contextual = Find("ContextualPanel");
        if (contextual != null) contextual.gameObject.SetActive(false);   // shown by ContextualQuestions when a question fires
        Mascot.Attach(transform);
        if (!ScreenFlow.GuidedMode) ContextualQuestions.Attach(transform);   // CQ01-05 are for Free Exploration

        Button("ResetBtn", () => { if (speed != null) speed.Reset(); });
        Button("Play/PauseBtn", TogglePlay);
        Button("SpeedBtn", NextSpeed);
        Button("BackBtn", ScreenFlow.ShowLeaveConfirm);
        Button("FieldJournalBtn", ScreenFlow.OpenJournalOverlay);   // opens on top, Back returns to this run
        Button("EndingGalleryBtn", GuidedScreens.OpenGallery);       // Guided HUD only; opens on top too
        if (overview != null) Button("DetailBtn", () => ShowExpanded(true), overview.transform);
        if (expanded != null) Button("DetailBtn", () => ShowExpanded(false), expanded.transform);
        if (inspection != null)
        {
            Button("CrossBtn", CloseInspection, inspection.transform);
            Button("ViewMoreBtn", () => { FieldJournalScreen.Request(FieldJournalScreen.Page.Facts); ScreenFlow.OpenJournalOverlay(); }, inspection.transform);   // sits under the animal's fun fact, so open the journal's Did You Know? page
        }

        ScreenFlow.AddLeaveButton(ownButton: false);   // the confirm box, opened by BackBtn
        HideOldHud(true);
        SetText("EcosystemTxt", "Himalayan Highlands");
        UpdateSpeedLabel();
        UpdatePlayLabel();
    }

    // The mock-up has a full-screen background that hid the whole world and blocked taps: make any
    // full-screen image clear and click-through. Also let every text shrink to fit its box.
    // Icon spots in the mock-up (ValueImage, AnimalImage, GrassImage, FeedbackImage, ...) are plain white or
    // green squares until the icons are drawn. Kids read them as buttons and tap them, so hide any that has no
    // picture yet; once Shu Min / Nomitha drop a sprite in, it shows again by itself.
    static bool IsIconPlaceholder(Image img)
    {
        string n = img.name.Trim().Trim('\'').Trim();
        return img.sprite == null && (n.EndsWith("Image") || n.EndsWith("Img") || n == "Image")
               && img.GetComponentInChildren<TMP_Text>(true) == null;   // a box holding text is a panel, not an icon
    }

    void SeeThrough()
    {
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            var rt = img.rectTransform;
            // Only the screen-sized background, not the cards (which also stretch, but inset from the edges).
            bool fullScreen = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.sizeDelta.x > -60f && rt.sizeDelta.y > -60f;
            if (img.GetComponent<Button>() != null) continue;
            if (fullScreen)
            {
                var c = img.color; c.a = 0f; img.color = c;
                img.raycastTarget = false;
            }
            else if (IsIconPlaceholder(img)) img.enabled = false;
        }
        foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.enableAutoSizing) continue;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = Mathf.Max(18f, t.fontSize * 0.6f);   // shrink to fit, but stay readable on a tablet
            t.enableAutoSizing = true;
        }
    }

    /// <summary>The previous HUD in HimalayaSimulation, now replaced by Shumin's. Also called while Free mode
    /// is still on the Configuration screen, so the old buttons never show. withHud moves the live weather
    /// line up, clear of the new control bar.</summary>
    public static bool HideOldHud(bool withHud)
    {
        var sim = SceneManager.GetSceneByName(ScreenFlow.Simulation);
        if (!sim.IsValid()) return false;
        bool moved = false;
        var hide = new HashSet<string> { "Play", "Pause", "Reset", "1x", "2x", "4x", "8x", "StatsTabs", "Speed1xButton", "Speed2xButton", "Speed4xButton", "Speed8xButton", "PlayButton", "PauseButton", "ResetButton" };
        foreach (var root in sim.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (hide.Contains(t.name)) t.gameObject.SetActive(false);
                else if (withHud && t.TryGetComponent(out TMP_Text txt) && txt.text != null && (txt.text.StartsWith("Live Everest") || txt.text.Contains("Temp:")) && t is RectTransform rt && !movedWeather.Contains(rt))
                {
                    movedWeather.Add(rt);
                    // Top-left, under the "Himalayan Highlands" title box: the bottom-left is the control bar.
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(48f, -185f);   // clear of the title box
                    txt.alignment = TextAlignmentOptions.TopLeft;

                    // White text was lost on the snowy peaks: the same dark pill as the day panel behind it.
                    var bg = new GameObject("Weather Bg", typeof(RectTransform), typeof(Image));
                    bg.transform.SetParent(rt.parent, false);
                    bg.transform.SetSiblingIndex(rt.GetSiblingIndex());   // drawn just before (under) the text
                    var brt = (RectTransform)bg.transform;
                    brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
                    Vector2 size = txt.GetPreferredValues(txt.text);
                    brt.anchoredPosition = rt.anchoredPosition + new Vector2(-14f, 8f);
                    brt.sizeDelta = size + new Vector2(28f, 16f);
                    var img = bg.GetComponent<Image>();
                    img.color = new Color(0.08f, 0.1f, 0.14f, 0.75f);
                    img.raycastTarget = false;
                }
                if (withHud && t is RectTransform r2 && movedWeather.Contains(r2)) moved = true;
            }
        return moved;
    }
    static readonly HashSet<RectTransform> movedWeather = new HashSet<RectTransform>();

    // ---------- Controls ----------

    void TogglePlay()
    {
        if (speed == null) return;
        if (speed.IsRunning) speed.Pause(); else speed.Play();
        UpdatePlayLabel();
    }

    void NextSpeed()
    {
        if (speed == null) return;
        speed.CycleSpeed();
        UpdateSpeedLabel();
    }

    // Read from the controller, not kept here: the speed also changes from the Field Journal graph and on Reset.
    void UpdateSpeedLabel()
    {
        int speedIndex = speed != null ? Mathf.Max(0, System.Array.IndexOf(SimulationSpeedController.Steps, speed.CurrentSpeed)) : 0;
        string[] names = { "1xTxt", "2xTxt", "4xTxt", "8xTxt" };
        for (int i = 0; i < names.Length; i++) { var t = Find(names[i]); if (t != null) t.gameObject.SetActive(i == speedIndex); }
    }

    void UpdatePlayLabel()
    {
        bool running = speed == null || speed.IsRunning;
        // The button shows what pressing it does.
        var play = Find("PlayTxt"); var pause = Find("PauseTxt");
        if (play != null) play.gameObject.SetActive(!running);
        if (pause != null) pause.gameObject.SetActive(running);
    }

    void ShowExpanded(bool on)
    {
        if (expanded != null) expanded.SetActive(on);
        if (overview != null) overview.SetActive(!on);
    }

    void CloseInspection()
    {
        shown = null;
        if (inspection != null) inspection.SetActive(false);
        if (AnimalInfoUI.Instance != null) AnimalInfoUI.Instance.HidePanel();
    }

    // ---------- Live data ----------

    void Update()
    {
        // The old grey card stays hidden; its selection drives Shumin's inspection panel.
        var info = AnimalInfoUI.Instance;
        if (info != null && info.panel != null && info.panel.activeSelf) info.panel.SetActive(false);
        var selected = info != null ? info.SelectedAnimal : null;
        if (selected != null && selected.IsCaught) selected = null;
        if (selected != shown)
        {
            shown = selected;
            if (shown != null) Mascot.NoteInspected(shown);
            if (inspection != null) inspection.SetActive(shown != null);
        }

        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.3f;
        UpdatePlayLabel();
        UpdateSpeedLabel();
        // The live weather line gets its text a moment after loading: keep looking until it is moved clear of the controls.
        if (!weatherMoved) weatherMoved = HideOldHud(true);
        RefreshOverview();
        if (shown != null) RefreshInspection(shown);
    }

    void RefreshOverview()
    {
        int day = clock != null ? clock.Day : 1;
        if (!ScreenFlow.GuidedMode)   // Guided's day panel is filled by GuidedChallenge
        {
            StyleFreeDayPanel();
            SetText("Text (TMP)", $"<b>Day {day}</b>   {SeasonWeather.CurrentSeason} · {SeasonWeather.Describe(SeasonWeather.CurrentWeather)} · {SeasonWeather.Temperature:0}°C", Find("DayPanel"));
        }

        int sheep = 0, leopards = 0;
        foreach (var a in AnimalBrain.All) if (a != null && !a.IsCaught) { if (a.IsPredatorSpecies) leopards++; else sheep++; }
        float veg = ResourcePatch.VegetationPercent();
        float water = WaterPercent();

        // Small overview card: four numbers.
        if (overview != null)
        {
            var p = overview.transform;
            SetText("ValueTxt", sheep.ToString(), Find("AnimalPanel_01", p));
            SetText("ValueTxt", leopards.ToString(), Find("AnimalPanel_02", p));
            SetText("ValueTxt", $"{veg:0}%", Find("VegetationPanel", p));
            SetText("ValueTxt", $"{water:0}%", Find("WaterPanel", p));
        }

        // Expanded card: number (ValueNoTxt), trend (ValueTxt), status pill, bar and a line for children.
        if (expanded != null)
        {
            var p = expanded.transform;
            var vp = Find("VegetationPanel", p);
            SetText("ValueNoTxt", $"{veg:0}%", vp);
            SetText("ValueTxt", Trend("veg", veg, 2f), vp);
            SetText("Text (TMP)", veg >= 50f ? "Enough" : veg >= 30f ? "Low" : "Very low", Find("Image", vp));
            SetBar(Find("Bar_Fill", vp), veg / 100f, Find("Bar_BG", vp));
            SetText("VegetationTxt", veg < 30f ? "The grass is running low! Blue Sheep may go hungry." : veg < 60f ? "Some grass has been eaten. It grows back over time." : "Plenty of grass for the Blue Sheep to eat.", vp);

            var wp = Find("WaterPanel", p);
            SetText("ValueNoTxt", $"{water:0}%", wp);
            SetText("ValueTxt", Trend("water", water, 2f), wp);
            SetText("Text (TMP)", water >= 50f ? "Enough" : water >= 20f ? "Low" : "Very low", Find("Status", wp));
            SetBar(Find("Bar_Fill", wp), water / 100f, Find("Bar_BG", wp));
            SetText("VegetationTxt", water >= 50f ? "Melting glaciers keep the streams full." : "The streams are running low. Animals may get thirsty.", wp);

            var sp = Find("AnimalPanel_01", p);
            SetText("HeaderTxt", "Blue Sheep", sp);
            SetText("ValueNoTxt", sheep.ToString(), sp);
            SetText("ValueTxt", Trend("sheep", sheep, 0.5f), sp);
            SetText("AnimalTxt", sheep == 0 ? "No Blue Sheep left. Snow Leopards will run out of food." : "Blue Sheep graze the meadows. Fewer sheep mean less food for Snow Leopards.", sp);

            var lp = Find("AnimalPanel_02", p);
            SetText("HeaderTxt", "Snow Leopard", lp);
            SetText("ValueNoTxt", leopards.ToString(), lp);
            SetText("ValueTxt", Trend("leopards", leopards, 0.5f), lp);
            SetText("AnimalTxt", leopards == 0 ? "No Snow Leopards. Nothing hunts the Blue Sheep now." : "Snow Leopards hunt Blue Sheep. More leopards mean more hunting.", lp);
        }
    }

    // Increasing / Decreasing / Stable, compared with the value about 20 real seconds ago.
    readonly Dictionary<string, (float value, float time)> history = new Dictionary<string, (float, float)>();
    readonly Dictionary<string, string> trends = new Dictionary<string, string>();
    string Trend(string key, float now, float threshold)
    {
        if (!history.TryGetValue(key, out var h)) { history[key] = (now, Time.unscaledTime); trends[key] = "Stable"; }
        else if (Time.unscaledTime - h.time > 20f)
        {
            trends[key] = now > h.value + threshold ? "Increasing" : now < h.value - threshold ? "Decreasing" : "Stable";
            history[key] = (now, Time.unscaledTime);
        }
        return trends[key];
    }

    void RefreshInspection(AnimalBrain a)
    {
        var p = inspection.transform;
        string name = a.species_data != null
            ? System.Text.RegularExpressions.Regex.Replace(a.species_data.speciesName, "(?<=[a-z])(?=[A-Z])", " ") : a.name;
        string sex = a.AnimalSex == AnimalBrain.Sex.Male ? "<color=#3A7BD5>♂ Male</color>" : "<color=#D5487A>♀ Female</color>";
        SetText("AnimalTxt", (a.IsYoung ? "Baby " : "") + name + "  <size=60%>" + sex + "</size>", p);

        var cond = Find("ConditionPanel", p);
        float hydration = a.HasThirst ? a.curr_thirst : 100f;
        float health = a.maxHealth > 0f ? a.curr_health / a.maxHealth * 100f : 100f;   // research HP: falls only while food or water is empty
        Meter(Find("FullnessTxt", cond), "FullnessBar", a.curr_hunger);
        Meter(Find("HydrationTxt", cond), "HydrationBar", hydration);
        Meter(Find("HealthTxt", cond), "HealthBar", health);
        string need = a.curr_hunger <= 0f ? "Starving" : a.HasThirst && a.curr_thirst <= 0f ? "Very thirsty"
                    : a.curr_hunger <= 50f || (a.HasThirst && a.curr_thirst <= 50f) ? "Hungry or thirsty" : "Healthy";
        SetText("CurrentConditionTxt", health < 40f ? "Weak" : need, cond);

        SetText("ActionTxt", ActionWords(a), p);
        SetText("DescriptionTxt", Describe(a), p);
        if (factAnimal != a) { factAnimal = a; fact = FieldJournalScreen.NextFactFor(a.IsPredatorSpecies); }   // new fact per tap, steady while open
        SetText("DYKTxt", fact, Find("DYKPanel", p));
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): crossing the river or a stream at a ford (same check as the swim animation).
    static bool Swimming(AnimalBrain a) => HimalayaRiver.InWater(a.transform.position, AnimalMotionBridge.SwimWithin);

    static string ActionWords(AnimalBrain a) => Swimming(a)
        ? (a.CurrentAction == AnimalBrain.AnimalAction.Fleeing ? "Swimming away!" : "Swimming")
        : a.CurrentAction switch
    {
        AnimalBrain.AnimalAction.EatingGrass => "Grazing",
        AnimalBrain.AnimalAction.DrinkingWater => "Drinking",
        AnimalBrain.AnimalAction.Hunting => a.IsStalking ? "Stalking" : "Hunting",
        AnimalBrain.AnimalAction.Fleeing => "Running away!",
        AnimalBrain.AnimalAction.Reproducing => "Looking for a mate",
        _ => "Exploring",
    };

    static string Describe(AnimalBrain a)
    {
        string who = a.IsPredatorSpecies ? "This Snow Leopard" : "This Blue Sheep";
        if (Swimming(a))
            return a.CurrentAction switch
            {
                AnimalBrain.AnimalAction.Fleeing => $"{who} is swimming across the water to get away!",
                AnimalBrain.AnimalAction.Hunting => $"{who} is swimming across the water after a Blue Sheep!",
                _ => $"{who} is swimming across at a shallow crossing.",
            };
        return a.CurrentAction switch
        {
            AnimalBrain.AnimalAction.EatingGrass => $"{who} is eating grass to stay full.",
            AnimalBrain.AnimalAction.DrinkingWater => $"{who} is drinking from the stream.",
            AnimalBrain.AnimalAction.Hunting => a.IsStalking ? $"{who} is creeping up quietly on the herd." : $"{who} is chasing a Blue Sheep!",
            AnimalBrain.AnimalAction.Fleeing => $"{who} spotted a Snow Leopard and is running to safety.",
            AnimalBrain.AnimalAction.Reproducing => $"{who} is well fed and looking for a partner.",
            _ => $"{who} is wandering around its home.",
        };
    }

    static float WaterPercent()
    {
        float sum = 0f; int n = 0;
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.Resource == null || !p.Resource.name.ToLowerInvariant().Contains("water")) continue;
            float max = p.Resource.maxAmount > 0f ? p.Resource.maxAmount : 100f;
            sum += p.Amount / max; n++;
        }
        return n > 0 ? sum / n * 100f : 100f;
    }

    // Free mode: the same dark one-line day pill as Guided, widened leftwards (its right edge stays put)
    // so the season and weather fit; the separate season pill then hides.
    bool dayStyled;
    void StyleFreeDayPanel()
    {
        if (dayStyled) return;
        dayStyled = true;
        DramaDirector.UpdateStatusVisibility();
        var panel = Find("DayPanel") as RectTransform;
        if (panel == null) return;
        float right = panel.anchoredPosition.x + (1f - panel.pivot.x) * panel.sizeDelta.x;
        panel.pivot = new Vector2(1f, panel.pivot.y);
        panel.sizeDelta = new Vector2(Mathf.Max(panel.sizeDelta.x, 480f), panel.sizeDelta.y);
        panel.anchoredPosition = new Vector2(right, panel.anchoredPosition.y);
        if (!panel.TryGetComponent(out Image bg)) bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.75f);
        bg.raycastTarget = false;
        var t = Txt("Text (TMP)", panel);
        if (t == null) return;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 4f); rt.offsetMax = new Vector2(-16f, -4f);
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMax = 30f;
        t.fontSizeMin = 22f;
    }

    void SetText(string name, string value, Transform under = null)
    {
        if (under == null && byName.TryGetValue(name, out var l) && l.Count > 1) return;   // ambiguous without a parent
        var t = Txt(name, under);
        if (t != null && t.text != value) t.text = value;
    }

    // One row of the inspection panel: the "75%" label and the bar under a FullnessTxt / HydrationTxt / HealthTxt.
    void Meter(Transform row, string bar, float percent)
    {
        if (row == null) return;
        SetText("AmountTxt", $"{percent:0}%", row);
        SetBar(Find(bar + "_Fill", row), percent / 100f, Find(bar + "_BG", row));
    }

    // The mock-up bars are plain images: size the fill to a share of its grey track (Bar_BG), from the track's left edge.
    static void SetBar(Transform fill, float value, Transform track)
    {
        var f = fill as RectTransform; var bg = track as RectTransform;
        if (f == null) return;
        if (bg == null) { SetBar(fill, value); return; }
        f.anchorMin = bg.anchorMin; f.anchorMax = bg.anchorMax;
        f.pivot = new Vector2(0f, bg.pivot.y);
        float width = bg.rect.width;
        f.anchoredPosition = bg.anchoredPosition - new Vector2(bg.pivot.x * width, 0f);
        f.sizeDelta = new Vector2(width * Mathf.Clamp01(value), f.sizeDelta.y);
        f.localScale = Vector3.one;
    }

    static void SetBar(Transform fill, float value)
    {
        if (fill == null) return;
        var rt = fill as RectTransform;
        if (rt != null && rt.pivot.x != 0f)
        {
            float shift = rt.pivot.x * rt.rect.width * rt.localScale.x;   // keep it in place while moving the pivot to the left edge
            rt.pivot = new Vector2(0f, rt.pivot.y);
            rt.anchoredPosition -= new Vector2(shift, 0f);
        }
        var s = fill.localScale;
        fill.localScale = new Vector3(Mathf.Clamp01(value), s.y, s.z);
    }
}
