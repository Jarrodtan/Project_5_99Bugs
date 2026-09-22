using UnityEngine;
using System.Collections.Generic;

public class EcosystemSimulator : MonoBehaviour
{
    public List<SpeciesDefinition> species;
    public List<ResourceDefinition> resources;
    [System.NonSerialized]
    public Dictionary<SpeciesDefinition, float> populations = new();

    void Start()
    {
        foreach (var s in species)
        {
            populations[s] = 1.0f; // starting population
        }
    }

    public void Tick()
    {
        foreach (var r in resources)
        {
            r.currentAmount = Mathf.Min(r.maxAmount, r.currentAmount + r.regenRate);
        }

        foreach (var s in species)
        {
            float pop = populations[s];
            populations[s] = pop + (pop * s.reproductionRate);
        }
    }
}
