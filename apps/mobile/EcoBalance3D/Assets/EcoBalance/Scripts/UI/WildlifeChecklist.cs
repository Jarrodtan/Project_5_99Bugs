// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Free Exploration "Spot these animals!" checklist. Watches <see cref="AnimalBrain.All"/> every
/// half-second of real time and ticks child-friendly behaviours. Built in code by
/// <see cref="FieldJournalInstaller"/>; never shown in Guided mode.
/// </summary>
public class WildlifeChecklist : MonoBehaviour
{
    sealed class SpotItem
    {
        public string id;
        public string label;
        public string speciesName;   // null = any species
        public Func<AnimalBrain, bool> match;
    }

    const float PollSeconds = 0.5f;
    const float ToastSeconds = 3.5f;
    const float PanelWidth = 520f;
    const float HeaderHeight = 84f;
    const float RowHeight = 48f;
    const float CelebrationHeight = 56f;
    // Overview / start zoom is ~110; Family follow is ~12–48. Above this = too far to "spot".
    const float MaxSpotDistance = 90f;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): 55 missed herds at the normal play zoom

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): Shu Min's HUD look (cream card, brown edge, dark ink, Playpen font)
    static readonly Color CardColor = new(0.98f, 0.94f, 0.84f, 0.97f);
    static readonly Color EdgeColor = new(0.45f, 0.32f, 0.18f);
    static readonly Color TitleColor = new(0.22f, 0.16f, 0.1f);
    static readonly Color BodyColor = new(0.32f, 0.25f, 0.18f);
    static readonly Color Green = new(0.3f, 0.62f, 0.38f);
    static readonly Color TickedColor = new(0.35f, 0.55f, 0.4f);
    static readonly Color MutedGreen = new(0.45f, 0.58f, 0.48f);

    static readonly HashSet<string> spotted = new();
    static WildlifeChecklist instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        spotted.Clear();
        instance = null;
    }

    SpotItem[] items;
    TextMeshProUGUI[] rowLabels;
    TextMeshProUGUI[] rowChecks;
    TextMeshProUGUI countText;
    TextMeshProUGUI celebrationText;
    RectTransform panel;
    RectTransform body;
    RectTransform toastCard;
    TextMeshProUGUI toastText;
    CanvasGroup toastGroup;
    GameObject canvasGo;
    EcosystemSimulator simulator;
    Camera viewCam;
    TerrainCameraController terrainCam;
    AudioSource clickSource;
    AudioClip clickClip;
    float nextPollAt;
    float toastHideAt = -1f;
    bool expanded = true;
    bool celebrated;
    bool uiBuilt;

    /// <summary>Attaches the checklist to the simulation object. Call only after the Guided-mode early return.</summary>
    public static WildlifeChecklist Install(GameObject simGo)
    {
        if (simGo == null) return null;
        spotted.Clear();
        if (instance != null)
        {
            if (instance.gameObject == simGo)
            {
                instance.BindSimulator(simGo.GetComponent<EcosystemSimulator>());
                instance.RefreshAllRows();
                return instance;
            }
            Destroy(instance);
        }

        var checklist = simGo.GetComponent<WildlifeChecklist>();
        if (checklist == null) checklist = simGo.AddComponent<WildlifeChecklist>();
        checklist.BindSimulator(simGo.GetComponent<EcosystemSimulator>());
        spotted.Clear();
        checklist.celebrated = false;
        checklist.RefreshAllRows();
        instance = checklist;
        return checklist;
    }

    void BindSimulator(EcosystemSimulator sim)
    {
        if (simulator != null) simulator.EcosystemReset -= OnEcosystemReset;
        simulator = sim;
        if (simulator != null) simulator.EcosystemReset += OnEcosystemReset;
    }

    void OnEcosystemReset()
    {
        spotted.Clear();
        celebrated = false;
        RefreshAllRows();
        HideToast();
    }

    void OnDestroy()
    {
        if (simulator != null) simulator.EcosystemReset -= OnEcosystemReset;
        if (instance == this) instance = null;
    }

    void Awake()
    {
        items = BuildItems();
        rowLabels = new TextMeshProUGUI[items.Length];
        rowChecks = new TextMeshProUGUI[items.Length];
    }

    void Update()
    {
        EnsureUI();
        UpdateCanvasVisibility();
        UpdateToast();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleDevHotkey();
#endif
        if (Time.unscaledTime < nextPollAt) return;
        nextPollAt = Time.unscaledTime + PollSeconds;
        Poll();
    }

    void EnsureUI()
    {
        if (uiBuilt) return;
        BuildUI();
        uiBuilt = true;
        RefreshAllRows();
    }

    void UpdateCanvasVisibility()
    {
        if (canvasGo == null) return;
        bool show = !ConfigurationScreen.Open;
        if (canvasGo.activeSelf != show) canvasGo.SetActive(show);
    }

    void UpdateToast()
    {
        if (toastCard == null || !toastCard.gameObject.activeSelf) return;
        if (Time.unscaledTime >= toastHideAt) HideToast();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void HandleDevHotkey()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.f9Key.wasPressedThisFrame) return;
        for (int i = 0; i < items.Length; i++) spotted.Add(items[i].id);
        RefreshAllRows();
        HideToast();
        ShowCelebrationIfComplete();
    }
#endif

    void Poll()
    {
        if (items == null) return;
        if (viewCam == null) viewCam = Camera.main;
        if (viewCam == null) return;
        var modes = CameraModeController.Instance;
        if (modes != null && modes.Mode == CameraMode.Ecosystem) return;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): any animal close and on screen counts, not only the one
        // being followed or tapped; children didn't know they had to select one, so nothing ever ticked.
        var focused = ResolveFocusedBrain(modes);
        if (focused != null && focused.isActiveAndEnabled) Check(focused, true);   // tapped / followed: counts at any zoom
        foreach (var other in AnimalBrain.All)
            if (other != null && other != focused && other.isActiveAndEnabled) Check(other, false);
    }

    void Check(AnimalBrain brain, bool focused)
    {
        Vector3 pos = brain.transform.position;
        if (!focused && (pos - viewCam.transform.position).sqrMagnitude > MaxSpotDistance * MaxSpotDistance) return;
        if (!IsOnScreen(viewCam, pos)) return;

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (spotted.Contains(item.id)) continue;
            if (item.speciesName != null)
            {
                if (brain.species_data == null) continue;
                if (brain.species_data.speciesName != item.speciesName) continue;
            }
            if (!item.match(brain)) continue;
            Tick(item, i);
        }
    }

    AnimalBrain ResolveFocusedBrain(CameraModeController modes)
    {
        if (terrainCam == null) terrainCam = FindAnyObjectByType<TerrainCameraController>();
        if (terrainCam != null)
        {
            var target = terrainCam.FollowTarget;
            if (target != null)
            {
                var fromFollow = target.GetComponent<AnimalBrain>()
                    ?? target.GetComponentInChildren<AnimalBrain>()
                    ?? target.GetComponentInParent<AnimalBrain>();
                if (fromFollow != null) return fromFollow;
            }
        }

        if (AnimalInfoUI.Instance != null && AnimalInfoUI.Instance.SelectedAnimal != null)
            return AnimalInfoUI.Instance.SelectedAnimal;

        if (modes != null &&
            (modes.Mode == CameraMode.Family || modes.Mode == CameraMode.OtherSpecies) &&
            modes.Lead != null)
        {
            return modes.Lead.GetComponent<AnimalBrain>()
                ?? modes.Lead.GetComponentInChildren<AnimalBrain>()
                ?? modes.Lead.GetComponentInParent<AnimalBrain>();
        }

        return null;
    }

    static bool IsOnScreen(Camera cam, Vector3 worldPos)
    {
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    void Tick(SpotItem item, int index)
    {
        if (!spotted.Add(item.id)) return;
        RefreshRow(index);
        UpdateCount();
        ShowToast(item.label);
        PlayClick();
        ShowCelebrationIfComplete();
    }

    void ShowCelebrationIfComplete()
    {
        if (celebrated || spotted.Count < items.Length) return;
        celebrated = true;
        if (celebrationText != null)
        {
            celebrationText.gameObject.SetActive(true);
            ResizePanel();
        }
    }

    void RefreshAllRows()
    {
        if (rowLabels == null) return;
        for (int i = 0; i < items.Length; i++) RefreshRow(i);
        UpdateCount();
        if (celebrationText != null)
        {
            bool all = spotted.Count >= items.Length;
            celebrated = all;
            celebrationText.gameObject.SetActive(all);
            ResizePanel();
        }
    }

    void RefreshRow(int index)
    {
        if (rowLabels == null || rowLabels[index] == null) return;
        bool done = spotted.Contains(items[index].id);
        rowChecks[index].text = done ? "✓" : "○";
        rowChecks[index].color = done ? Green : BodyColor;
        rowLabels[index].text = items[index].label;
        rowLabels[index].color = done ? TickedColor : BodyColor;
        rowLabels[index].fontStyle = done ? FontStyles.Strikethrough : FontStyles.Normal;
    }

    void UpdateCount()
    {
        if (countText == null) return;
        countText.text = spotted.Count + "/" + items.Length;
    }

    void ShowToast(string label)
    {
        if (toastCard == null) return;
        toastText.text = "You spotted: " + label + "!";
        toastCard.gameObject.SetActive(true);
        toastGroup.alpha = 1f;
        toastHideAt = Time.unscaledTime + ToastSeconds;
    }

    void HideToast()
    {
        toastHideAt = -1f;
        if (toastCard != null) toastCard.gameObject.SetActive(false);
    }

    void PlayClick()
    {
        if (clickClip == null) clickClip = Resources.Load<AudioClip>("UISound/UI Button Click Positive_2");
        if (clickClip == null) return;
        if (clickSource == null)
        {
            clickSource = gameObject.AddComponent<AudioSource>();
            clickSource.playOnAwake = false;
            clickSource.spatialBlend = 0f;
            clickSource.ignoreListenerPause = true;
        }
        if (AudioManager.Instance != null && AudioManager.Instance.sfx != null)
            clickSource.outputAudioMixerGroup = AudioManager.Instance.sfx.outputAudioMixerGroup;
        clickSource.PlayOneShot(clickClip);
    }

    void ToggleExpand()
    {
        expanded = !expanded;
        if (body != null) body.gameObject.SetActive(expanded);
        ResizePanel();
    }

    void ResizePanel()
    {
        if (panel == null) return;
        float h = HeaderHeight;
        if (expanded)
        {
            h += items.Length * RowHeight + 16f;
            if (celebrationText != null && celebrationText.gameObject.activeSelf)
                h += CelebrationHeight;
        }
        panel.sizeDelta = new Vector2(PanelWidth, h);
        PlaceToast();
    }

    void PlaceToast()
    {
        if (toastCard == null || panel == null) return;
        toastCard.anchoredPosition = new Vector2(panel.anchoredPosition.x, panel.anchoredPosition.y - panel.sizeDelta.y - 12f);
    }

    void BuildUI()
    {
        canvasGo = new GameObject("Wildlife Checklist Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1600);
        scaler.matchWidthOrHeight = 0.5f;

        panel = Panel(canvasGo.transform, "Panel", CardColor);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(36f, -360f);
        panel.sizeDelta = new Vector2(PanelWidth, HeaderHeight);

        var header = Panel(panel, "Header", new Color(0f, 0f, 0f, 0f));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, HeaderHeight);
        header.gameObject.AddComponent<Button>().onClick.AddListener(ToggleExpand);

        var title = Text(header, "Spot these animals!", 32f, TitleColor, FontStyles.Bold,
            new Vector2(20f, -12f), new Vector2(340f, 48f));
        title.raycastTarget = false;
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): says how to spot, since ticks only count up close
        var hint = Text(header, "Zoom in close to watch the animals", 20f, BodyColor, FontStyles.Italic,
            new Vector2(20f, -46f), new Vector2(PanelWidth - 40f, 26f));
        hint.raycastTarget = false;

        countText = Text(header, "0/" + items.Length, 28f, Green, FontStyles.Bold,
            new Vector2(-20f, -12f), new Vector2(100f, 48f));
        countText.rectTransform.anchorMin = countText.rectTransform.anchorMax = countText.rectTransform.pivot = new Vector2(1f, 1f);
        countText.alignment = TextAlignmentOptions.TopRight;
        countText.raycastTarget = false;

        body = Panel(panel, "Body", new Color(0f, 0f, 0f, 0f));
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(1f, 1f);
        body.pivot = new Vector2(0.5f, 1f);
        body.anchoredPosition = new Vector2(0f, -HeaderHeight);
        body.sizeDelta = new Vector2(0f, items.Length * RowHeight + CelebrationHeight + 16f);
        body.GetComponent<Image>().raycastTarget = false;

        for (int i = 0; i < items.Length; i++)
        {
            float y = -8f - i * RowHeight;
            rowChecks[i] = Text(body, "○", 28f, BodyColor, FontStyles.Bold,
                new Vector2(18f, y), new Vector2(40f, RowHeight - 4f));
            rowChecks[i].alignment = TextAlignmentOptions.MidlineLeft;

            rowLabels[i] = Text(body, items[i].label, 26f, BodyColor, FontStyles.Normal,
                new Vector2(58f, y), new Vector2(PanelWidth - 80f, RowHeight - 4f));
            rowLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
        }

        celebrationText = Text(body, "You found every behaviour! Try a different ecosystem.", 24f, Green, FontStyles.Bold,
            new Vector2(18f, -8f - items.Length * RowHeight), new Vector2(PanelWidth - 40f, CelebrationHeight - 8f));
        celebrationText.alignment = TextAlignmentOptions.TopLeft;
        celebrationText.gameObject.SetActive(false);

        toastCard = Panel(canvasGo.transform, "Toast", CardColor);
        toastCard.anchorMin = toastCard.anchorMax = toastCard.pivot = new Vector2(0f, 1f);
        toastCard.sizeDelta = new Vector2(PanelWidth, 88f);
        toastGroup = toastCard.gameObject.AddComponent<CanvasGroup>();
        toastGroup.blocksRaycasts = false;
        toastCard.GetComponent<Image>().raycastTarget = false;

        var accent = Panel(toastCard, "Accent", Green);
        accent.anchorMin = Vector2.zero;
        accent.anchorMax = new Vector2(0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.sizeDelta = new Vector2(12f, 0f);
        accent.GetComponent<Image>().raycastTarget = false;

        toastText = Text(toastCard, "", 28f, TitleColor, FontStyles.Bold,
            new Vector2(28f, -18f), new Vector2(PanelWidth - 48f, 56f));
        toastText.alignment = TextAlignmentOptions.MidlineLeft;
        toastCard.gameObject.SetActive(false);

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): same edge and font as the other HUD cards
        foreach (var card in new[] { panel, toastCard })
        {
            var edge = card.gameObject.AddComponent<Outline>();
            edge.effectColor = EdgeColor;
            edge.effectDistance = new Vector2(4f, -4f);
        }
        var font = ScreenFlow.ShuminFont("PlaypenSans-SemiBold") ?? ScreenFlow.ShuminFont("PlaypenSans");
        if (font != null)
            foreach (var t in canvasGo.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.text != "○" && t.text != "✓") t.font = font;   // keep the tick marks in a font that has them

        UIClickSound.AddToAll();
        ResizePanel();
        if (ConfigurationScreen.Open) canvasGo.SetActive(false);
    }

    static SpotItem[] BuildItems()
    {
        return new[]
        {
            Item("sheep_eat", "Sheep eats grass", "BlueSheep",
                a => a.CurrentAction == AnimalBrain.AnimalAction.EatingGrass && a.IsHeadDown),
            Item("sheep_drink", "Sheep drinks water", "BlueSheep",
                a => a.CurrentAction == AnimalBrain.AnimalAction.DrinkingWater && a.IsHeadDown),
            Item("sheep_flee", "Sheep runs away from danger", "BlueSheep",
                a => a.CurrentAction == AnimalBrain.AnimalAction.Fleeing),
            Item("leopard_hunt", "Leopard hunts", "SnowLeopard",
                a => a.CurrentAction == AnimalBrain.AnimalAction.Hunting),
            Item("leopard_drink", "Leopard drinks water", "SnowLeopard",
                a => a.CurrentAction == AnimalBrain.AnimalAction.DrinkingWater && a.IsHeadDown),
            Item("mate", "Two animals mate", null,
                a => a.IsMating),
            Item("hungry", "A hungry animal", null, a => a.curr_hunger < 30f),
            Item("thirsty", "A thirsty animal", null, a => a.curr_thirst < 30f),
        };
    }

    static SpotItem Item(string id, string label, string species, Func<AnimalBrain, bool> match)
    {
        return new SpotItem { id = id, label = label, speciesName = species, match = match };
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
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = size * 0.6f;
        return t;
    }
}
