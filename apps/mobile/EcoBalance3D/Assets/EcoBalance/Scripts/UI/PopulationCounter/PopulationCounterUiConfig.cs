using UnityEngine;

/// <summary>
/// Central visual, layout, text, and timing knobs for Display 1 (<see cref="PopulationCounterPanel"/>).
/// Generic list styling (fonts, row geometry, normal colors, "Label: value" format) comes from <see cref="listStyle"/>;
/// this config holds only population-specific placement, strings, states, and timing.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Population Counter UI Config", fileName = "PopulationCounterUiConfig")]
public class PopulationCounterUiConfig : ScriptableObject
{
    [Header("Shared style")]
    [Tooltip("Shared look for labeled-stat lists (panel, title, rows, \"Label: value\" format). Also used by the Inspect tab.")]
    public StatListStyle listStyle;

    [Header("Panel")]
    [Tooltip("Panel width (pixels). Height grows with the number of species rows.")]
    public float panelWidth = 300f;

    [Tooltip("Pixel offset of the panel's top-left corner from the screen's top-left corner.")]
    public Vector2 panelAnchoredPosition = new Vector2(20f, -20f);

    [Tooltip("Canvas sorting order for the Screen Space Overlay canvas.")]
    public int canvasSortOrder = 90;

    [Header("Title")]
    [Tooltip("Heading shown above the species rows. Empty hides the title.")]
    public string titleText = "Population";

    [Header("Text formats")]
    [Tooltip("Row text when the count is zero. {0} = display name, {1} = count.")]
    public string extinctFormat = "{0}: {1} — Extinct";

    [Tooltip("Row text when the data provider has no value for the species. {0} = display name.")]
    public string unavailableFormat = "{0}: —";

    [Tooltip("Shown instead of rows while the data provider lists no species (e.g. live adapter not ready).")]
    public string noDataText = "Waiting for simulation data…";

    [Header("State colors")]
    [Tooltip("Row text color once the species is extinct.")]
    public Color extinctTextColor = new Color(1f, 0.45f, 0.45f, 1f);

    [Tooltip("Row background color while the species is extinct (replaces the resting background).")]
    public Color extinctBackgroundColor = new Color(0.55f, 0.1f, 0.1f, 0.35f);

    [Tooltip("Row text color when the provider has no value for the species.")]
    public Color unavailableTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Change indicator")]
    [Tooltip("Glyph shown when the most recent change was an increase.")]
    public string upArrow = "▲";

    [Tooltip("Glyph shown when the most recent change was a decrease.")]
    public string downArrow = "▼";

    [Tooltip("Arrow and flash color for increases.")]
    public Color increaseColor = new Color(0.35f, 0.9f, 0.45f, 1f);

    [Tooltip("Arrow and flash color for decreases.")]
    public Color decreaseColor = new Color(1f, 0.4f, 0.35f, 1f);

    [Tooltip("Alpha of the row background at the start of a change flash (0–1).")]
    [Range(0f, 1f)]
    public float flashBackgroundAlpha = 0.45f;

    [Tooltip("Seconds for a change flash to fade back to the resting color. Zero disables the flash.")]
    public float flashDurationSeconds = 0.6f;

    [Header("Behaviour")]
    [Tooltip("Seconds between reads from the data provider. Zero reads every frame.")]
    public float pollIntervalSeconds = 0.25f;
}
