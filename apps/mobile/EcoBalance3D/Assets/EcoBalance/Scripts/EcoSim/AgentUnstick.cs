// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Safety net for crowded runs (Free mode with up to 100 of each species): an animal that wants to move
/// but has not moved for a few seconds gets unstuck. Causes seen: agents jammed in a packed herd, a path
/// that ends at water the animal may not cross (a closed ford), or an animal left just off the NavMesh.
///   - off the NavMesh: put it back on the nearest walkable ground;
///   - path blocked or ending short: drop the path, so the brain picks a new place to go;
///   - jammed by neighbours: same, plus a small random step so the crowd untangles.
/// Never acts while the simulation is paused, the animal is busy (eating, drinking, caught) or standing on purpose.
/// Added automatically by AnimalBrain.
/// </summary>
[RequireComponent(typeof(AnimalBrain))]
public class AgentUnstick : MonoBehaviour
{
    const float CheckEvery = 0.5f;      // seconds
    const float StuckAfter = 3f;        // seconds of wanting to move without moving
    const float MovedMetres = 0.3f;     // less than this in StuckAfter seconds counts as not moving

    // Unity computes every agent's path from one shared queue, 100 search steps per frame by default.
    // With 200 animals asking for paths a few times a second the queue backed up: some animals waited
    // seconds for a path (panel says Exploring / Running, legs still) while others moved.
    const int PathStepsPerFrame = 1000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RaisePathBudget()
    {
        if (NavMesh.pathfindingIterationsPerFrame < PathStepsPerFrame)
            NavMesh.pathfindingIterationsPerFrame = PathStepsPerFrame;
    }

    // Freeze report (only with -freezeProbe): every ReportEvery seconds, one Console line per animal that has
    // not moved since the last report, with everything that could hold it still.
    const float ReportEvery = 10f;
    Vector3 reportPos;
    float reportTimer = ReportEvery;

    static readonly bool Report = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), FreezeProbe.Arg) >= 0;

    void LateUpdate()
    {
        if (!Report) return;   // only in the headless freeze check (FreezeProbe), not in normal play
        reportTimer -= Time.deltaTime;
        if (reportTimer > 0f) return;
        reportTimer = ReportEvery;
        Vector3 d = transform.position - reportPos; d.y = 0f;
        reportPos = transform.position;
        if (d.magnitude > 0.5f) return;
        var brain = GetComponent<AnimalBrain>();
        if (brain == null || brain.IsCaught) return;
        var a = agent != null ? agent : GetComponent<NavMeshAgent>();
        string nav = a == null ? "NO AGENT" :
            $"agent on {a.enabled}, onMesh {(a.enabled && a.isOnNavMesh)}, stopped {(a.enabled && a.isOnNavMesh && a.isStopped)}, " +
            $"hasPath {(a.enabled && a.hasPath)}, pending {(a.enabled && a.pathPending)}, status {(a.enabled ? a.pathStatus.ToString() : "-")}, " +
            $"left {(a.enabled && a.isOnNavMesh ? a.remainingDistance : -1f):0.0} m, speed {a.speed:0.0}, vel {(a.enabled ? a.velocity.magnitude : 0f):0.00}, mask {a.areaMask}";
        float open = 0f;   // longest straight run over walkable ground, 8 directions, up to 20 m
        for (int k = 0; k < 8; k++)
        {
            Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * 20f;
            float r = NavMesh.Raycast(transform.position, transform.position + dir, out var e, NavMesh.AllAreas)
                ? Vector3.Distance(transform.position, e.position) : 20f;
            open = Mathf.Max(open, r);
        }
        Debug.LogWarning($"[Frozen] {name} at {transform.position:F0} hasn't moved in {ReportEvery:0}s | {brain.DebugWhyStill()} | {nav} | open ground {open:0.0} m, water {HimalayaRiver.DistanceToAnyWater(transform.position):0.0} m", this);
    }

    const float SmallPatch = 4f;   // metres of straight walkable run: less than this in every direction = trapped

    /// <summary>Longest straight run over walkable ground from <paramref name="p"/>, 8 directions, up to 20 m.</summary>
    public static float OpenGround(Vector3 p)
    {
        float open = 0f;
        for (int k = 0; k < 8; k++)
        {
            Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * 20f;
            float r = NavMesh.Raycast(p, p + dir, out var e, NavMesh.AllAreas) ? Vector3.Distance(p, e.position) : 20f;
            if (r > open) open = r;
            if (open >= 20f) break;
        }
        return open;
    }

    bool TryFindOpenGround(out Vector3 spot)
    {
        for (float ring = 5f; ring <= 40f; ring += 5f)
            for (int k = 0; k < 8; k++)
            {
                Vector3 c = transform.position + Quaternion.Euler(0f, k * 45f + ring * 7f, 0f) * Vector3.forward * ring;
                if (NavMesh.SamplePosition(c, out var h, 4f, agent.areaMask) && OpenGround(h.position) >= 10f)
                { spot = h.position; return true; }
            }
        spot = default;
        return false;
    }

    NavMeshAgent agent;
    float timer, stuckTime;
    int unstuckCount;
    Vector3 lastPos;

    void Start()
    {
        RaisePathBudget();   // the start-up value gets reset (NavMesh rebuild), so set it again per animal: one compare
        agent = GetComponent<NavMeshAgent>();
        lastPos = transform.position;
        timer = Random.value * CheckEvery;
    }

    void Update()
    {
        if (agent == null) { agent = GetComponent<NavMeshAgent>(); if (agent == null) return; }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = CheckEvery;

        // Agent left switched off (spawned just off the NavMesh): the brain skips its whole update, so the
        // animal never moves. Put it on the nearest walkable ground and switch it on.
        if (!agent.enabled)
        {
            if (NavMesh.SamplePosition(transform.position, out var near, 15f, NavMesh.AllAreas))
            {
                transform.position = near.position;
                agent.enabled = true;
                if (!agent.isOnNavMesh) agent.enabled = false;
                else Debug.Log($"[Unstick] {name}: agent was off (spawned off the NavMesh); placed back on it.", this);
            }
            return;
        }

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 6f, agent.areaMask)) agent.Warp(hit.position);
            return;
        }

        // Busy animals (eating, drinking, caught, paused) have their agent stopped; everyone else should be moving
        // now that nobody pauses between walks, whatever its path says ("arrived" in a jammed herd counts too).
        var brain = GetComponent<AnimalBrain>();
        bool wantsToMove = !agent.isStopped && brain != null && !brain.IsCaught
                        && brain.CurrentAction != AnimalBrain.AnimalAction.Reproducing;
        Vector3 d = transform.position - lastPos; d.y = 0f;
        if (!wantsToMove || d.magnitude > MovedMetres) { stuckTime = 0f; lastPos = transform.position; return; }

        stuckTime += CheckEvery;
        if (stuckTime < StuckAfter) return;

        stuckTime = 0f;
        lastPos = transform.position;
        if (Report && ++unstuckCount <= 3)   // a few per animal, enough to see why without flooding the Console
            Debug.Log($"[Unstick] {name}: wanted to move for {StuckAfter:0}s without moving " +
                      $"(path {agent.pathStatus}, left {agent.remainingDistance:0.0} m, speed {agent.speed:0.0}). Nudged.", this);
        // On a tiny patch of walkable ground (a ledge on a steep slope): it can never walk off, so move it to
        // proper open ground nearby.
        if (OpenGround(transform.position) < SmallPatch && TryFindOpenGround(out var open))
        {
            agent.Warp(open);
            Debug.Log($"[Unstick] {name}: was on a tiny walkable patch; moved to open ground.", this);
            return;
        }
        // A short random step out of the jam, then let the brain choose again.
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
        if (NavMesh.SamplePosition(transform.position + new Vector3(r.x, 0f, r.y), out var step, 3f, agent.areaMask))
            agent.SetDestination(step.position);
        else
            agent.ResetPath();
    }
}
// [AI-GENERATED END]
