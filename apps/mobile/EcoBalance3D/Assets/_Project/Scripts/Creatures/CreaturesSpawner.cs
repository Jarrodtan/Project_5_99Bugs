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
        int targetCount = Mathf.Min(maxVisible, Mathf.RoundToInt(pop / populationPerVisual)); // [AI-GENERATED] Claude Code (Claude Opus 5.5): round like PopulationDisplay so models match the label

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
            int index = IndexToRemove(); // [AI-GENERATED] Claude Code (Claude Opus 5.5): caught animals go first
            Destroy(spawned[index]);
            spawned.RemoveAt(index);
        }
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Which model to remove when the population drops: the most recently caught animal (so the one a
    // predator just ate disappears), otherwise the last spawned one (e.g. starvation).
    int IndexToRemove()
    {
        int best = spawned.Count - 1;
        float bestTime = -1f;
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null) return i;
            if (spawned[i].TryGetComponent(out AnimalBrain brain) && brain.CaughtTime > bestTime)
            {
                bestTime = brain.CaughtTime;
                best = i;
            }
        }
        return best;
    }
    // [AI-GENERATED END]

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
