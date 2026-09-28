// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the number of visible agents in line with one species' population.
/// Births appear next to an existing animal; when the population drops, animals that
/// were just caught by a predator are removed first, so the viewer sees who got eaten.
/// </summary>
public class EcoPopulationView : MonoBehaviour
{
    public EcosystemSimulator simulator;
    public SpeciesDefinition species;
    public GameObject agentPrefab;
    public bool isPredator;
    [Min(1f)] public float populationPerVisual = 4f;
    [Min(1)] public int maxVisible = 20;
    [Min(1f)] public float spawnRadius = 10f;

    readonly List<EcoAgent> agents = new List<EcoAgent>();

    public int VisibleCount => agents.Count;

    /// <summary>Visible agents for a population: at least 1 while any are alive, capped at max.</summary>
    public static int TargetCount(float population, float populationPerVisual, int maxVisible)
    {
        if (population <= 0f) return 0;
        return Mathf.Clamp(Mathf.CeilToInt(population / Mathf.Max(1f, populationPerVisual)), 0, maxVisible);
    }

    public void Refresh()
    {
        if (simulator == null || species == null || agentPrefab == null) return;

        simulator.populations.TryGetValue(species, out float pop);
        int target = TargetCount(pop, populationPerVisual, maxVisible);
        agents.RemoveAll(a => a == null);

        while (agents.Count < target) Spawn();
        while (agents.Count > target) Remove(PickToRemove());
    }

    public void Clear()
    {
        foreach (var a in agents)
            if (a != null) Destroy(a.gameObject);
        agents.Clear();
    }

    void Spawn()
    {
        Vector3 pos;
        if (agents.Count > 0)
        {
            // Birth: appear next to a parent.
            var parent = agents[Random.Range(0, agents.Count)].transform.position;
            Vector2 off = Random.insideUnitCircle * 1.2f;
            pos = new Vector3(parent.x + off.x, 0f, parent.z + off.y);
        }
        else
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            pos = new Vector3(r.x, 0f, r.y);
        }
        pos.y = agentPrefab.transform.localScale.y;

        var go = Instantiate(agentPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        go.name = $"{species.speciesName}_{agents.Count:00}";
        var agent = go.GetComponent<EcoAgent>();
        if (agent == null) agent = go.AddComponent<EcoAgent>();
        agent.species = species;
        agent.isPredator = isPredator;
        agent.arenaRadius = spawnRadius;
        agents.Add(agent);
    }

    EcoAgent PickToRemove()
    {
        EcoAgent caught = null;
        foreach (var a in agents)
            if (Time.time - a.CaughtTime < 5f && (caught == null || a.CaughtTime > caught.CaughtTime))
                caught = a;
        return caught != null ? caught : agents[Random.Range(0, agents.Count)];
    }

    void Remove(EcoAgent agent)
    {
        agents.Remove(agent);
        Destroy(agent.gameObject);
    }
}
// [AI-GENERATED END]
