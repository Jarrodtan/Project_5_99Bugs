using UnityEngine;
using System.Collections.Generic;

public class CreatureSpawner : MonoBehaviour {
    public SpeciesDefinition species;
    public EcosystemSimulator simulator;
    public int maxVisible = 10;
    public float spawnRadius = 8f;

    private List<GameObject> spawned = new List<GameObject>();

    public void RefreshVisuals() {
        if (!simulator.populations.ContainsKey(species)) return;
        if (species.visualPrefab == null) return;

        float pop = simulator.populations[species];
        int targetCount = Mathf.Min(maxVisible, Mathf.RoundToInt(pop));

        while (spawned.Count < targetCount) {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0.5f,
                Random.Range(-spawnRadius, spawnRadius)
            );
            GameObject go = Instantiate(species.visualPrefab, pos, Quaternion.identity, transform);
            spawned.Add(go);
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
