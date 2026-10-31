using TMPro;
using UnityEngine;

/// <summary>
/// Shared look for any list of labeled stats ("Label: value" rows) — the Population tab and the
/// Inspect tab both read it, so population data and an inspected object's data render identically.
/// Applied through <see cref="StatListStyling"/>; content-specific states (e.g. extinct) stay on each panel's config.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Stat List Style", fileName = "StatListStyle")]
public class StatListStyle : ScriptableObject
{
    [Header("Panel")]
    [Tooltip("Panel background color including alpha.")]
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Inner padding around title and rows (pixels).")]
    public int panelPadding = 12;

    [Header("Title")]
    [Tooltip("TMP font size for the list heading.")]
    public float titleFontSize = 22f;

    [Tooltip("Heading text color.")]
    public Color titleColor = Color.white;

    [Tooltip("Optional shared TMP font for title and rows. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Rows")]
    [Tooltip("Row text. {0} = label, {1} = value.")]
    public string labelValueFormat = "{0}: {1}";

    [Tooltip("TMP font size for rows.")]
    public float rowFontSize = 18f;

    [Tooltip("Preferred height of each row (pixels).")]
    public float rowHeight = 30f;

    [Tooltip("Vertical spacing between title and rows (pixels).")]
    public float rowSpacing = 4f;

    [Tooltip("Horizontal inset of row text inside the row background (pixels).")]
    public float rowTextInset = 6f;

    [Tooltip("Width reserved on the right of each row for an indicator such as a trend arrow (pixels).")]
    public float trailingColumnWidth = 24f;

    [Tooltip("Row text color in the normal state.")]
    public Color textColor = Color.white;

    [Tooltip("Resting row background color (usually transparent).")]
    public Color rowBackgroundColor = new Color(1f, 1f, 1f, 0f);
}
