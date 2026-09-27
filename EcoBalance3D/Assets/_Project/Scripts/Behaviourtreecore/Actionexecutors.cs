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

// TODO: SeekResource needs a way to find the nearest in-world instance of a resource
// this species Eats (SpeciesDefinition.GetResourceTargets). The sim currently tracks
// resources as pooled amounts (ResourceDefinition.currentAmount), not world objects,
// so there's nothing to path to yet. Wire this up once resource nodes exist in-scene
// (e.g. tag them with the ResourceDefinition's name and FindGameObjectsWithTag here).
public class SeekResourceExecutor : IActionExecutor
{
    public NodeStatus Execute(BTContext ctx, ActionDefinition action)
    {
        Debug.LogWarning("SeekResourceExecutor not wired to world resource instances yet.");
        return NodeStatus.Failure;
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
