using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Island category: options come from <see cref="IslandPlatformConfig.islands"/> and clicks call <see cref="IslandManager.RequestSwap"/>.
/// Prototype UI — not approved Feature Plan work.
/// </summary>
public class IslandSwapHandler : MonoBehaviour, IActionMenuCategoryHandler
{
    [Tooltip("Must match the island category id on the action menu hub.")]
    [SerializeField]
    string categoryId = string.Empty;

    [Tooltip("Island catalog. Options are not copied onto the action menu hub.")]
    [SerializeField]
    IslandPlatformConfig platformConfig;

    [Tooltip("Swap owner.")]
    [SerializeField]
    IslandManager islandManager;

    /// <inheritdoc />
    public string CategoryId => categoryId;

    /// <inheritdoc />
    public IReadOnlyList<ActionMenuOption> GetOptions()
    {
        var results = new List<ActionMenuOption>();
        if (platformConfig == null || platformConfig.islands == null)
            return results;

        for (var i = 0; i < platformConfig.islands.Count; i++)
        {
            var definition = platformConfig.islands[i];
            if (definition == null || string.IsNullOrEmpty(definition.id))
                continue;

            results.Add(new ActionMenuOption
            {
                id = definition.id,
                label = string.IsNullOrEmpty(definition.displayName) ? definition.id : definition.displayName,
                preview = definition.islandPrefab
            });
        }

        return results;
    }

    /// <inheritdoc />
    public void OnOptionChosen(string optionId, ActionMenuSettings settings)
    {
        if (islandManager == null || platformConfig == null || platformConfig.islands == null)
            return;

        for (var i = 0; i < platformConfig.islands.Count; i++)
        {
            var definition = platformConfig.islands[i];
            if (definition != null && definition.id == optionId)
            {
                islandManager.RequestSwap(definition);
                return;
            }
        }
    }
}
