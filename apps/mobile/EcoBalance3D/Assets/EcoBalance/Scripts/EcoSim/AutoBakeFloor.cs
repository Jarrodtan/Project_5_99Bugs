using UnityEngine;
using Unity.AI.Navigation;

public class AutoBakeFloor : MonoBehaviour
{
    [Tooltip("Builds the Himalaya terrain, trees, water and grass, then bakes the NavMesh and starts the clock. Leave empty to use the old behaviour (bake only).")]
    [SerializeField] HimalayaWorldBuilder worldBuilder;

    bool hasLocation;
    double centerLat, centerLon;

    /// <summary>Optional: build the world around this point instead of the box in HimalayaConfig. Call before Bake.</summary>
    public void SetLocation(double lat, double lon)
    {
        hasLocation = true;
        centerLat = lat;
        centerLon = lon;
    }

    // Called by TerrainConfig once its API request has finished.
    public void Bake(int tree, int river, int hill, int mud)
    {
        Debug.Log($"[LEVEL GENERATION] Ready to spawn {tree} trees, {river} rivers, {hill} hills, {mud} mud.");

        if (worldBuilder != null)
        {
            // The builder downloads data, so it bakes the NavMesh and enables the clock itself when it finishes.
            if (hasLocation) worldBuilder.BuildFromCounts(tree, river, hill, mud, centerLat, centerLon);
            else worldBuilder.BuildFromCounts(tree, river, hill, mud);
            hasLocation = false;
            return;
        }

        var surface = GetComponent<NavMeshSurface>();
        surface.layerMask &= ~(1 << 2);   // skip Ignore Raycast (decor trees): their meshes are not readable on device
        surface.BuildNavMesh();
        Debug.Log("Succesfully created.");

        SimulationClock clock = FindAnyObjectByType<SimulationClock>();
        if (clock != null) clock.enabled = true;
    }
}
