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

            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            // Nearest animal whose species Eats ours (T01 relationships), via EcoRegistry.
            // Nothing dangerous in range -> Failure, so the tree falls through to other goals.
            var threat = EcoRegistry.NearestPredator(ctx.species, ctx.transform.position, ActionSearch.Range(action));
            if (threat == null)
            {
                ctx.actionStartTime.Remove(action);
                return NodeStatus.Failure;
            }
            var awayDir = ctx.transform.position - threat.position;
            awayDir.y = 0f;
            awayDir = awayDir.sqrMagnitude > 0.0001f ? awayDir.normalized : Random.insideUnitSphere;
            // [AI-GENERATED END]

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

// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
// Chases the nearest animal this species Eats (EcoRegistry.NearestPrey), re-pathing as it
// moves. On reaching it, stands for action.duration ("eating") and succeeds, which satisfies
// the related need. Population numbers stay with EcosystemSimulator - this is the visible
// behaviour only. No prey in range -> Failure, so a following Wander lets it roam and search.
public class HuntTargetExecutor : IActionExecutor
{
    const float catchDistance = 1.5f;
    const float repathDistance = 0.5f;

    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        if (ctx.agent == null) return NodeStatus.Failure;

        var prey = EcoRegistry.NearestPrey(ctx.species, ctx.transform.position, ActionSearch.Range(action));
        if (prey == null)
        {
            ctx.actionStartTime.Remove(action);
            ctx.actionTarget.Remove(action);
            return NodeStatus.Failure;
        }

        var flat = prey.position - ctx.transform.position;
        flat.y = 0f;

        if (flat.magnitude > catchDistance)
        {
            // Still chasing: re-path now and then, not every tick.
            ctx.actionStartTime.Remove(action);
            if (!ctx.actionTarget.TryGetValue(action, out var last) || (last - prey.position).sqrMagnitude > repathDistance * repathDistance)
            {
                if (NavMesh.SamplePosition(prey.position, out var hit, 2f, NavMesh.AllAreas))
                    ctx.agent.SetDestination(hit.position);
                ctx.actionTarget[action] = prey.position;
            }
            return NodeStatus.Running;
        }

        // Caught: stop and "eat" for action.duration.
        if (!ctx.actionStartTime.TryGetValue(action, out var start))
        {
            ctx.actionStartTime[action] = Time.time;
            ctx.agent.ResetPath();
            return NodeStatus.Running;
        }
        if (Time.time - start < action.duration)
            return NodeStatus.Running;

        ctx.actionStartTime.Remove(action);
        ctx.actionTarget.Remove(action);
        return NodeStatus.Success;
    }
}

// Shared by Flee and Hunt: ActionDefinition.searchRadius, where 0 means unlimited.
static class ActionSearch
{
    public static float Range(ActionDefinition action)
        => action != null && action.searchRadius > 0f ? action.searchRadius : float.PositiveInfinity;
}
// [AI-GENERATED END]

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
