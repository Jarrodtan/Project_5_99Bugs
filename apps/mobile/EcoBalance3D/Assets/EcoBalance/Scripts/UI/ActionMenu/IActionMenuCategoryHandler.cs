using System.Collections.Generic;

/// <summary>
/// Options and click behavior for one action-menu category. Matched to the hub by <see cref="CategoryId"/>.
/// </summary>
public interface IActionMenuCategoryHandler
{
    /// <summary>Hub category id this handler serves.</summary>
    string CategoryId { get; }

    /// <summary>Options to show for this category. Never null.</summary>
    IReadOnlyList<ActionMenuOption> GetOptions();

    /// <summary>
    /// Runs after the menu closes for a chosen option.
    /// </summary>
    /// <param name="optionId">Id of the option bubble that was clicked.</param>
    /// <param name="settings">Current modal values.</param>
    void OnOptionChosen(string optionId, ActionMenuSettings settings);
}
