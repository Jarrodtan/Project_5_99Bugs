using TMPro;
using UnityEngine;

public class AnimalInfoUI : MonoBehaviour
{
    public static AnimalInfoUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Text")]
    public TMP_Text nameText;
    public TMP_Text hungerText;
    public TMP_Text thirstText;
    public TMP_Text actionText;
    public TMP_Text targetText;

    private AnimalBrain selectedAnimal;

    private void Awake()
    {
        Instance = this;

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

        var funFact = panel.transform.Find("DidYouKnow") as RectTransform;
        TMP_Text fact = funFact != null ? funFact.GetComponent<TMP_Text>() : null;
        var rows = new (TMP_Text text, float height, float size)[]
        {
            (nameText, 90f, 52f), (hungerText, 70f, 38f), (thirstText, 70f, 38f),
            (actionText, 70f, 38f), (targetText, 70f, 38f), (fact, 330f, 32f),
        };

        float y = -40f;
        foreach (var (text, height, size) in rows)
        {
            if (text == null) continue;
            var t = text.rectTransform;
            t.anchorMin = new Vector2(0f, 1f); t.anchorMax = new Vector2(1f, 1f);
            t.pivot = new Vector2(0.5f, 1f);
            t.localScale = Vector3.one;
            t.offsetMin = new Vector2(40f, 0f); t.offsetMax = new Vector2(-110f, 0f);   // room for the X on the right
            t.anchoredPosition = new Vector2(t.anchoredPosition.x, y);
            t.sizeDelta = new Vector2(t.sizeDelta.x, height);
            // Shrink to fit its row instead of spilling out of the card (long names, long fun facts).
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
        label.text = "✕";
        label.fontSize = 56f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }
    // [AI-GENERATED END]

    public void ShowAnimal(AnimalBrain animal)
    {
        if (animal == null)
            return;

        selectedAnimal = animal;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        UpdateUI();
    }

    public void HidePanel()
    {
        selectedAnimal = null;

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
                nameText.text =
                    selectedAnimal.species_data.speciesName;
            }
            else
            {
                nameText.text =
                    selectedAnimal.gameObject.name;
            }
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
                $"Currently: {GetActionText(selectedAnimal.CurrentAction)}";
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
                return "Roaming";
        }
    }
}
