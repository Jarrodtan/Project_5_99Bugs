using UnityEngine;
using UnityEngine.AI;

// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj

// Moves to a random point within range and reports Success on arrival - same behavior
// AnimalBrain.Roam() had, just reframed as a node so it can compete with other goals.
public class WanderExecutor : IActionExecutor
{
    const float wanderRadius = 10f;
    const float arriveDistance = 0.5f;

    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        if (ctx.agent == null) return NodeStatus.Failure;

        if (!ctx.actionTarget.ContainsKey(action))
        {
            var randomDir = Random.insideUnitSphere * wanderRadius + ctx.transform.position;
            if (NavMesh.SamplePosition(randomDir, out var hit, wanderRadius, NavMesh.AllAreas))
            {
                ctx.actionTarget[action] = hit.position;
                ctx.agent.SetDestination(hit.position);
            }
            else
            {
                return NodeStatus.Failure;
            }
        }

        if (!ctx.agent.pathPending && ctx.agent.remainingDistance <= arriveDistance)
        {
            ctx.actionTarget.Remove(action);
            return NodeStatus.Success;
        }
        return NodeStatus.Running;
    }
}

// Stands still for action.duration seconds, then succeeds.
public class IdleExecutor : IActionExecutor
{
    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        if (!ctx.actionStartTime.TryGetValue(action, out var start))
        {
            start = Time.time;
            ctx.actionStartTime[action] = start;
            if (ctx.agent != null) ctx.agent.ResetPath();
        }

        if (Time.time - start >= action.duration)
        {
            ctx.actionStartTime.Remove(action);
            return NodeStatus.Success;
        }
        return NodeStatus.Running;
    }
}

// Runs directly away from the nearest predator (a species this animal's own species
// has an Eats relationship pointed AT it) for action.duration seconds.
public class FleeExecutor : IActionExecutor
{
    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        if (ctx.agent == null) return NodeStatus.Failure;

        if (!ctx.actionStartTime.TryGetValue(action, out var start))
        {
            start = Time.time;
            ctx.actionStartTime[action] = start;

            // TODO: replace with a real nearest-predator lookup once predator instances
            // are queryable (e.g. via a shared spatial registry populated by CreatureSpawner).
            var threat = GameObject.FindGameObjectWithTag("Predator");
            var awayDir = threat != null
                ? (ctx.transform.position - threat.transform.position).normalized
                : Random.insideUnitSphere;

            var fleeTarget = ctx.transform.position + awayDir * 8f;
            if (NavMesh.SamplePosition(fleeTarget, out var hit, 8f, NavMesh.AllAreas))
                ctx.agent.SetDestination(hit.position);
        }

        if (Time.time - start >= action.duration)
        {
            ctx.actionStartTime.Remove(action);
            return NodeStatus.Success;
        }
        return NodeStatus.Running;
    }
}

// Finds the nearest edible ResourcePatch, walks to it, stands there for action.duration
// while "eating", then takes action.resourceConsumedPerUse from it.
// Returns Failure when nothing edible is in range - so a goal that lists Wander as its
// next possibleAction makes the animal roam until it spots food.
// Patches must sit on (or within 2 units of) the NavMesh.
public class SeekResourceExecutor : IActionExecutor
{
    const float reachDistance = 1.2f;

    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        if (ctx.agent == null) return NodeStatus.Failure;

        ctx.actionPatch.TryGetValue(action, out var patch);

        // No target yet, or it got eaten out / destroyed since last tick: look for a new one.
        if (patch == null || !patch.IsEdible)
        {
            Clear(ctx, action);

            var wanted = action.targetResource != null
                ? new System.Collections.Generic.List<ResourceDefinition> { action.targetResource }
                : ctx.species.GetResourceTargets(RelationshipType.Eats);

            patch = ResourcePatch.FindNearest(ctx.transform.position, wanted, action.searchRadius);
            if (patch == null) return NodeStatus.Failure;

            if (!NavMesh.SamplePosition(patch.transform.position, out var hit, 2f, NavMesh.AllAreas))
                return NodeStatus.Failure;

            ctx.actionPatch[action] = patch;
            ctx.agent.SetDestination(hit.position);
        }

        var flat = patch.transform.position - ctx.transform.position;
        flat.y = 0f;
        if (flat.magnitude > reachDistance)
            return NodeStatus.Running; // still walking there

        // Arrived: stand and eat for action.duration.
        if (!ctx.actionStartTime.TryGetValue(action, out var start))
        {
            ctx.actionStartTime[action] = Time.time;
            ctx.agent.ResetPath();
            return NodeStatus.Running;
        }
        if (Time.time - start < action.duration)
            return NodeStatus.Running;

        float eaten = patch.Consume(action.resourceConsumedPerUse);
        Clear(ctx, action);
        return eaten > 0f ? NodeStatus.Success : NodeStatus.Failure;
    }

    static void Clear(BTContext ctx, ActionDefinition action)
    {
        ctx.actionPatch.Remove(action);
        ctx.actionStartTime.Remove(action);
    }
}

// TODO: same gap as SeekResource, but for GetSpeciesTargets(Eats) - needs a way to find
// the nearest live instance of prey (probably a registry the prey's own brain registers
// itself into on spawn, since CreatureSpawner only tracks visuals per-species right now).
public class HuntTargetExecutor : IActionExecutor
{
    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        Debug.LogWarning("HuntTargetExecutor not wired to world prey instances yet.");
        return NodeStatus.Failure;
    }
}

// TODO: decide what "Reproduce" should actually do at the per-instance visual level -
// the population-level sim (EcosystemSimulator.Tick) already handles reproduction rate
// numerically, so this may just need to play an animation/VFX rather than spawn anything.
public class ReproduceExecutor : IActionExecutor
{
    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        return NodeStatus.Success;
    }
}
