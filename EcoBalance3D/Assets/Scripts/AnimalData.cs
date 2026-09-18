using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimal", menuName = "Ecobalance/AnimalData")]

public class AnimalData : ScriptableObject
{
    [Header("Basic Info")]
    public string animal_name;

    [Header("Max Stats")]
    public float max_health = 100.0f;
    public float max_hunger = 100.0f;
    public float max_thirst = 100.0f;

    [Header("Decay Rates")]
    public float hunger_decay = 2.0f;
    public float thirst_decay = 3.0f;

    [Header("Relationship System")]
    public string[] food;
    public string[] water;
    public string[] predator;

}
