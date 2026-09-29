using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class CreatureSpawner : MonoBehaviour {
    public SpeciesDefinition species;
    public EcosystemSimulator simulator;
    public int maxVisible = 10;
    public float spawnRadius = 8f;


    // [AI-GENERATED] Claude Code (Claude Opus 5.5): one visual per N animals, so the number of
    // visible agents follows real populations (e.g. 50 sheep) instead of sitting at maxVisible.
    [Min(1f)] public float populationPerVisual = 5f;

    private List<GameObject> spawned = new List<GameObject>();

    public void RefreshVisuals() {
        if (!simulator.populations.ContainsKey(species)) return;
        if (species.visualPrefab == null) return;

        float pop = simulator.populations[species];
        int targetCount = Mathf.Min(maxVisible, Mathf.CeilToInt(pop / populationPerVisual));

        while (spawned.Count < targetCount) {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0.5f,
                Random.Range(-spawnRadius, spawnRadius)
            );
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 4f, NavMesh.AllAreas))
            {
                GameObject go = Instantiate(species.visualPrefab, hit.position, Quaternion.identity, transform);

                var entity = go.GetComponent<EcoEntity>();
                if (entity == null) entity = go.AddComponent<EcoEntity>();
                entity.SetDefinition(species);

                spawned.Add(go);
            }
            else
            {
                break;
            }
        }

        while (spawned.Count > targetCount) {
            int last = spawned.Count - 1;
            Destroy(spawned[last]);
            spawned.RemoveAt(last);
        }
    }

    public void ResetVisuals()
    {
        foreach (GameObject creature in spawned)
        {
            if (creature != null)
                Destroy(creature);
        }

        spawned.Clear();

        RefreshVisuals();
    }
}
