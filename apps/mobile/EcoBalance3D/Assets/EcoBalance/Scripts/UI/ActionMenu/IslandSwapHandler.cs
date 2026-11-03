using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Island category: options come from <see cref="IslandPlatformConfig.islands"/> and clicks call <see cref="IslandManager.RequestSwap"/>.
/// Prototype UI — not approved Feature Plan work.
/// </summary>
public class IslandSwapHandler : MonoBehaviour, IActionMenuCategoryHandler, IIslandSwapListener
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

    /// <inheritdoc />
    public void OnIslandSpawned(IslandDefinition definition, Transform islandRoot, IslandPlatformConfig config)
    {
        if (islandRoot == null)
            return;

        StartCoroutine(ShowOnlyThisIsland(islandRoot));
    }

    /// <inheritdoc />
    public void OnIslandDespawning(IslandDefinition definition, Transform islandRoot)
    {
    }

    IEnumerator ShowOnlyThisIsland(Transform islandRoot)
    {
        HimalayaTerrainBuilder builder = null;
        var wait = 0f;
        while (wait < 30f && islandRoot != null)
        {
            builder = islandRoot.GetComponentInChildren<HimalayaTerrainBuilder>(true);
            if (builder != null && builder.IsBuilt && builder.Terrain != null)
                break;

            wait += Time.deltaTime;
            yield return null;
        }

        if (builder == null || builder.Terrain == null)
            yield break;

        var mine = builder.Terrain;
        var terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < terrains.Length; i++)
        {
            if (terrains[i] != null)
                terrains[i].gameObject.SetActive(terrains[i] == mine);
        }

        var camera = FindAnyObjectByType<TerrainCameraController>();
        if (camera != null)
            camera.FocusTerrain(mine);
    }
}
