using UnityEngine;

/// <summary>
/// Data for the optional selection demo placeholders. Not part of the production picking pipeline —
/// delete Selection/Demo and this asset when real agents/resources exist.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Selection Demo Config", fileName = "SelectionDemoConfig")]
public class SelectionDemoConfig : ScriptableObject
{
    [Header("Types")]
    [Tooltip("Type asset assigned to the entity placeholder Selectable.")]
    public SelectableTypeDefinition entityType;

    [Tooltip("Type asset assigned to the resource placeholder Selectable.")]
    public SelectableTypeDefinition resourceType;

    [Header("Primitive")]
    [Tooltip("Unity primitive used for both placeholders (e.g. Cube).")]
    public PrimitiveType placeholderPrimitive = PrimitiveType.Cube;

    [Tooltip("Unity primitive used for the non-selectable ground.")]
    public PrimitiveType groundPrimitive = PrimitiveType.Plane;

    [Header("Ground")]
    [Tooltip("World position of the non-selectable ground plane.")]
    public Vector3 groundPosition = new Vector3(0f, 0f, 0f);

    [Tooltip("Local scale of the ground plane.")]
    public Vector3 groundScale = new Vector3(10f, 1f, 10f);

    [Tooltip("Unity layer index for the ground (typically Default = 0).")]
    public int groundLayer;

    [Tooltip("GameObject name for the ground.")]
    public string groundObjectName = "SelectionDemoGround";

    [Tooltip("Tint color applied to the ground renderer.")]
    public Color groundColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Entity placeholder")]
    [Tooltip("World position of the entity placeholder.")]
    public Vector3 entityPosition = new Vector3(-1.5f, 0.5f, 0f);

    [Tooltip("Tint color applied to the entity placeholder renderer.")]
    public Color entityColor = new Color(0.2f, 0.45f, 0.95f, 1f);

    [Tooltip("Instance id for the entity placeholder handle.")]
    public int entityId = 1;

    [Tooltip("GameObject name for the entity placeholder.")]
    public string entityObjectName = "DemoEntity";

    [Header("Resource placeholder")]
    [Tooltip("World position of the resource placeholder.")]
    public Vector3 resourcePosition = new Vector3(1.5f, 0.5f, 0f);

    [Tooltip("Tint color applied to the resource placeholder renderer.")]
    public Color resourceColor = new Color(0.25f, 0.8f, 0.35f, 1f);

    [Tooltip("Instance id for the resource placeholder handle (may match entityId; type differs).")]
    public int resourceId = 1;

    [Tooltip("GameObject name for the resource placeholder.")]
    public string resourceObjectName = "DemoResource";

    [Header("Selectable layer")]
    [Tooltip("Unity layer index applied to selectable placeholders (must match SelectionConfig.selectableLayers).")]
    public int selectableLayer = 6;

    [Header("Inspection")]
    [Tooltip("Shared inspection config (schema catalog). Same asset used by logger and query.")]
    public InspectionConfig inspectionConfig;

    [Tooltip("Resource definition for the resource placeholder live stats (e.g. Grass).")]
    public ResourceDefinition demoResource;
}
