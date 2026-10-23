using UnityEngine;
using Unity.AI.Navigation;

public class AutoBakeFloor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Delay baking process by 0.5s so that it gives time for the floor to be generated
        // before the floor turns into a walkable floor
    }

    // Generate the walkable pathfinding grid
    public void Bake(int tree, int river, int hill, int mud)
    {
        // TODO FOR SECOND PERSON: Add prefab instantiation loops here.
        // Example: for(int i = 0; i < treeCount; i++) { Instantiate(treePrefab); }
        Debug.Log($"[LEVEL GENERATION] Ready to spawn {tree} trees, {river} rivers, {hill} hills, {mud} mud.");

        GetComponent<NavMeshSurface>().BuildNavMesh();
        Debug.Log("Succesfully created.");

        SimulationClock clock = FindAnyObjectByType<SimulationClock>();
        if (clock != null) clock.enabled = true;
    }
}
