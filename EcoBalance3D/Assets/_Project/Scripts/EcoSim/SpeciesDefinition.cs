using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "EcoSim/Species")]
public class SpeciesDefinition : ScriptableObject
{
    public string speciesName;
    public List<SpeciesDefinition> eats;
    public List<ResourceDefinition> needs;
    public float reproductionRate = 0.1f;
    public float starvationThreshold = 0.2f;

    [Tooltip("Fraction of a prey species' population this species can eat, per predator, per tick. Only relevant if 'Eats' is non-empty.")]
    public float predationRate = 0.3f;
 
    [Tooltip("How much food (prey eaten) one individual of this species needs per tick to grow normally. Only relevant if 'Eats' is non-empty.")]
    public float foodNeededPerCapita = 0.5f;
    public GameObject visualPrefab;
}
