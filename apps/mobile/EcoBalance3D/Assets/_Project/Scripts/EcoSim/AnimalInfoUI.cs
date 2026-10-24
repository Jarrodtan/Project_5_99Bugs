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
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (selectedAnimal == null)
            return;

        UpdateUI();
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
