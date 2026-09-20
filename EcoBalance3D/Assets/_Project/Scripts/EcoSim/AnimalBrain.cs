//Animal Core AI logic

using UnityEngine;
using UnityEngine.AI;

public class AnimalBrain : MonoBehaviour
{
    public SpeciesDefinition species_data;
    public float curr_hunger = 100f;
    private NavMeshAgent agent;
    private float wander_timer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Find collider to determine where is the flor
        Collider col = GetComponentInChildren<Collider>();
        if (col != null )
        {
            float dist = col.bounds.min.y - transform.position.y;
            agent.baseOffset = -dist;
        } 

        if (species_data != null)
        {
            gameObject.name = species_data.speciesName;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Timer that times when and how long object will start walking
        wander_timer -= Time.deltaTime;
        if (wander_timer <= 0f)
        {
            Roam();
            wander_timer = Random.Range(3f, 6f);
        }
    }

    // Allow the object to 'walk' around
    void Roam()
    {
        Vector3 rand_dir = Random.insideUnitSphere * 10f;
        rand_dir += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(rand_dir, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

}
