using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional island module: roster of species definition assets for one island.
/// Populations and rates live on those species assets — not here.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Island Species Roster", fileName = "SpeciesRosterModule")]
public class SpeciesRosterModule : IslandModule
{
    [Header("Species")]
    [Tooltip("Species definition assets available on this island. Leave empty until authored.")]
    public List<SpeciesDefinition> species = new();
}
