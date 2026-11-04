using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Writes each species' population from the animals currently active in the scene.
/// The species asset's startingPopulation is not used.
/// </summary>
public class SpawnedPopulationSync : MonoBehaviour
{
    [SerializeField]
    EcosystemSimulator simulator;

    void OnEnable()
    {
        if (simulator != null)
            simulator.EcosystemReset += Apply;
    }

    void Start() => Apply();

    void Update() => Apply();

    void OnDisable()
    {
        if (simulator != null)
            simulator.EcosystemReset -= Apply;
    }

    void Apply()
    {
        if (simulator == null || simulator.species == null)
            return;

        for (var i = 0; i < simulator.species.Count; i++)
        {
            var species = simulator.species[i];
            if (species != null)
                simulator.populations[species] = 0f;
        }

        var scene = gameObject.scene;
        if (!scene.IsValid())
            return;

        var roots = scene.GetRootGameObjects();
        for (var r = 0; r < roots.Length; r++)
        {
            var brains = roots[r].GetComponentsInChildren<AnimalBrain>(true);
            for (var i = 0; i < brains.Length; i++)
            {
                var brain = brains[i];
                if (brain == null || !brain.gameObject.activeInHierarchy)
                    continue;

                var species = MatchSpecies(brain);
                if (species == null)
                    continue;

                simulator.populations[species] = simulator.populations.GetValueOrDefault(species) + 1f;
            }
        }
    }

    SpeciesDefinition MatchSpecies(AnimalBrain brain)
    {
        var source = brain.species_data;
        var name = source != null
            ? Compact(string.IsNullOrEmpty(source.speciesName) ? source.name : source.speciesName)
            : Compact(brain.gameObject.name);

        SpeciesDefinition byName = null;
        for (var i = 0; i < simulator.species.Count; i++)
        {
            var species = simulator.species[i];
            if (species == null)
                continue;

            if (source != null && species == source)
                return species;

            if (byName == null && name.Length > 0 && Compact(string.IsNullOrEmpty(species.speciesName) ? species.name : species.speciesName) == name)
                byName = species;
        }

        return byName;
    }

    static string Compact(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace(" ", string.Empty);
    }
}
