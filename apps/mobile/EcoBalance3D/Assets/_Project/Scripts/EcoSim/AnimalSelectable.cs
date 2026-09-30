using UnityEngine;

public class AnimalSelectable : MonoBehaviour
{
    private AnimalBrain animalBrain;

    private void Awake()
    {
        // Look for AnimalBrain on this object or anywhere underneath it.
        animalBrain = GetComponentInChildren<AnimalBrain>(true);

        if (animalBrain == null)
        {
            Debug.LogWarning(
                $"AnimalSelectable: No AnimalBrain found on {gameObject.name} or its children."
            );
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"Clicked animal: {gameObject.name}");

        if (animalBrain == null)
        {
            Debug.LogWarning("AnimalSelectable: AnimalBrain is missing.");
            return;
        }

        if (AnimalInfoUI.Instance == null)
        {
            Debug.LogWarning("AnimalSelectable: AnimalInfoUI.Instance is missing.");
            return;
        }

        AnimalInfoUI.Instance.ShowAnimal(animalBrain);
    }
}
