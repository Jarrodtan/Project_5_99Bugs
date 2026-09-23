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
            populations[s] = 1f; // starting population
        }
    }

    public void Tick()
    {
        foreach (var r in resources)
        {
            r.currentAmount = Mathf.Min(r.maxAmount, r.currentAmount + r.regenRate);
        }

        // Pass 1: predators eat prey, prey populations shrink.
        // Collected here first so every species' consumption this tick is based on
        // the population counts at the START of the tick, not partially-eaten numbers.
        Dictionary<SpeciesDefinition, float> foodGotten = new();
        Dictionary<SpeciesDefinition, float> preyLosses = new();

        foreach (var s in species)
        {
            if (s.eats == null || s.eats.Count == 0) continue;

            float predatorPop = populations[s];
            float totalEaten = 0f;

            foreach (var prey in s.eats)
            {
                if (!populations.ContainsKey(prey)) continue;

                float preyPop = populations[prey];
                // Predation scales with BOTH populations: more predators AND more
                // prey both increase how much gets eaten this tick. Not capped by
                // hunger - a big prey population gets hunted hard regardless of
                // whether the predators "needed" that much.
                float eaten = Mathf.Min(s.predationRate * predatorPop * preyPop, preyPop);

                if (eaten > 0f)
                {
                    preyLosses[prey] = preyLosses.GetValueOrDefault(prey, 0f) + eaten;
                    totalEaten += eaten;
                }
            }

            foodGotten[s] = totalEaten;
        }

        // Pass 2: apply prey losses.
        foreach (var kvp in preyLosses)
        {
            populations[kvp.Key] = Mathf.Max(0f, populations[kvp.Key] - kvp.Value);
        }

        // Pass 3: growth. Predators grow/shrink based on how well-fed they were;
        // everything else keeps the simple reproductionRate growth for now.
        foreach (var s in species)
        {
            float pop = populations[s];

            if (s.eats != null && s.eats.Count > 0)
            {
                float foodNeeded = pop * s.foodNeededPerCapita;
                float ratio = foodNeeded > 0f ? foodGotten[s] / foodNeeded : 1f;

                if (ratio >= 1f)
                {
                    populations[s] = pop + (pop * s.reproductionRate);
                }
                else
                {
                    // Underfed: shrink toward starvation proportional to how short they were.
                    float shortfall = 1f - ratio;
                    populations[s] = Mathf.Max(0f, pop - (pop * shortfall * s.starvationThreshold));
                }
            }
            else
            {
                populations[s] = pop + (pop * s.reproductionRate);
            }
        }
    }
}