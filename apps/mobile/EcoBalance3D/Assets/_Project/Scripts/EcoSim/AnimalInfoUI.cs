using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimalInfoUI : MonoBehaviour
{
    public static AnimalInfoUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Text")]
    public TMP_Text nameText;
    public TMP_Text hungerText;
    public TMP_Text actionText;
    public TMP_Text targetText;
    public TMP_Text factText;

    [Header("Facts")]
    public SpeciesFact[] facts;

    [Serializable]
    public class SpeciesFact
    {
        public string speciesName;
        [TextArea]
        public string fact;
    }

    private AnimalBrain selectedAnimal;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (selectedAnimal == null)
            return;

        if (PressedOutside())
        {
            HidePanel();
            return;
        }

        UpdateUI();
    }

    // A press on the card keeps it. A press on another animal switches it.
    // Any other press, including empty ground, closes it.
    bool PressedOutside()
    {
        if (!TryGetPointerPress(out var screen))
            return false;

        if (panel != null &&
            panel.transform is RectTransform rect &&
            RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null))
            return false;

        var camera = Camera.main;
        if (camera == null)
            return true;

        var ray = camera.ScreenPointToRay(screen);
        return !Physics.Raycast(ray, out var hit, 5000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<AnimalSelectable>() == null;
    }

    static bool TryGetPointerPress(out Vector2 screen)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screen = Mouse.current.position.ReadValue();
            return true;
        }

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screen = touch.primaryTouch.position.ReadValue();
            return true;
        }

        screen = default;
        return false;
    }

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

        if (factText != null)
        {
            var fact = FindFact(SelectedSpeciesName());
            factText.text = string.IsNullOrEmpty(fact)
                ? ""
                : $"Did you know?\n{fact}";
        }
    }

    string SelectedSpeciesName()
    {
        if (selectedAnimal.species_data != null &&
            !string.IsNullOrEmpty(selectedAnimal.species_data.speciesName))
            return selectedAnimal.species_data.speciesName;

        return selectedAnimal.gameObject.name;
    }

    string FindFact(string speciesName)
    {
        if (facts == null || string.IsNullOrEmpty(speciesName))
            return "";

        var compact = speciesName.Replace(" ", "");
        foreach (var entry in facts)
        {
            if (entry == null || string.IsNullOrEmpty(entry.speciesName))
                continue;

            if (string.Equals(
                    entry.speciesName.Replace(" ", ""),
                    compact,
                    StringComparison.OrdinalIgnoreCase))
                return entry.fact;
        }

        return "";
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

            case AnimalBrain.AnimalAction.Roaming:
            default:
                return "Roaming";
        }
    }
}
