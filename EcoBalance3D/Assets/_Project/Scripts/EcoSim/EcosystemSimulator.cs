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
        if (species == null)
            return;

        foreach (var s in species)
        {
            if (s != null)
                populations[s] = 1f;
        }
    }

    public void Tick()
    {
        if (resources != null)
        {
            foreach (var r in resources)
            {
                if (r == null)
                    continue;
                r.currentAmount = Mathf.Min(r.maxAmount, r.currentAmount + r.regenRate);
            }
        }

        if (species == null)
            return;

        foreach (var s in species)
        {
            if (s == null || !populations.ContainsKey(s))
                continue;
            float pop = populations[s];
            populations[s] = pop + (pop * s.reproductionRate);
        }
    }
}
