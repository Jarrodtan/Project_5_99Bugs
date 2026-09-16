using TMPro;
using UnityEngine;

/// <summary>
/// Central visual and layout knobs for the inspector panel. Panel scripts must read these fields —
/// never invent sizes, colors, fonts, or spacing in C# at runtime.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Inspection UI Config", fileName = "InspectionUiConfig")]
public class InspectionUiConfig : ScriptableObject
{
    [Header("Panel")]
    [Tooltip("Width and height of the panel RectTransform (pixels).")]
    public Vector2 panelSize = new Vector2(320f, 220f);

    [Tooltip("Anchored position relative to the top-left of the screen (x right, y down when negative).")]
    public Vector2 panelAnchoredPosition = new Vector2(20f, -20f);

    [Tooltip("Panel background color including alpha.")]
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Inner padding around title and rows (pixels).")]
    public float panelPadding = 12f;

    [Tooltip("Canvas sorting order for the Screen Space Overlay.")]
    public int canvasSortOrder = 100;

    [Header("Title")]
    [Tooltip("TMP font size for the display-name title.")]
    public float titleFontSize = 22f;

    [Tooltip("Title text color.")]
    public Color titleColor = Color.white;

    [Tooltip("Optional shared TMP font. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Rows")]
    [Tooltip("TMP font size for label and value columns.")]
    public float rowFontSize = 16f;

    [Tooltip("Stat label column color.")]
    public Color labelColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Tooltip("Stat value column color.")]
    public Color valueColor = Color.white;

    [Tooltip("Preferred height of each stat row (pixels).")]
    public float rowHeight = 28f;

    [Tooltip("Vertical spacing between rows (pixels).")]
    public float rowSpacing = 4f;

    [Tooltip("Fraction of row width reserved for the label column (0–1). Remainder goes to the value.")]
    [Range(0.1f, 0.9f)]
    public float labelColumnWidthRatio = 0.45f;

    [Header("Behaviour")]
    [Tooltip("When true, the panel starts hidden until the first valid Show.")]
    public bool startHidden = true;
}
