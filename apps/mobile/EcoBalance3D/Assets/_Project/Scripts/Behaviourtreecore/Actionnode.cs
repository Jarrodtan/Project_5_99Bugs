// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Yusuf Bin Abdul Rahman Bahajjaj
// Leaf node wrapping one designer-authored ActionDefinition asset.
// Dispatches to whatever IActionExecutor is registered for that action's ActionType -
// the node itself never knows how "Wander" or "HuntTarget" actually moves the animal.
public class ActionNode : BTNode
{
    readonly ActionDefinition action;
    readonly NeedDefinition relatedNeed;

    public ActionNode(ActionDefinition action, NeedDefinition relatedNeed)
    {
        this.action = action;
        this.relatedNeed = relatedNeed;
    }

    public override NodeStatus Tick(BTContext ctx)
    {
        if (action == null) return NodeStatus.Failure;

        if (!ActionExecutorRegistry.TryGet(action.type, out var executor))
        {
            UnityEngine.Debug.LogWarning($"No IActionExecutor registered for ActionType.{action.type}.");
            return NodeStatus.Failure;
        }

        var status = executor.Execute(ctx, action);

        if (status == NodeStatus.Success && relatedNeed != null && ctx.needs != null)
            ctx.needs.Satisfy(relatedNeed, action.needSatisfactionAmount);

        return status;
    }
}
