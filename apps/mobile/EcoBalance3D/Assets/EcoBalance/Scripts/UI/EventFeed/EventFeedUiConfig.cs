using TMPro;
using UnityEngine;

/// <summary>
/// Central visual, layout, text, and timing knobs for Display 3 (<see cref="EventFeedPanel"/>).
/// Panel and row scripts must read these fields — never invent sizes, colors, strings, or timings at runtime.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Event Feed UI Config", fileName = "EventFeedUiConfig")]
public class EventFeedUiConfig : ScriptableObject
{
    [Header("Panel")]
    [Tooltip("Panel width (pixels) when standalone. Ignored inside TabbedStatsPanel.")]
    public float panelWidth = 300f;

    [Tooltip("Screen corner the standalone panel is pinned to, as a 0–1 anchor. (0,0) = bottom-left.")]
    public Vector2 panelAnchor = new Vector2(0f, 0f);

    [Tooltip("Pixel offset from the anchored corner when standalone.")]
    public Vector2 panelAnchoredPosition = new Vector2(20f, 20f);

    [Tooltip("Panel background color including alpha.")]
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Inner padding around title and rows (pixels).")]
    public int panelPadding = 12;

    [Tooltip("Canvas sorting order for the standalone Screen Space Overlay canvas.")]
    public int canvasSortOrder = 92;

    [Header("Title")]
    [Tooltip("Heading shown above the feed when standalone, and the tab label when hosted. Empty hides the title.")]
    public string titleText = "Events";

    [Tooltip("TMP font size for the title.")]
    public float titleFontSize = 22f;

    [Tooltip("Title text color.")]
    public Color titleColor = Color.white;

    [Tooltip("Optional shared TMP font. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Feed")]
    [Tooltip("Most entries shown at once, newest first. Older entries roll off the bottom.")]
    public int maxVisibleEntries = 8;

    [Tooltip("TMP font size for feed entries.")]
    public float rowFontSize = 15f;

    [Tooltip("Minimum height of each entry (pixels). Long entries wrap and grow taller.")]
    public float rowMinHeight = 22f;

    [Tooltip("Vertical spacing between entries (pixels).")]
    public float rowSpacing = 2f;

    [Tooltip("Horizontal inset of entry text inside its background (pixels).")]
    public float rowTextInset = 6f;

    [Tooltip("Entry text. {0} = event description.")]
    public string entryFormat = "{0}";

    [Tooltip("Shown instead of entries while the provider has no events yet.")]
    public string noDataText = "Waiting for simulation events…";

    [Header("Colors")]
    [Tooltip("Text color of the newest entry.")]
    public Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    [Tooltip("Text alpha multiplier for the oldest visible entry; entries in between blend. 1 disables the fade.")]
    [Range(0f, 1f)]
    public float oldestEntryAlpha = 0.45f;

    [Tooltip("Entry background at rest (usually transparent).")]
    public Color rowBackgroundColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("Entry text color when the provider has no events.")]
    public Color unavailableTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("New entry highlight")]
    [Tooltip("Entry background when it first appears; fades to the resting background.")]
    public Color newEntryHighlightColor = new Color(0.35f, 0.7f, 1f, 0.35f);

    [Tooltip("Seconds for the new-entry highlight to fade. Zero disables it.")]
    public float highlightDurationSeconds = 1.2f;

    [Header("Behaviour")]
    [Tooltip("Seconds between reads from the event provider. Zero reads every frame.")]
    public float pollIntervalSeconds = 0.25f;
}
