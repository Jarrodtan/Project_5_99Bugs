using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj

// Result of ticking a node this frame/tick.
public enum NodeStatus { Success, Failure, Running }

// Everything a node needs to act, gathered once per animal. Nodes and ActionDefinitions
// are shared data/behavior (possibly reused across many animals), so all per-instance
// state (current target, when an action started, etc.) lives here instead - never on
// the node or the ScriptableObject.
public class BTContext
{
    public GameObject gameObject;
    public Transform transform;
    public NavMeshAgent agent;
    public SpeciesDefinition species;
    public AnimalNeedsState needs;
    public float deltaTime;

    // Scratch space actions use to remember state between ticks. Keyed by the
    // ActionDefinition asset so re-entering the same action resumes cleanly.
    public readonly Dictionary<ActionDefinition, float> actionStartTime = new();
    public readonly Dictionary<ActionDefinition, Vector3> actionTarget = new();
    public readonly Dictionary<ActionDefinition, ResourcePatch> actionPatch = new();
}

public abstract class BTNode
{
    public abstract NodeStatus Tick(BTContext ctx);
}

// Runs children in priority order, returns the first non-Failure result.
// Re-evaluates from the top every tick (reactive), so a higher-priority child
// further up the list can interrupt one that was Running - which is what lets
// e.g. a Flee goal cut in over a SeekResource goal already in progress.
public class Selector : BTNode
{
    readonly List<BTNode> children;
    public Selector(List<BTNode> children) => this.children = children;

    public override NodeStatus Tick(BTContext ctx)
    {
        foreach (var child in children)
        {
            var status = child.Tick(ctx);
            if (status != NodeStatus.Failure) return status;
        }
        return NodeStatus.Failure;
    }
}

// Runs children in order; all must succeed for the sequence to succeed.
// Stops and reports Running/Failure at the first child that isn't a Success.
public class Sequence : BTNode
{
    readonly List<BTNode> children;
    public Sequence(List<BTNode> children) => this.children = children;

    public override NodeStatus Tick(BTContext ctx)
    {
        foreach (var child in children)
        {
            var status = child.Tick(ctx);
            if (status != NodeStatus.Success) return status;
        }
        return NodeStatus.Success;
    }
}
