using TMPro;
using UnityEngine;

public class AnimalInfoUI : MonoBehaviour
{
    public static AnimalInfoUI Instance { get; private set; }
    /// <summary>True while the Animal Inspection card is showing (the camera view bar hides so it does not cover its title).</summary>
    public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

    [Header("Panel")]
    public GameObject panel;

    [Header("Text")]
    public TMP_Text nameText;
    public TMP_Text hungerText;
    public TMP_Text thirstText;
    public TMP_Text actionText;
    public TMP_Text targetText;
    // [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
    TMP_Text thoughtText;
    string lastThoughtLine;

    // [AI-GENERATED BEGIN] Claude (Claude Sonnet 5.5, claude.ai chat) - not yet reviewed
    [Header("Sound")]
    public AudioClip popSound;                    // drag your pop clip here
    [Range(0f, 1f)] public float popVolume = 1f;
    public AudioSource sfxSource;                 // optional: drag your AudioManager's SFX AudioSource here so its volume setting applies
    private AudioSource uiAudio;
    // [AI-GENERATED END]

    private AnimalBrain selectedAnimal;
    public AnimalBrain SelectedAnimal => selectedAnimal;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): read by SimHud
    AnimalBrain factAnimal;   // new fun fact each time a different animal is tapped
    string fact;
    public TMP_Text factText;   // wired by IslandPlatformHud / LayoutCard

    private void Awake()
    {
        Instance = this;

        // [AI-GENERATED] Claude (Claude Sonnet 5.5, claude.ai chat): use the SFX source if one was given,
        // otherwise create a plain 2D AudioSource just for UI sounds
        uiAudio = sfxSource != null ? sfxSource : gameObject.AddComponent<AudioSource>();
        if (sfxSource == null)
        {
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;
        }

        if (panel != null)
        {
            LayoutCard();
            AddCloseButton();
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (selectedAnimal == null)
        {
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): the animal was removed (died) while selected
            if (panel != null && panel.activeSelf) HidePanel();
            return;
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): Esc closes it too, and a dead animal's card goes away
        if ((UnityEngine.InputSystem.Keyboard.current != null &&
             UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) ||
            selectedAnimal.IsCaught)
        {
            HidePanel();
            return;
        }

        UpdateUI();
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // The panel was set to stretch over the whole screen with its texts placed from the screen centre,
    // so on a tablet it covered the view and drifted. Make it a fixed card on the right edge, middle height,
    // with the texts stacked top to bottom inside it.
    void LayoutCard()
    {
        var rt = panel.transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-40f, 0f);
        rt.sizeDelta = new Vector2(680f, 900f);
        rt.localScale = Vector3.one;
        if (!panel.TryGetComponent(out UnityEngine.UI.Image bg)) bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.97f, 0.97f, 0.95f, 0.92f);

        TMP_Text fact = null;
        foreach (var t in panel.GetComponentsInChildren<TMP_Text>(true))
            if (t.name == "DidYouKnow") { fact = t; break; }
        factText = fact;
        // [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
        thoughtText = EnsureThoughtRow();
        var rows = new (TMP_Text text, float height, float size)[]
        {
            (nameText, 90f, 52f), (hungerText, 70f, 38f), (thirstText, 70f, 38f),
            (actionText, 70f, 38f), (targetText, 70f, 38f),
            (thoughtText, 90f, 36f), (fact, 280f, 32f),
        };

        float y = -40f;
        foreach (var (text, height, size) in rows)
        {
            if (text == null) continue;
            var t = text.rectTransform;
            // Some texts sat inside another box in the card, so positions were measured from the wrong place.
            // Put every row directly in the card.
            if (t.parent != panel.transform) t.SetParent(panel.transform, false);
            t.anchorMin = new Vector2(0f, 1f); t.anchorMax = new Vector2(1f, 1f);
            t.pivot = new Vector2(0.5f, 1f);
            t.localScale = Vector3.one;
            // Left edge 40 in, right edge 110 in (room for the X); row at y, this tall.
            t.offsetMin = new Vector2(40f, y - height);
            t.offsetMax = new Vector2(text == nameText ? -110f : -40f, y);
            // Shrink to fit its row instead of spilling out of the card (long names, long fun facts).
            // The scene saved old negative margins (e.g. -104 left on Hunger) that pushed text out of the card.
            text.margin = Vector4.zero;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = size * 0.5f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            y -= height + 12f;
        }
    }

    // A big "X" in the panel's top-right corner, built in code so the scene doesn't need changing.
    void AddCloseButton()
    {
        var go = new GameObject("Close", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(panel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -12f);
        rt.sizeDelta = new Vector2(90f, 90f);   // big enough for a child's finger
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.35f);
        go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(HidePanel);

        var label = new GameObject("X", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(go.transform, false);
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        label.text = "X";   // the font has no ✕ symbol (it showed as an empty box)
        label.fontSize = 56f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Cursor (Grok 4.7) - reviewed by Yande
    TMP_Text EnsureThoughtRow()
    {
        foreach (var t in panel.GetComponentsInChildren<TMP_Text>(true))
            if (t.name == "ThoughtLine") return t;

        var go = new GameObject("ThoughtLine", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.color = new Color(0.2f, 0.25f, 0.35f, 1f);
        tmp.fontStyle = FontStyles.Italic;
        return tmp;
    }
    // [AI-GENERATED END]

    public void ShowAnimal(AnimalBrain animal)
    {
        if (animal == null)
            return;

        // [AI-GENERATED] Claude (Claude Sonnet 5.5, claude.ai chat): remember if the card was already open,
        // so the pop only plays when it actually appears
        bool wasOpen = panel != null && panel.activeSelf;

        selectedAnimal = animal;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (!wasOpen) PlayPop();

        UpdateUI();
    }

    // [AI-GENERATED BEGIN] Claude (Claude Sonnet 5.5, claude.ai chat) - not yet reviewed
    private void PlayPop()
    {
        if (popSound == null || uiAudio == null) return;
        uiAudio.PlayOneShot(popSound, popVolume);
    }
    // [AI-GENERATED END]

    public void HidePanel()
    {
        selectedAnimal = null;
        lastThoughtLine = null;   // [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (selectedAnimal == null)
            return;

        // Animal name
        if (nameText != null)
        {
            if (selectedAnimal.species_data != null)
            {
                // [AI-GENERATED] Claude Code (Claude Opus 5.5): "SnowLeopard" -> "Snow Leopard"
                // [AI-GENERATED] Claude Code (Claude Opus 5.5): sex symbol (blue ♂ / pink ♀) and "Baby" so each animal is easy to tell apart
                string sex = selectedAnimal.AnimalSex == AnimalBrain.Sex.Male
                    ? "<color=#3A7BD5>" + selectedAnimal.SexSymbol + " Male</color>"
                    : "<color=#D5487A>" + selectedAnimal.SexSymbol + " Female</color>";
                nameText.text = (selectedAnimal.IsYoung ? "Baby " : "") + System.Text.RegularExpressions.Regex.Replace(
                    selectedAnimal.species_data.speciesName, "(?<=[a-z])(?=[A-Z])", " ") + "  " + sex;
            }
            else
            {
                nameText.text =
                    selectedAnimal.gameObject.name;
            }
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): a fun fact that matches the animal
        if (factText != null)
        {
            if (factAnimal != selectedAnimal) { factAnimal = selectedAnimal; fact = FieldJournalScreen.NextFactFor(selectedAnimal.IsPredatorSpecies); }
            factText.text = "Did you know?\n" + fact;
        }

        // Hunger
        if (hungerText != null)
        {
            hungerText.text =
                $"Hunger: {selectedAnimal.curr_hunger:0}%";
        }

        // Thirst (blank for species that don't need water)
        if (thirstText != null)
        {
            thirstText.text =
                selectedAnimal.HasThirst
                    ? $"Thirst: {selectedAnimal.curr_thirst:0}%"
                    : "";
        }

        // Current action
        if (actionText != null)
        {
            actionText.text =
                HimalayaRiver.InWater(selectedAnimal.transform.position, AnimalMotionBridge.SwimWithin)
                    ? "Currently: Swimming"   // [AI-GENERATED] Claude Code (Claude Opus 5.5): crossing at a ford
                    : $"Currently: {GetActionText(selectedAnimal.CurrentAction)}";
        }

        // Target
        if (targetText != null)
        {
            if (string.IsNullOrEmpty(
                    selectedAnimal.CurrentTargetName))
            {
                targetText.text = "";
            }
            else
            {
                targetText.text =
                    $"Target: {selectedAnimal.CurrentTargetName}";
            }
        }

        // [AI-GENERATED BEGIN] Cursor (Grok 4.7) - reviewed by Yande
        if (thoughtText != null)
        {
            string line = AnimalThought.Line(selectedAnimal);
            if (!ReferenceEquals(line, lastThoughtLine))
            {
                lastThoughtLine = line;
                thoughtText.text = line;
            }
        }
        // [AI-GENERATED END]
    }


    private string GetActionText(
        AnimalBrain.AnimalAction action)
    {
        switch (action)
        {
            case AnimalBrain.AnimalAction.EatingGrass:
                return "Eating Grass";

            case AnimalBrain.AnimalAction.DrinkingWater:
                return "Drinking Water";

            case AnimalBrain.AnimalAction.Hunting:
                return "Hunting";

            case AnimalBrain.AnimalAction.Fleeing:
                return "Fleeing";

            case AnimalBrain.AnimalAction.Reproducing:
                return "Reproducing";

            case AnimalBrain.AnimalAction.Roaming:
            default:
                return selectedAnimal != null && selectedAnimal.IsLookingAround ? "Looking around" : "Roaming";
        }
    }
}
