using UnityEngine;
using UnityEngine.Rendering;

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
            ApplyColor(renderer, demoConfig.groundColor);
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
            ApplyColor(renderer, color);

        return go;
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // CreatePrimitive gives the built-in default material, which URP cannot draw in a player build
    // (shows magenta on Android). Swap in URP Lit before tinting. pipeline.defaultMaterial is
    // editor-only (null in player builds), so fall back to the Lit shader, which project
    // materials already reference and therefore ship in the build.
    static void ApplyColor(Renderer renderer, Color color)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultMaterial != null)
            renderer.sharedMaterial = new Material(pipeline.defaultMaterial);
        else
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
                renderer.sharedMaterial = new Material(lit);
        }
        renderer.material.color = color;
    }
    // [AI-GENERATED END]
}
