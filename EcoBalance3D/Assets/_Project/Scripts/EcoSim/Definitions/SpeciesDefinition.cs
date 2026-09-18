using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "EcoSim/Species")]
public class SpeciesDefinition : ScriptableObject
{
    public string speciesName;
    public GameObject visualPrefab;

    [Header("Diet / Relationships")]
    public List<SpeciesDefinition> eats;                  // empty = herbivore
    public List<ResourceDefinition> consumesResources;     // e.g. Grass

    [Header("Needs & Goals — drives GOBT")]
    public List<NeedDefinition> needs;
    public List<GoalDefinition> availableGoals;
}
