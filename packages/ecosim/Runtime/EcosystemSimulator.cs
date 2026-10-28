using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tick-based ecosystem core. Also implements <see cref="IEcosystemReset"/> for island swaps
/// (full population re-init — no entity remapping).
/// </summary>
public class EcosystemSimulator : MonoBehaviour, IEcosystemReset
{
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    /// <summary>How populations change.</summary>
    public enum SimulationMode
    {
        /// <summary>Tick() applies predation / grazing / growth formulas (deterministic, used by the tests).</summary>
        Formula,
        /// <summary>Populations change only from animal events (eat, catch, starve); Tick() only regrows resources.</summary>
        Events,
    }

    [Tooltip("Formula: populations follow the tick formulas. Events: +1/-1 from what the 3D animals do.")]
    public SimulationMode mode = SimulationMode.Formula;

    [Tooltip("Events mode: amount each resource regrows per tick.")]
    [Min(0f)] public float eventRegenPerTick = 3f;

    [Tooltip("Events mode: eating (grass, or prey for predators) adds +1 to the eater's population, as a simple " +
             "stand-in for reproduction. Off until the reproduction system exists, so eating only feeds the animal.")]
    public bool eatingAddsOffspring = false;
    // [AI-GENERATED END]

    public List<SpeciesDefinition> species;
    public List<ResourceDefinition> resources;
    [System.NonSerialized]
    public Dictionary<SpeciesDefinition, float> populations = new();

    /// <summary>
    /// Starting counts picked on the Configuration screen, keyed by species asset name.
    /// A species listed here starts (and resets) at this count instead of its startingPopulation.
    /// </summary>
    public static readonly Dictionary<string, float> StartingPopulationOverrides = new();

    // Domain reload is off in this project, so statics survive between Play sessions: start each one empty.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStartingPopulationOverrides() => StartingPopulationOverrides.Clear();

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
        {
            if (s == null) continue;
            populations[s] = StartingPopulationOverrides.TryGetValue(s.name, out float chosen)
                ? chosen
                : s.startingPopulation;
        }
        // Resource assets keep their values between Play sessions in the Editor,
        // so each run starts from the configured starting amount.
        foreach (var r in resources)
            r.currentAmount = Mathf.Min(r.startingAmount, r.maxAmount);
        TickCount = 0;
        EcosystemReset?.Invoke();
        // [AI-GENERATED END]
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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    /// <summary>Completed ticks since the last reset (one tick = one simulated day).</summary>
    public int TickCount { get; private set; }

    /// <summary>Raised after each completed <see cref="Tick"/>, once populations and resources are updated.</summary>
    public event System.Action Ticked;

    /// <summary>Raised after <see cref="ResetEcosystem"/> restores the starting state.</summary>
    public event System.Action EcosystemReset;

    public void Tick()
    {
        if (!ValidateConfiguration()) return;
        Step();
        TickCount++;
        Ticked?.Invoke();
    }

    void Step()
    {
        if (mode == SimulationMode.Events)
        {
            // Populations change only through RecordPredation / RecordEat / RecordStarvation.
            foreach (var r in resources)
                r.currentAmount = Mathf.Min(r.maxAmount, r.currentAmount + eventRegenPerTick);
            return;
        }
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
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Kept for Reginald's Reset button (F02). Uses the same full reset as island swaps,
    // so every species returns to its startingPopulation and resources to startingAmount.
    public void ResetSimulation() => ResetEcosystem();
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // ---- Events mode: called by the animals in the scene ----

    /// <summary>Raised after <see cref="RecordPredation"/> changes populations: (predator, prey).</summary>
    public event System.Action<SpeciesDefinition, SpeciesDefinition> PredationRecorded;

    /// <summary>Raised after <see cref="RecordEat"/> succeeds: (eater, resource).</summary>
    public event System.Action<SpeciesDefinition, ResourceDefinition> EatRecorded;

    /// <summary>Raised after <see cref="RecordStarvation"/> changes the population.</summary>
    public event System.Action<SpeciesDefinition> StarvationRecorded;

    /// <summary>A predator caught and ate one prey: prey -1 (predator +1 only if <see cref="eatingAddsOffspring"/>).</summary>
    public void RecordPredation(SpeciesDefinition predator, SpeciesDefinition prey)
    {
        if (predator == null || prey == null) return;
        if (!populations.ContainsKey(prey) || populations[prey] < 1f) return;
        AddPopulation(prey, -1f);
        if (eatingAddsOffspring) AddPopulation(predator, +1f);
        PredationRecorded?.Invoke(predator, prey);
    }

    /// <summary>
    /// An animal ate/drank <paramref name="amount"/> of a resource. If that much is available,
    /// the resource goes down (and the species +1 only if <see cref="eatingAddsOffspring"/>).
    /// Returns false (no change) if there wasn't enough.
    /// </summary>
    public bool RecordEat(SpeciesDefinition eater, ResourceDefinition resource, float amount)
    {
        if (eater == null || resource == null || !populations.ContainsKey(eater)) return false;
        if (resource.currentAmount < amount) return false;
        resource.currentAmount -= amount;
        if (eatingAddsOffspring) AddPopulation(eater, +1f);
        EatRecorded?.Invoke(eater, resource);
        return true;
    }

    /// <summary>One animal starved: species -1.</summary>
    public void RecordStarvation(SpeciesDefinition species)
    {
        if (species == null || !populations.ContainsKey(species)) return;
        AddPopulation(species, -1f);
        StarvationRecorded?.Invoke(species);
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    [Tooltip("Events mode: a species can't grow past this many animals through births (keeps the tablet fast).")]
    [Min(1f)] public float maxPopulationPerSpecies = 200f;

    /// <summary>Raised after <see cref="RecordBirth"/> adds animals: (species, how many were born).</summary>
    public event System.Action<SpeciesDefinition, int> BirthRecorded;

    /// <summary>
    /// Animals of <paramref name="species"/> reproduced: species +<paramref name="count"/>, capped at
    /// <see cref="maxPopulationPerSpecies"/>. The 3D model appears on the next RefreshVisuals.
    /// Returns how many were actually born (0 if the species is unknown, extinct or already at the cap).
    /// </summary>
    public int RecordBirth(SpeciesDefinition species, int count = 1)
    {
        if (species == null || count < 1 || !populations.TryGetValue(species, out var pop)) return 0;
        if (pop < 1f) return 0; // an extinct species can't reproduce
        int born = Mathf.Min(count, Mathf.FloorToInt(maxPopulationPerSpecies - pop));
        if (born <= 0) return 0;
        AddPopulation(species, born);
        BirthRecorded?.Invoke(species, born);
        return born;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong

    void AddPopulation(SpeciesDefinition s, float delta)
    {
        populations[s] = Mathf.Max(0f, populations.GetValueOrDefault(s, 0f) + delta);
    }
    // [AI-GENERATED END]
}
