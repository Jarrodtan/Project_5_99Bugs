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

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Habitat: blue sheep graze in herds on grassy slopes; snow leopards keep to higher, rockier ground above them.
    [Tooltip("Lowest spawn height, as a fraction of the terrain's height range (0 = valley floor).")]
    [Range(0f, 1f)] public float minElevationFraction = 0f;
    [Tooltip("Optional: only spawn within Near Resource Distance of a patch of this (e.g. Grass), so herds start by their food.")]
    public ResourceDefinition nearResource;
    public float nearResourceDistance = 12f;

    [Tooltip("Separate groups (herds / territories). 0 = spread over the whole habitat.")]
    public int groups = 0;
    [Tooltip("How far members of a group spawn and roam from its centre.")]
    public float groupRadius = 15f;
    [Tooltip("Minimum distance between group centres, so groups live in different places.")]
    public float minGroupSeparation = 60f;

    readonly List<Vector3> groupCentres = new List<Vector3>();
    int nextGroup;

    // Picks group centres once (in the habitat, far apart), then a spot near the next group's centre.
    bool TryGetGroupPoint(out Vector3 point, out Vector3 centre)
    {
        point = centre = default;
        for (int tries = 0; groupCentres.Count < groups && tries < 120; tries++)
        {
            if (!TryGetSpawnPoint(out var c)) break;
            // With a river, alternate sides: group 1 on one bank, group 2 on the other.
            int wantSide = groupCentres.Count % 2 == 0 ? 1 : -1;
            if (HimalayaRiver.Exists && tries < 50 &&
                (HimalayaRiver.Side(c) != wantSide || HimalayaRiver.DistanceTo(c) < groupRadius)) continue;
            bool farEnough = true;
            foreach (var g in groupCentres) if ((g - c).sqrMagnitude < minGroupSeparation * minGroupSeparation) { farEnough = false; break; }
            if (farEnough || tries > 50) groupCentres.Add(c);   // give up on spacing rather than have fewer groups
        }
        if (groupCentres.Count == 0) return false;

        centre = groupCentres[nextGroup++ % groupCentres.Count];
        Vector3 fallback = centre;
        float bestGap = -1f;
        for (int i = 0; i < 30; i++)
        {
            Vector2 r = Random.insideUnitCircle * groupRadius;
            if (!NavMesh.SamplePosition(centre + new Vector3(r.x, 0f, r.y), out var hit, 6f, NavMesh.AllAreas)) continue;
            float gap = NearestSpawnedDistance(hit.position);
            if (gap >= minSpacing) { point = hit.position; return true; }
            if (gap > bestGap) { bestGap = gap; fallback = hit.position; }   // most room found so far
        }
        point = fallback;
        return true;
    }

    [Tooltip("Spawn at least this far from other animals of this species (sheep: a few metres; leopards live alone).")]
    public float minSpacing = 3f;

    float NearestSpawnedDistance(Vector3 p)
    {
        float best = float.PositiveInfinity;
        foreach (var go in spawned)
        {
            if (go == null) continue;
            Vector3 d = go.transform.position - p; d.y = 0f;
            best = Mathf.Min(best, d.magnitude);
        }
        return best;
    }
    // [AI-GENERATED END]

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
        float minElev = tb.MinElevation + (tb.MaxElevation - tb.MinElevation) * minElevationFraction;
        for (int i = 0; i < 400; i++)
        {
            float u = Random.value, v = Random.value;
            float elev = tb.ElevationMeters(u, v);
            if (elev > maxElev || elev < minElev) continue;
            if (tb.SlopeDegrees(u, v) > maxSpawnSlope) continue;
            if (nearResource != null &&
                ResourcePatch.FindNearest(tb.WorldPosition(u, v), new[] { nearResource }, nearResourceDistance) == null) continue;
            if (NavMesh.SamplePosition(tb.WorldPosition(u, v), out var hit, 6f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): habitat had no walkable spot (e.g. too steep up high):
        // take the nearest walkable ground to a random spot in the band rather than spawn nothing.
        for (int i = 0; i < 100; i++)
        {
            float u = Random.value, v = Random.value;
            float elev = tb.ElevationMeters(u, v);
            if (elev > maxElev || elev < minElev) continue;
            if (NavMesh.SamplePosition(tb.WorldPosition(u, v), out var near, 25f, NavMesh.AllAreas))
            {
                point = near.position;
                return true;
            }
        }
        return false;
    }


    // [AI-GENERATED] Claude Code (Claude Opus 5.5): one visual per N animals, so the number of
    // visible agents follows real populations (e.g. 50 sheep) instead of sitting at maxVisible.
    [Min(1f)] public float populationPerVisual = 5f;

    private List<GameObject> spawned = new List<GameObject>();
    private bool warnedNoSpawn;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Mothers that just gave birth, waiting for the next refresh to place their baby beside them.
    static readonly List<AnimalBrain> pendingMothers = new List<AnimalBrain>();

    /// <summary>Call right before RecordBirth, so the baby model is placed next to this mother.</summary>
    public static void QueueBirth(AnimalBrain mother) { if (mother != null) pendingMothers.Add(mother); }

    static AnimalBrain TakePendingMother(SpeciesDefinition sp)
    {
        for (int i = 0; i < pendingMothers.Count; i++)
        {
            var m = pendingMothers[i];
            if (m == null || m.IsCaught) { pendingMothers.RemoveAt(i--); continue; }
            if (m.species_data != sp) continue;
            pendingMothers.RemoveAt(i);
            return m;
        }
        return null;
    }
    // [AI-GENERATED END]

    public void RefreshVisuals() {
        if (!simulator.populations.ContainsKey(species)) return;
        if (species.visualPrefab == null) return;

        float pop = simulator.populations[species];
        int targetCount = Mathf.Min(maxVisible, Mathf.RoundToInt(pop / populationPerVisual)); // [AI-GENERATED] Claude Code (Claude Opus 5.5): round like PopulationDisplay so models match the label

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Bodies whose death was already counted and whose lie-down time is over go now,
        // so the live models always match the population.
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null) { spawned.RemoveAt(i); continue; }
            if (spawned[i].TryGetComponent(out AnimalBrain b) && b.DeathFinished)
            {
                Destroy(spawned[i]);
                spawned.RemoveAt(i);
            }
        }
        // [AI-GENERATED END]

        while (spawned.Count < targetCount)
        {
            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // A birth: the new model appears as a baby right beside its mother.
            var mother = TakePendingMother(species);
            if (mother != null && NavMesh.SamplePosition(mother.transform.position - mother.transform.forward * 0.8f, out var nearMum, 2f, NavMesh.AllAreas))
            {
                GameObject baby = Instantiate(species.visualPrefab, nearMum.position, mother.transform.rotation, transform);
                var babyEntity = baby.GetComponent<EcoEntity>();
                if (babyEntity == null) babyEntity = baby.AddComponent<EcoEntity>();
                babyEntity.SetDefinition(species);
                if (baby.TryGetComponent(out AnimalBrain babyBrain)) { babyBrain.MakeBaby(mother); babyBrain.SetHabitat(minElevationFraction, maxElevationFraction); babyBrain.CopyHome(mother); }
                spawned.Add(baby);
                DramaDirector.Report(DramaDirector.Moment.Birth, baby.transform,   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
                    $"A baby {AnimalBrain.SpeciesLabel(species)} is born! Watch it follow its mum.");
                Debug.Log($"[Birth] {species.name} baby placed beside its mother at {nearMum.position} (brain {(babyBrain != null ? "yes" : "MISSING")}).", baby);
                continue;
            }
            // [AI-GENERATED END]

            Vector3 spawnPos, home = default;
            bool gotPoint = groups > 0 ? TryGetGroupPoint(out spawnPos, out home) : TryGetSpawnPoint(out spawnPos);
            if (!gotPoint)
            {
                // [AI-GENERATED] Claude Code (Claude Opus 5.5): say why nothing spawned instead of failing silently
                if (!warnedNoSpawn)
                {
                    warnedNoSpawn = true;
                    Debug.LogWarning($"[Spawner] {species.name}: no spawn point (world {(world == null ? "none" : world.IsReady ? "ready" : "not ready")}, NavMesh triangles {NavMesh.CalculateTriangulation().indices.Length / 3}).");
                }
                break;
            }

            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Random heading so idle animals don't all face the same way.
            var facing = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject go = Instantiate(species.visualPrefab, spawnPos, facing, transform);
            // [AI-GENERATED END]

            var entity = go.GetComponent<EcoEntity>();
            if (entity == null) entity = go.AddComponent<EcoEntity>();
            entity.SetDefinition(species);
            if (go.TryGetComponent(out AnimalBrain brain))   // [AI-GENERATED] Claude Code (Claude Opus 5.5): wander stays in this band / group
            {
                brain.SetHabitat(minElevationFraction, maxElevationFraction);
                if (groups > 0) brain.SetHome(home, groupRadius);
            }

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
