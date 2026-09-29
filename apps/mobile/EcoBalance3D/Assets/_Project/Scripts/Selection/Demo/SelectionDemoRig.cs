using UnityEngine;

/// <summary>
/// Spawns optional ground and placeholder selectables for Play-mode verification.
/// Not part of the production picking pipeline — remove with Selection/Demo when no longer needed.
/// All spawn parameters come from <see cref="SelectionDemoConfig"/>.
/// </summary>
public class SelectionDemoRig : MonoBehaviour
{
    [Tooltip("Demo-only layout, colors, ids, layers, type assets, and inspection config.")]
    [SerializeField]
    SelectionDemoConfig demoConfig;

    [Tooltip("Central simulation query used to Configure inspectables.")]
    [SerializeField]
    SimulationQuery simulationQuery;

    void Start()
    {
        if (demoConfig == null)
        {
            Debug.LogError($"{nameof(SelectionDemoRig)} requires a {nameof(SelectionDemoConfig)}.", this);
            return;
        }

        if (demoConfig.entityType == null || demoConfig.resourceType == null)
        {
            Debug.LogError($"{nameof(SelectionDemoRig)}: assign entityType and resourceType on the demo config.", this);
            return;
        }

        if (demoConfig.inspectionConfig == null || simulationQuery == null)
        {
            Debug.LogError($"{nameof(SelectionDemoRig)}: assign InspectionConfig and SimulationQuery.", this);
            return;
        }

        if (demoConfig.demoResource == null)
        {
            Debug.LogError($"{nameof(SelectionDemoRig)}: assign demoResource on the demo config.", this);
            return;
        }

        SpawnGround();
        SpawnEntityPlaceholder();
        SpawnResourcePlaceholder();
    }

    void SpawnGround()
    {
        var ground = GameObject.CreatePrimitive(demoConfig.groundPrimitive);
        ground.name = demoConfig.groundObjectName;
        ground.transform.SetParent(transform, false);
        ground.transform.position = demoConfig.groundPosition;
        ground.transform.localScale = demoConfig.groundScale;
        ground.layer = demoConfig.groundLayer;

        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = demoConfig.groundColor;
    }

    void SpawnEntityPlaceholder()
    {
        var go = CreatePlaceholderBase(
            demoConfig.entityObjectName,
            demoConfig.entityPosition,
            demoConfig.entityColor);

        var selectable = go.AddComponent<Selectable>();
        selectable.Configure(demoConfig.entityType, demoConfig.entityId);

        var inspectable = go.AddComponent<EntityInspectable>();
        inspectable.Configure(demoConfig.inspectionConfig, simulationQuery, demoConfig.entityType);

        go.SetActive(true);
    }

    void SpawnResourcePlaceholder()
    {
        var go = CreatePlaceholderBase(
            demoConfig.resourceObjectName,
            demoConfig.resourcePosition,
            demoConfig.resourceColor);

        var selectable = go.AddComponent<Selectable>();
        selectable.Configure(demoConfig.resourceType, demoConfig.resourceId);

        var inspectable = go.AddComponent<ResourceInspectable>();
        inspectable.Configure(
            demoConfig.inspectionConfig,
            simulationQuery,
            demoConfig.resourceType,
            demoConfig.demoResource);

        go.SetActive(true);
    }

    GameObject CreatePlaceholderBase(string objectName, Vector3 position, Color color)
    {
        var go = GameObject.CreatePrimitive(demoConfig.placeholderPrimitive);
        go.name = objectName;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.layer = demoConfig.selectableLayer;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        return go;
    }
}
