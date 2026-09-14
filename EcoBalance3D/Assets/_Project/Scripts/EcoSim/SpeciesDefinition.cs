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
    public GameObject visualPrefab;
}
