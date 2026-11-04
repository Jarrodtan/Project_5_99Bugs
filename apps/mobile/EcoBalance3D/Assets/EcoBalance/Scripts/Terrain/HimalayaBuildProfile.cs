using UnityEngine;

/// <summary>
/// Swappable Himalaya build variant (mountain vs flat-grass cut). Assigned on the island prefab —
/// builders branch only on these flags, never on island ids.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Himalaya Build Profile", fileName = "HimalayaBuildProfile")]
public class HimalayaBuildProfile : ScriptableObject
{
    [Tooltip("After DEM sampling, write a constant heightmap (flat cut of the same land).")]
    public bool flattenHeights;

    [Tooltip("Paint grass-dominant splat instead of elevation/slope snow-rock-grass.")]
    public bool forceGrassSplat = true;

    [Tooltip("Keep the mountain shape, but never paint the snow layer. Steep ground stays rock.")]
    public bool suppressSnow;

    [Tooltip("Run the tree placer stage.")]
    public bool placeTrees = true;

    [Tooltip("Fetch water features and place water resource patches.")]
    public bool placeWater = true;

    [Tooltip("Place grass resource patches.")]
    public bool placeGrassPatches = true;
}
