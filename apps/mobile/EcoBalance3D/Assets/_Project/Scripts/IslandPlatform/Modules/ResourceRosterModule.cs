using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional island module: roster of resource definition assets for one island.
/// Regen rates and amounts live on those resource assets — not here.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Island Resource Roster", fileName = "ResourceRosterModule")]
public class ResourceRosterModule : IslandModule
{
    [Header("Resources")]
    [Tooltip("Resource definition assets available on this island. Leave empty until authored.")]
    public List<ResourceDefinition> resources = new();
}
