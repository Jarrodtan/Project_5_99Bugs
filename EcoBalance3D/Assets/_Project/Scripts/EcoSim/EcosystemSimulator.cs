using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tick-based ecosystem core. Also implements <see cref="IEcosystemReset"/> for island swaps
/// (full population re-init — no entity remapping).
/// </summary>
public class EcosystemSimulator : MonoBehaviour, IEcosystemReset
{
    public List<SpeciesDefinition> species;
    public List<ResourceDefinition> resources;
    [System.NonSerialized]
    public Dictionary<SpeciesDefinition, float> populations = new();

    void Start()
    {
        ResetEcosystem();
    }

    /// <inheritdoc />
    public void ResetEcosystem()
    {
        populations.Clear();
        if (!ValidateConfiguration())
            return;

        foreach (var s in species)
            populations[s] = 1f;
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Removes null entries and fills missing population entries so one
    // misconfigured asset logs a warning instead of crashing the whole simulation.
    bool ValidateConfiguration()
    {
        species ??= new List<SpeciesDefinition>();
        resources ??= new List<ResourceDefinition>();

        if (species.RemoveAll(s => s == null) > 0)
            Debug.LogWarning($"[{name}] EcosystemSimulator: removed empty entries from the Species list.", this);
        if (resources.RemoveAll(r => r == null) > 0)
            Debug.LogWarning($"[{name}] EcosystemSimulator: removed empty entries from the Resources list.", this);

        foreach (var s in species)
        {
            if (!populations.ContainsKey(s))
                populations[s] = 0f;
            if (s.relationships != null && s.relationships.RemoveAll(r => r == null || r.Target == null) > 0)
                Debug.LogWarning($"EcosystemSimulator: '{s.name}' had empty entries in its Eats list.", s);
        }

        if (species.Count == 0)
        {
            Debug.LogWarning($"[{name}] EcosystemSimulator has no Species assigned - nothing to simulate.", this);
            return false;
        }
        return true;
    }
    // [AI-GENERATED END]

    public void Tick()
    {
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        if (!ValidateConfiguration()) return;
        // [AI-GENERATED END]

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
            List<SpeciesDefinition> prey_list = s.GetSpeciesTargets(RelationshipType.Eats);
            if (prey_list.Count == 0) continue;

            float predatorPop = populations[s];
            float totalEaten = 0f;

            foreach (var prey in prey_list)
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

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Pass 1b: grazing. Species that Eat a resource (Blue Sheep eats Grass) each want
        // foodNeededPerCapita units per individual. When a resource can't cover everyone's
        // demand, every grazer gets the same fraction of what it wanted.
        foreach (var r in resources)
        {
            float demand = 0f;
            foreach (var s in species)
                if (s.GetResourceTargets(RelationshipType.Eats).Contains(r))
                    demand += populations[s] * s.foodNeededPerCapita;
            if (demand <= 0f) continue;

            float share = Mathf.Min(1f, r.currentAmount / demand);
            foreach (var s in species)
            {
                if (!s.GetResourceTargets(RelationshipType.Eats).Contains(r)) continue;
                float grazed = populations[s] * s.foodNeededPerCapita * share;
                foodGotten[s] = foodGotten.GetValueOrDefault(s, 0f) + grazed;
            }
            r.currentAmount = Mathf.Max(0f, r.currentAmount - demand * share);
        }
        // [AI-GENERATED END]

        // Pass 2: apply prey losses.
        foreach (var kvp in preyLosses)
        {
            populations[kvp.Key] = Mathf.Max(0f, populations[kvp.Key] - kvp.Value);
        }

        // Pass 3: growth. Anything that eats (hunters and grazers) grows/shrinks based on
        // how well-fed it was; species that eat nothing keep simple reproductionRate growth.
        foreach (var s in species)
        {
            float pop = populations[s];
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): grazers use the same fed-ratio growth as hunters
            bool eatsSomething = s.GetSpeciesTargets(RelationshipType.Eats).Count > 0
                              || s.GetResourceTargets(RelationshipType.Eats).Count > 0;
            if (eatsSomething)
            {
                float foodNeeded = pop * s.foodNeededPerCapita;
                float ratio = foodNeeded > 0f ? foodGotten.GetValueOrDefault(s, 0f) / foodNeeded : 1f;

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
