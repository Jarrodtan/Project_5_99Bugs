using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Category whose option clicks only close the menu. The shell closes the menu before this runs.
/// Assign one component per hub category that should not change the simulation.
/// Prototype UI — not approved Feature Plan work.
/// </summary>
public class CloseOnlyHandler : MonoBehaviour, IActionMenuCategoryHandler
{
    [Tooltip("Must match one category id on the action menu hub.")]
    [SerializeField]
    string categoryId = string.Empty;

    [Tooltip("Hub whose static option list is shown for this category.")]
    [SerializeField]
    ActionMenuConfig config;

    /// <inheritdoc />
    public string CategoryId => categoryId;

    /// <inheritdoc />
    public IReadOnlyList<ActionMenuOption> GetOptions()
    {
        if (config != null && config.TryGetCategory(categoryId, out var category) && category.options != null)
            return category.options;

        return new List<ActionMenuOption>();
    }

    /// <inheritdoc />
    public void OnOptionChosen(string optionId, ActionMenuSettings settings)
    {
    }
}
