using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One island identity and placement asset. Visuals, pivot, containment, and content modules
/// are authored on this asset — never hardcode island-specific values in scripts.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Island Definition", fileName = "IslandDefinition")]
public class IslandDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key used by IslandPlatformConfig.TryGetIsland. Do not rename after assets reference it.")]
    public string id = string.Empty;

    [Tooltip("Human-readable label for UI and logs. Leave empty until authored on the asset.")]
    public string displayName = string.Empty;

    [Header("Visual")]
    [Tooltip("Prefab used as the island visual model. Leave empty until assigned on the asset.")]
    public GameObject islandPrefab;

    [Header("Placement")]
    [Tooltip("Local offset applied to correct the prefab pivot when placing or centering the island.")]
    public Vector3 centerOffset = Vector3.zero;

    [Tooltip("Containment circle relative to the island root. Authored per island asset.")]
    public IslandBoundary boundary;

    [Header("Modules")]
    [Tooltip("Optional content modules for this island. Assign SpeciesRosterModule, ResourceRosterModule, or future module assets.")]
    public List<IslandModule> modules = new();

    /// <summary>
    /// Returns the first assigned module of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Concrete <see cref="IslandModule"/> subclass to find.</typeparam>
    /// <param name="module">Matched module, or null.</param>
    /// <returns>True when a module of that type was found.</returns>
    public bool TryGetModule<T>(out T module) where T : IslandModule
    {
        module = null;
        if (modules == null)
            return false;

        for (var i = 0; i < modules.Count; i++)
        {
            if (modules[i] is T typed)
            {
                module = typed;
                return true;
            }
        }

        return false;
    }
}
