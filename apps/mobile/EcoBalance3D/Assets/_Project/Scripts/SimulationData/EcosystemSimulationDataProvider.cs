using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live-sim adapter: implements <see cref="ISimulationDataProvider"/> against <see cref="EcosystemSimulator"/>
/// for the Population and Resources tabs. Ids are the species / resource asset names (e.g. "BlueSheep", "Grass"),
/// the same keys the Configuration screen uses for starting counts.
/// </summary>
public class EcosystemSimulationDataProvider : MonoBehaviour, ISimulationDataProvider
{
    [Tooltip("Live ecosystem simulator. Species and resources are read from its lists.")]
    [SerializeField]
    EcosystemSimulator simulator;

    readonly List<string> speciesIds = new();
    readonly List<string> resourceIds = new();
    readonly Dictionary<string, SpeciesDefinition> speciesById = new();
    readonly Dictionary<string, ResourceDefinition> resourcesById = new();
    int lookupFrame = -1;

    /// <summary>
    /// Asset names of the simulator's species, in its list order.
    /// </summary>
    /// <returns>Read-only list of species ids; never null.</returns>
    public IReadOnlyList<string> GetSpeciesIds()
    {
        RebuildLookups();
        return speciesIds;
    }

    /// <summary>
    /// Asset names of the simulator's resources, in its list order.
    /// </summary>
    /// <returns>Read-only list of resource ids; never null.</returns>
    public IReadOnlyList<string> GetResourceIds()
    {
        RebuildLookups();
        return resourceIds;
    }

    /// <summary>
    /// Current population of a species from the live sim.
    /// </summary>
    /// <param name="speciesId">Species asset name.</param>
    /// <param name="count">Population when found; otherwise 0.</param>
    /// <returns>False when the id is unknown or the simulator has not initialised populations yet.</returns>
    public bool TryGetPopulationCount(string speciesId, out float count)
    {
        count = 0f;
        RebuildLookups();
        return speciesId != null
            && speciesById.TryGetValue(speciesId, out var species)
            && simulator.populations.TryGetValue(species, out count);
    }

    /// <summary>
    /// Current amount of a resource from the live sim.
    /// </summary>
    /// <param name="resourceId">Resource asset name.</param>
    /// <param name="level">Current amount when found; otherwise 0.</param>
    /// <returns>False when the id is unknown.</returns>
    public bool TryGetResourceLevel(string resourceId, out float level)
    {
        level = 0f;
        RebuildLookups();
        if (resourceId == null || !resourcesById.TryGetValue(resourceId, out var resource))
            return false;

        level = resource.currentAmount;
        return true;
    }

    /// <summary>
    /// speciesName / resourceName from the asset, falling back to the id.
    /// </summary>
    /// <param name="id">Species or resource id.</param>
    /// <returns>Display name, or <paramref name="id"/> when no name is configured.</returns>
    public string GetDisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        RebuildLookups();
        if (speciesById.TryGetValue(id, out var species) && !string.IsNullOrEmpty(species.speciesName))
            return species.speciesName;
        if (resourcesById.TryGetValue(id, out var resource) && !string.IsNullOrEmpty(resource.resourceName))
            return resource.resourceName;
        return id;
    }

    void Awake()
    {
        if (simulator == null)
            Debug.LogError($"{nameof(EcosystemSimulationDataProvider)} requires an {nameof(EcosystemSimulator)}.", this);
    }

    // At most once per frame, so a caller looping over GetSpeciesIds() keeps a stable list,
    // while still picking up changes to the simulator's lists.
    void RebuildLookups()
    {
        if (lookupFrame == Time.frameCount)
            return;
        lookupFrame = Time.frameCount;

        speciesIds.Clear();
        resourceIds.Clear();
        speciesById.Clear();
        resourcesById.Clear();
        if (simulator == null)
            return;

        if (simulator.species != null)
        {
            foreach (var s in simulator.species)
            {
                if (s != null && speciesById.TryAdd(s.name, s))
                    speciesIds.Add(s.name);
            }
        }

        if (simulator.resources != null)
        {
            foreach (var r in simulator.resources)
            {
                if (r != null && resourcesById.TryAdd(r.name, r))
                    resourceIds.Add(r.name);
            }
        }
    }
}
