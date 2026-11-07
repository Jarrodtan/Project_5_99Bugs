using UnityEngine;

/// <summary>
/// Central visual knobs for the selection highlight ring/decal.
/// Highlighter and visual modules must read these fields — never invent colors, scales, or offsets in C#.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Selection Highlight Config", fileName = "SelectionHighlightConfig")]
public class SelectionHighlightConfig : ScriptableObject
{
    [Header("Ring")]
    [Tooltip("Local position of the ring relative to the selected object.")]
    public Vector3 localOffset = new Vector3(0f, 0.05f, 0f);

    [Tooltip("Local scale of the ring (X/Z width, Y thickness for procedural cylinder).")]
    public Vector3 localScale = new Vector3(1.4f, 0.05f, 1.4f);

    [Tooltip("Ring color including alpha.")]
    public Color color = new Color(0.2f, 0.85f, 1f, 0.85f);

    [Tooltip("Optional prefab. When empty, a flat cylinder is created and styled from this config.")]
    public GameObject ringPrefab;

    [Tooltip("Optional material override for the procedural ring. When empty, a runtime unlit material uses color.")]
    public Material ringMaterial;

    [Tooltip("Sorting order applied to the procedural ring renderer when supported.")]
    public int sortingOrder;

    [Tooltip("Optional sorting layer name for the procedural ring. Empty keeps the default layer.")]
    public string sortingLayerName = string.Empty;
}
