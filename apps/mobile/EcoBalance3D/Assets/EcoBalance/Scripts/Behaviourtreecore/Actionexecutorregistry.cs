using System.Collections.Generic;
// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj

// Actual behavior for one ActionType (movement, animation triggers, etc). Keeping this
// as an interface means adding a new ActionType later is "write one class + register
// it" - nothing about BTNode, ActionNode, or GoalSelectorNode changes.
public interface IActionExecutor
{
    NodeStatus Execute(BTContext ctx, ActionDefinition action);
}

public static class ActionExecutorRegistry
{
    static readonly Dictionary<ActionType, IActionExecutor> executors = new();
    static bool initialized;

    public static void Register(ActionType type, IActionExecutor executor) => executors[type] = executor;

    public static bool TryGet(ActionType type, out IActionExecutor executor)
    {
        EnsureDefaultsRegistered();
        return executors.TryGetValue(type, out executor);
    }

    // Registers the built-in executors the first time anything asks for one.
    // Call Register() yourself before this to override any of them.
    static void EnsureDefaultsRegistered()
    {
        if (initialized) return;
        initialized = true;

        executors.TryAdd(ActionType.Wander, new WanderExecutor());
        executors.TryAdd(ActionType.SeekResource, new SeekResourceExecutor());
        executors.TryAdd(ActionType.HuntTarget, new HuntTargetExecutor());
        executors.TryAdd(ActionType.Flee, new FleeExecutor());
        executors.TryAdd(ActionType.Reproduce, new ReproduceExecutor());
        executors.TryAdd(ActionType.Idle, new IdleExecutor());
    }
}
