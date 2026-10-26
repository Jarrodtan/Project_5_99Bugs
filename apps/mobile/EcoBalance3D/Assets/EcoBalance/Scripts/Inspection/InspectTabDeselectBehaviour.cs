/// <summary>
/// What the Inspect tab does when the selection is cleared (e.g. the user clicks empty space).
/// Selected via <see cref="InspectionUiConfig.tabDeselectBehaviour"/>. Applies only when the inspector is
/// hosted as a tab; the floating overlay always hides.
/// </summary>
public enum InspectTabDeselectBehaviour
{
    /// <summary>Team decision (default): hide the tab and fall back to the previously selected tab. Never shows stale data.</summary>
    HideTab = 0,

    /// <summary>Keep the tab showing the last object's stats, frozen and visibly marked as stale, until the next selection.</summary>
    KeepLastSelection = 1
}
