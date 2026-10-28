using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class CreatureSpawner : MonoBehaviour {
    public SpeciesDefinition species;
    public EcosystemSimulator simulator;
    public int maxVisible = 10;
    public float spawnRadius = 8f;

    public HimalayaWorldBuilder world;
    [Range(0.05f, 1f)] public float maxElevationFraction = 0.2f;  // lowest 20% of the height range
    public float maxSpawnSlope = 25f;

    void OnEnable() { if (world != null) world.Built += RefreshVisuals; }
    void OnDisable() { if (world != null) world.Built -= RefreshVisuals; }

    bool TryGetSpawnPoint(out Vector3 point)
    {
        point = default;

        // No world builder assigned: old flat-island behaviour.
        if (world == null)
        {
            var pos = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0.5f, Random.Range(-spawnRadius, spawnRadius));
            if (NavMesh.SamplePosition(pos, out var h, 4f, NavMesh.AllAreas)) { point = h.position; return true; }
            return false;
        }

        if (!world.IsReady) return false;   // NavMesh doesn't exist yet

        var tb = world.TerrainBuilder;
        float maxElev = tb.MinElevation + (tb.MaxElevation - tb.MinElevation) * maxElevationFraction;
        for (int i = 0; i < 200; i++)
        {
            float u = Random.value, v = Random.value;
            if (tb.ElevationMeters(u, v) > maxElev) continue;
            if (tb.SlopeDegrees(u, v) > maxSpawnSlope) continue;
            if (NavMesh.SamplePosition(tb.WorldPosition(u, v), out var hit, 6f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }
        return false;
    }


    // [AI-GENERATED] Claude Code (Claude Opus 5.5): one visual per N animals, so the number of
    // visible agents follows real populations (e.g. 50 sheep) instead of sitting at maxVisible.
    [Min(1f)] public float populationPerVisual = 5f;

    private List<GameObject> spawned = new List<GameObject>();

    public void RefreshVisuals() {
        if (!simulator.populations.ContainsKey(species)) return;
        if (species.visualPrefab == null) return;

        float pop = simulator.populations[species];
        int targetCount = Mathf.Min(maxVisible, Mathf.RoundToInt(pop / populationPerVisual)); // [AI-GENERATED] Claude Code (Claude Opus 5.5): round like PopulationDisplay so models match the label

        while (spawned.Count < targetCount)
        {
            if (!TryGetSpawnPoint(out var spawnPos)) break;

            GameObject go = Instantiate(species.visualPrefab, spawnPos, Quaternion.identity, transform);

            var entity = go.GetComponent<EcoEntity>();
            if (entity == null) entity = go.AddComponent<EcoEntity>();
            entity.SetDefinition(species);

            spawned.Add(go);
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
