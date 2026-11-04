// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Animals stay on their own bank and only swim across the river or a stream at a ford when they have to:
/// a fleeing blue sheep escaping, or a hunting snow leopard following it. Does this by adding or removing
/// the "Ford" NavMesh area (<see cref="HimalayaRiver.FordArea"/>) from the agent's area mask.
/// An animal already standing in a ford keeps it, so it finishes crossing instead of getting stuck.
/// Added automatically by AnimalBrain.
/// </summary>
[RequireComponent(typeof(AnimalBrain))]
public class FordAccess : MonoBehaviour
{
    const float CheckEvery = 0.25f;   // seconds; the action changes far slower than that

    AnimalBrain brain;
    NavMeshAgent agent;
    float timer;
    int fordBit;

    void Awake()
    {
        brain = GetComponent<AnimalBrain>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        int area = HimalayaRiver.FordArea;
        fordBit = area > 0 ? 1 << area : 0;   // no "Ford" area in the project: leave the mask alone
        timer = Random.value * CheckEvery;    // spread the checks over frames
    }

    void Update()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();   // the brain may add its agent after us
        if (fordBit == 0 || agent == null || !agent.isOnNavMesh) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = CheckEvery;

        bool allowed = brain.CurrentAction == AnimalBrain.AnimalAction.Fleeing
                    || brain.CurrentAction == AnimalBrain.AnimalAction.Hunting
                    || StandingOnFord();
        bool has = (agent.areaMask & fordBit) != 0;
        if (allowed == has) return;

        agent.areaMask = allowed ? agent.areaMask | fordBit : agent.areaMask & ~fordBit;
        // A path planned through a ford is now closed: plan again to the same place (the nearest
        // reachable point on this bank), so the animal doesn't walk up to the water and stop.
        if (!allowed && agent.hasPath) agent.SetDestination(agent.destination);
    }

    bool StandingOnFord() =>
        NavMesh.SamplePosition(transform.position, out var hit, 1f, NavMesh.AllAreas) && (hit.mask & fordBit) != 0;
}
// [AI-GENERATED END]
