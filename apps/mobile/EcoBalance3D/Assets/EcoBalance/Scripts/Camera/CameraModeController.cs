// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Family / whole-map / other-species camera modes on top of <see cref="TerrainCameraController"/>.
/// Builds its own button bar at runtime; only appears once a terrain camera exists in the scene.
/// Camera only: never reads or writes simulation or story state.
/// </summary>
public enum CameraMode { Free, Family, Ecosystem, OtherSpecies }

public class CameraModeController : MonoBehaviour
{
    public static CameraModeController Instance { get; private set; }

    /// <summary>Optional family list from SCRUM-379 (main animal first). Null = filter AnimalBrain.All by MainSpecies.</summary>
    public static Func<IReadOnlyList<Transform>> FamilySource;

    /// <summary>Species the player follows; Other Species watches the counterpart.</summary>
    public static string MainSpecies = "BlueSheep";

    /// <summary>Top-centre offset in canvas units: one segmented "view" bar just under the Day / season banner.</summary>
    public static readonly Vector2 BarAnchoredPosition = new Vector2(0f, -225f);

    public CameraMode Mode { get; private set; } = CameraMode.Free;
    public event Action<CameraMode> ModeChanged;

    const float GroupRadius = 30f;
    const int MaxCompanions = 6;
    const float BaseFollowDistance = 12f;

    TerrainCameraController cam;
    Transform anchor;
    Transform lead;
    IReadOnlyList<Transform> activeList;
    int observedIndex;
    bool followArmed;

    GameObject canvasRoot;
    Image[] modeButtons;
    TextMeshProUGUI[] modeLabels;
    static readonly Color IdleColor = new Color(0.08f, 0.1f, 0.14f, 0.88f);
    static readonly Color ActiveColor = new Color(0.25f, 0.62f, 0.35f, 0.95f);

    // Shu Min's look, borrowed from the simulation UI once it is on screen: cream pill (speed / Field Journal
    // buttons) when idle, her green pill when active, Chewy font, dark ink text on cream.
    static readonly Color Ink = new Color(0.22f, 0.16f, 0.1f);
    Sprite creamSprite, greenSprite;
    Image barImage;
    bool styled;

    void TryStyleLikeShumin()
    {
        if (styled || modeButtons == null) return;
        creamSprite = ScreenFlow.ShuminSprite("SpeedBtn", "FieldJournalBtn");
        greenSprite = ScreenFlow.ShuminSprite(ScreenFlow.GreenButtons);
        if (creamSprite == null || greenSprite == null) return;   // UI not loaded yet: try again next frame
        var font = ScreenFlow.ShuminFont("Chewy");
        barImage.sprite = creamSprite;   // one cream pill holds all three; only the chosen view is green
        barImage.type = Image.Type.Sliced;
        barImage.color = Color.white;
        var edge = barImage.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(0.45f, 0.32f, 0.18f, 0.6f);
        edge.effectDistance = new Vector2(2f, -2f);
        for (int i = 0; i < modeButtons.Length; i++)
        {
            modeButtons[i].type = Image.Type.Sliced;
            if (font != null) modeLabels[i].font = font;
        }
        styled = true;
        RefreshHighlights();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[CameraModeController]");
        DontDestroyOnLoad(go);
        go.AddComponent<CameraModeController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildUI();
        EnsureAnchor();
        SetBarVisible(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        cam = null;
        lead = null;
        activeList = null;
        followArmed = false;
        styled = false;
        Mode = CameraMode.Free;
        SetBarVisible(false);
        RefreshHighlights();
        UseFamilyStory();
    }

    // SCRUM-380 + SCRUM-384: in Guided, "Follow" is the chosen story's named family (Milo or Snowy, main animal
    // first) and "Watch" is the other species. Free mode has no family, so it falls back to the whole species.
    static readonly List<Transform> familyScratch = new List<Transform>(8);

    void UseFamilyStory()
    {
        bool snowy = FamilyManager.SelectedStory == FamilyStory.SnowLeopard;
        MainSpecies = snowy ? "SnowLeopard" : "BlueSheep";
        FamilySource = FamilyTransforms;
        if (modeLabels == null) return;
        bool guided = ScreenFlow.GuidedMode;
        modeLabels[0].text = guided ? (snowy ? "Follow Snowy" : "Follow Milo") : (snowy ? "Follow leopards" : "Follow sheep");
        modeLabels[2].text = snowy ? "Watch sheep" : "Watch leopards";
    }

    static IReadOnlyList<Transform> FamilyTransforms()
    {
        if (!FamilyManager.Ready) return null;
        familyScratch.Clear();
        var main = FamilyManager.MainAnimal;
        if (main != null) familyScratch.Add(main.transform);
        foreach (var m in FamilyManager.Members)
            if (m != null && m != main && m.IsAlive) familyScratch.Add(m.transform);
        return familyScratch;
    }

    public void SetMode(CameraMode mode)
    {
        if (mode == CameraMode.Free) return;
        if (cam == null) return;

        bool cycleOther = mode == CameraMode.OtherSpecies && Mode == CameraMode.OtherSpecies;
        if (cycleOther) observedIndex++;

        if (mode == CameraMode.Ecosystem)
        {
            ApplyEcosystem();
            return;
        }

        var list = mode == CameraMode.Family ? BuildFamilyList() : BuildOtherSpeciesList();
        if (list == null || list.Count == 0)
        {
            ApplyEcosystem();
            return;
        }

        activeList = list;
        if (cycleOther || !IsAlive(lead) || !ListContains(list, lead))
            lead = PickLead(list, mode == CameraMode.OtherSpecies ? observedIndex : 0);

        if (!IsAlive(lead))
        {
            ApplyEcosystem();
            return;
        }

        Mode = mode;
        EnsureAnchor();
        UpdateAnchorPose(rebindFollow: true);
        RefreshHighlights();
        ModeChanged?.Invoke(Mode);
    }

    void Update()
    {
        if (cam == null)
        {
            cam = FindAnyObjectByType<TerrainCameraController>();
            if (cam == null) { SetBarVisible(false); return; }
        }

        // Hide the view bar while a Guided card is up (intro, chapter, event): it was drawn over them.
        // PopupOpen is static and can stay true after leaving Guided mid-card, so only trust it in Guided.
        // Free mode has no family story, so no view bar there (the player moves the camera freely).
        bool popup = ScreenFlow.GuidedMode && GuidedChallenge.PopupOpen;
        bool show = ScreenFlow.GuidedMode && !popup && !AnimalInfoUI.IsOpen && !SimHud.InspectionOpen;   // the view bar covered the inspection card's title
        if (canvasRoot != null && canvasRoot.activeSelf != show) SetBarVisible(show);
        if (!show) return;

        TryStyleLikeShumin();
        HandleHotkeys();

        // Animal tap (or DramaDirector / Mascot Follow) during Ecosystem leaves Free with no button lit.
        if (Mode == CameraMode.Ecosystem && cam.FollowTarget != null)
        {
            Mode = CameraMode.Free;
            RefreshHighlights();
            ModeChanged?.Invoke(Mode);
            return;
        }

        if (Mode == CameraMode.Family || Mode == CameraMode.OtherSpecies)
        {
            if (followArmed && cam.FollowTarget != anchor)
            {
                Mode = CameraMode.Free;
                followArmed = false;
                lead = null;
                activeList = null;
                RefreshHighlights();
                ModeChanged?.Invoke(Mode);
                return;
            }

            if (!IsAlive(lead))
            {
                if (activeList == null || CountAlive(activeList) == 0)
                {
                    ApplyEcosystem();
                    return;
                }
                lead = PickLead(activeList, Mode == CameraMode.OtherSpecies ? observedIndex : 0);
                if (!IsAlive(lead))
                {
                    ApplyEcosystem();
                    return;
                }
                UpdateAnchorPose(rebindFollow: false);
            }
            else
            {
                UpdateAnchorPose(rebindFollow: false);
            }
        }
    }

    void HandleHotkeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        // 7/8/9 (not 1/2/3: those are the demo hotkeys in SimulationHotkeys). Numpad 1/2/3 still work.
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SetMode(CameraMode.Family);
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SetMode(CameraMode.Ecosystem);
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SetMode(CameraMode.OtherSpecies);
    }

    void ApplyEcosystem()
    {
        Mode = CameraMode.Ecosystem;
        followArmed = false;
        lead = null;
        activeList = null;
        cam.FrameOverview();
        RefreshHighlights();
        ModeChanged?.Invoke(Mode);
    }

    void EnsureAnchor()
    {
        if (anchor != null) return;
        var go = new GameObject("[CameraModeAnchor]");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        anchor = go.transform;
    }

    void UpdateAnchorPose(bool rebindFollow)
    {
        if (anchor == null || !IsAlive(lead) || activeList == null) return;

        var members = new List<Transform>(MaxCompanions + 1) { lead };
        var scored = new List<(Transform t, float d)>();
        Vector3 leadPos = lead.position;
        for (int i = 0; i < activeList.Count; i++)
        {
            var t = activeList[i];
            if (!IsAlive(t) || t == lead) continue;
            float d = Vector3.Distance(leadPos, t.position);
            if (d <= GroupRadius) scored.Add((t, d));
        }
        scored.Sort((a, b) => a.d.CompareTo(b.d));
        for (int i = 0; i < scored.Count && members.Count < MaxCompanions + 1; i++)
            members.Add(scored[i].t);

        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < members.Count; i++) centroid += members[i].position;
        centroid /= members.Count;

        float maxSpread = 0f;
        for (int i = 0; i < members.Count; i++)
            maxSpread = Mathf.Max(maxSpread, Vector3.Distance(centroid, members[i].position));

        anchor.position = centroid;
        anchor.rotation = Quaternion.Euler(0f, lead.eulerAngles.y, 0f);

        if (rebindFollow)
        {
            cam.StopFollowing();
            cam.Follow(anchor, BaseFollowDistance + maxSpread * 1.2f);
            followArmed = true;
        }
    }

    IReadOnlyList<Transform> BuildFamilyList()
    {
        if (FamilySource != null)
        {
            var fromHook = FamilySource();
            if (fromHook != null && fromHook.Count > 0) return fromHook;   // null/empty (Free mode, family not formed): fall back below
        }
        return CollectSpecies(MainSpecies, matchMain: true);
    }

    IReadOnlyList<Transform> BuildOtherSpeciesList()
    {
        string main = Normalize(MainSpecies);
        if (main == "bluesheep") return CollectSpecies("SnowLeopard", matchMain: true);
        if (main == "snowleopard") return CollectSpecies("BlueSheep", matchMain: true);
        return CollectSpecies(MainSpecies, matchMain: false);
    }

    List<Transform> CollectSpecies(string species, bool matchMain)
    {
        string want = Normalize(species);
        var list = new List<Transform>();
        foreach (var brain in AnimalBrain.All)
        {
            if (brain == null || !brain.isActiveAndEnabled) continue;
            if (brain.species_data == null) continue;
            string name = Normalize(brain.species_data.speciesName);
            bool isMatch = name == want;
            if (matchMain ? !isMatch : isMatch) continue;
            var root = brain.GetComponentInParent<EcoEntity>()?.transform ?? brain.transform;
            if (root != null) list.Add(root);
        }
        return list;
    }

    static Transform PickLead(IReadOnlyList<Transform> list, int index)
    {
        if (list == null || list.Count == 0) return null;
        int start = ((index % list.Count) + list.Count) % list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[(start + i) % list.Count];
            if (IsAlive(t)) return t;
        }
        return null;
    }

    static int CountAlive(IReadOnlyList<Transform> list)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (IsAlive(list[i])) n++;
        return n;
    }

    static bool ListContains(IReadOnlyList<Transform> list, Transform t)
    {
        if (t == null || list == null) return false;
        for (int i = 0; i < list.Count; i++) if (list[i] == t) return true;
        return false;
    }

    static bool IsAlive(Transform t) => t != null && t.gameObject.activeInHierarchy;

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var chars = new char[s.Length];
        int n = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == ' ' || c == '_') continue;
            chars[n++] = char.ToLowerInvariant(c);
        }
        return new string(chars, 0, n);
    }

    void BuildUI()
    {
        canvasRoot = new GameObject("CameraMode Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1600);
        scaler.matchWidthOrHeight = 0.5f;

        var bar = new GameObject("ModeBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        bar.transform.SetParent(canvasRoot.transform, false);
        var brt = (RectTransform)bar.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = BarAnchoredPosition;
        brt.sizeDelta = new Vector2(940f, 100f);   // same height as Field Journal / Ending Gallery
        barImage = bar.GetComponent<Image>();
        barImage.color = IdleColor;
        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        modeButtons = new Image[3];
        modeLabels = new TextMeshProUGUI[3];
        string[] labels = { "Follow family", "Overview", "Other animals" };
        CameraMode[] modes = { CameraMode.Family, CameraMode.Ecosystem, CameraMode.OtherSpecies };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var buttonGo = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(bar.transform, false);
            modeButtons[i] = buttonGo.GetComponent<Image>();
            modeButtons[i].color = IdleColor;
            var btn = buttonGo.GetComponent<Button>();
            var captured = modes[idx];
            btn.onClick.AddListener(() => SetMode(captured));

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(buttonGo.transform, false);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.text = labels[i];
            label.fontSize = 34f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            modeLabels[i] = label;
        }
        RefreshHighlights();
    }

    void SetBarVisible(bool on)
    {
        if (canvasRoot != null) canvasRoot.SetActive(on);
    }

    void RefreshHighlights()
    {
        if (modeButtons == null) return;
        CameraMode[] modes = { CameraMode.Family, CameraMode.Ecosystem, CameraMode.OtherSpecies };
        for (int i = 0; i < modeButtons.Length; i++)
        {
            bool on = Mode == modes[i];
            if (styled)
            {
                modeButtons[i].sprite = greenSprite;
                modeButtons[i].color = on ? Color.white : Color.clear;   // idle segments show the cream bar behind
                modeLabels[i].color = on ? Color.white : Ink;
            }
            else modeButtons[i].color = on ? ActiveColor : IdleColor;
        }
    }
}
