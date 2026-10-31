using TMPro;
using UnityEngine;

/// <summary>
/// Shared look and timing for the screen-space tooltip host.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Tooltip Style", fileName = "TooltipStyle")]
public class TooltipStyle : ScriptableObject
{
    [Header("Frame")]
    [Tooltip("Tooltip background color including alpha.")]
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Inner padding around tooltip content (pixels).")]
    public float padding = 10f;

    [Tooltip("Maximum content width before the text wraps (pixels).")]
    public float maxWidth = 240f;

    [Tooltip("Gap between the target rect and the tooltip frame (pixels).")]
    public float gap = 8f;

    [Tooltip("Inset from each screen edge when clamping the tooltip (pixels).")]
    public Vector2 screenPadding = new Vector2(8f, 8f);

    [Tooltip("Canvas sorting order for the Screen Space Overlay canvas.")]
    public int canvasSortOrder = 200;

    [Header("Text")]
    [Tooltip("TMP font size for text tooltips.")]
    public float fontSize = 16f;

    [Tooltip("Text color for text tooltips.")]
    public Color textColor = Color.white;

    [Tooltip("Optional TMP font. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Timing")]
    [Tooltip("Seconds the mouse must stay over a target before the tooltip opens.")]
    public float hoverDelaySeconds = 0.35f;

    [Tooltip("Seconds a touch must stay down before the tooltip opens.")]
    public float longPressSeconds = 0.45f;

    [Tooltip("Pointer travel (pixels) that cancels a pending long-press.")]
    public float longPressMoveSlop = 16f;
}
