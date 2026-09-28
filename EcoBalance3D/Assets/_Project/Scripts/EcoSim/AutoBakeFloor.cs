using UnityEngine;
using Unity.AI.Navigation;

public class AutoBakeFloor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Delay baking process by 0.5s so that it gives time for the floor to be generated
        // before the floor turns into a walkable floor
        Invoke("Bake", 0.5f);
    }

    // Generate the walkable pathfinding grid
    void Bake()
    {
        GetComponent<NavMeshSurface>().BuildNavMesh();
        Debug.Log("Succesfully created.");
    }
}
