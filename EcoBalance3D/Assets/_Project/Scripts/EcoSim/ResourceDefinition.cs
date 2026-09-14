using UnityEngine;

// This allows you to create Grass/Water assets from the Editor.
[CreateAssetMenu(menuName = "EcoSim/Resource")]

// Unity class of ScriptableObject
public class ResourceDefinition : ScriptableObject
{
    public string resourceName;
    public float regenRate = 1f;
    public float maxAmount = 100f;
    public float currentAmount = 100f;
}
