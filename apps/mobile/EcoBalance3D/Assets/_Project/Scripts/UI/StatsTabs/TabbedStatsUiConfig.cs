using TMPro;
using UnityEngine;

/// <summary>
/// Central visual and layout knobs for <see cref="TabbedStatsPanel"/> (tab bar + hosted tabs).
/// Hosted panels keep their own configs for row styling; their width and position are taken from here.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Tabbed Stats UI Config", fileName = "TabbedStatsUiConfig")]
public class TabbedStatsUiConfig : ScriptableObject
{
    [Header("Placement")]
    [Tooltip("Width of the tab bar and the hosted panels (pixels). Overrides the hosted panels' own panelWidth.")]
    public float panelWidth = 300f;

    [Tooltip("Pixel offset of the tab bar's top-left corner from the screen's top-left corner.")]
    public Vector2 panelAnchoredPosition = new Vector2(20f, -20f);

    [Tooltip("Canvas sorting order for the Screen Space Overlay canvas.")]
    public int canvasSortOrder = 90;

    [Tooltip("Tab selected on start (0-based, clamped to the tab count). Falls back to the first visible tab.")]
    public int defaultTabIndex = 0;

    [Tooltip("Switch to a contextual tab when it appears or asks for focus (e.g. Inspect on every new selection).")]
    public bool focusTabWhenRevealed = true;

    [Header("Tabs")]
    [Tooltip("Height of the tab bar (pixels).")]
    public float tabHeight = 34f;

    [Tooltip("Horizontal gap between tab buttons (pixels).")]
    public float tabSpacing = 2f;

    [Tooltip("Largest TMP font size for tab labels.")]
    public float tabFontSize = 16f;

    [Tooltip("Smallest TMP font size labels may shrink to when many tabs share the bar.")]
    public float tabMinFontSize = 11f;

    [Tooltip("Horizontal inset of each label inside its tab (pixels).")]
    public float tabLabelPadding = 4f;

    [Tooltip("Optional shared TMP font. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Colors")]
    [Tooltip("Selected tab background. Match the hosted panels' background so the tab reads as attached.")]
    public Color activeTabColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Unselected tab background.")]
    public Color inactiveTabColor = new Color(0.04f, 0.05f, 0.06f, 0.75f);

    [Tooltip("Unselected tab background while hovered.")]
    public Color hoverTabColor = new Color(0.12f, 0.15f, 0.18f, 0.85f);

    [Tooltip("Selected tab label color.")]
    public Color activeTextColor = Color.white;

    [Tooltip("Unselected tab label color.")]
    public Color inactiveTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Tooltip("Seconds for a tab background to fade between hover states.")]
    public float tabColorFadeSeconds = 0.08f;
}
