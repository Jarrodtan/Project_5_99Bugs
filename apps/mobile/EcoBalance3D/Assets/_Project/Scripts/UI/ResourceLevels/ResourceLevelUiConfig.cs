using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Per-resource gauge limits that differ from the config defaults.
/// </summary>
[Serializable]
public struct ResourceGaugeOverride
{
    [Tooltip("Resource id from ISimulationDataProvider (e.g. grass).")]
    public string id;

    [Tooltip("Level that fills the bar completely. Mirror ResourceDefinition.maxAmount for live data.")]
    public float maxLevel;

    [Tooltip("Critical when level / maxLevel is at or below this fraction (0–1).")]
    [Range(0f, 1f)]
    public float criticalFraction;
}

/// <summary>
/// Central visual, layout, text, gauge-limit, and timing knobs for Display 2 (<see cref="ResourceLevelPanel"/>).
/// Panel and row scripts must read these fields — never invent sizes, colors, strings, limits, or timings at runtime.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Resource Level UI Config", fileName = "ResourceLevelUiConfig")]
public class ResourceLevelUiConfig : ScriptableObject
{
    [Header("Panel")]
    [Tooltip("Panel width (pixels). Height grows with the number of resource rows.")]
    public float panelWidth = 300f;

    [Tooltip("Screen corner the panel is pinned to, as a 0–1 anchor. (1,1) = top-right.")]
    public Vector2 panelAnchor = new Vector2(1f, 1f);

    [Tooltip("Pixel offset from the anchored corner. Negative X moves left, negative Y moves down.")]
    public Vector2 panelAnchoredPosition = new Vector2(-20f, -20f);

    [Tooltip("Panel background color including alpha.")]
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [Tooltip("Inner padding around title and rows (pixels).")]
    public int panelPadding = 12;

    [Tooltip("Canvas sorting order for the Screen Space Overlay canvas.")]
    public int canvasSortOrder = 91;

    [Header("Title")]
    [Tooltip("Heading shown above the resource rows. Empty hides the title.")]
    public string titleText = "Resources";

    [Tooltip("TMP font size for the title.")]
    public float titleFontSize = 22f;

    [Tooltip("Title text color.")]
    public Color titleColor = Color.white;

    [Tooltip("Optional shared TMP font. When empty, TMP default font is used.")]
    public TMP_FontAsset font;

    [Header("Rows")]
    [Tooltip("TMP font size for resource name, value, and warning icon.")]
    public float rowFontSize = 17f;

    [Tooltip("Height of the name/value text line (pixels).")]
    public float textLineHeight = 22f;

    [Tooltip("Height of the level bar (pixels).")]
    public float barHeight = 10f;

    [Tooltip("Gap between the text line and the bar (pixels).")]
    public float barGap = 3f;

    [Tooltip("Vertical spacing between rows (pixels).")]
    public float rowSpacing = 8f;

    [Tooltip("Fraction of the text line reserved for the resource name (0–1). Remainder goes to the value.")]
    [Range(0.1f, 0.9f)]
    public float labelColumnWidthRatio = 0.5f;

    [Tooltip("Width reserved on the right of the text line for the warning icon (pixels).")]
    public float iconColumnWidth = 22f;

    [Header("Text")]
    [Tooltip("Numeric readout. {0} = level, {1} = max level, {2} = percent of max (0–100).")]
    public string valueFormat = "{0:0} / {1:0}";

    [Tooltip("Readout when the data provider has no value for the resource.")]
    public string unavailableText = "—";

    [Tooltip("Shown instead of rows while the data provider lists no resources (e.g. live adapter not ready).")]
    public string noDataText = "Waiting for simulation data…";

    [Header("Gauge limits")]
    [Tooltip("Level that fills the bar for resources without an override. Matches ResourceDefinition.maxAmount's default.")]
    public float defaultMaxLevel = 100f;

    [Tooltip("Critical when level / max is at or below this fraction, for resources without an override (0–1).")]
    [Range(0f, 1f)]
    public float defaultCriticalFraction = 0.2f;

    [Tooltip("Per-resource max level and critical threshold. Ids not listed use the defaults above.")]
    public List<ResourceGaugeOverride> overrides = new();

    [Header("Colors")]
    [Tooltip("Resource name color while not critical.")]
    public Color labelColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Tooltip("Numeric readout color while not critical.")]
    public Color valueColor = Color.white;

    [Tooltip("Empty part of the level bar.")]
    public Color barBackgroundColor = new Color(1f, 1f, 1f, 0.12f);

    [Tooltip("Filled part of the level bar while not critical.")]
    public Color barFillColor = new Color(0.35f, 0.8f, 0.45f, 1f);

    [Tooltip("Bar fill, text, and warning icon color while critical.")]
    public Color criticalColor = new Color(1f, 0.3f, 0.25f, 1f);

    [Tooltip("Text color when the provider has no value for the resource.")]
    public Color unavailableColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Critical cue")]
    [Tooltip("Glyph shown on the right of a critical row.")]
    public string warningGlyph = "!";

    [Tooltip("Pulses per second for the warning icon and critical bar. Zero keeps them solid.")]
    public float criticalPulsesPerSecond = 1.5f;

    [Tooltip("Lowest alpha reached during a critical pulse (0–1).")]
    [Range(0f, 1f)]
    public float criticalPulseMinAlpha = 0.35f;

    [Header("Behaviour")]
    [Tooltip("How fast the bar slides to a new level (fraction of the gap closed per second, roughly). Zero snaps instantly.")]
    public float barFillLerpSpeed = 6f;

    [Tooltip("Seconds between reads from the data provider. Zero reads every frame.")]
    public float pollIntervalSeconds = 0.25f;

    /// <summary>
    /// Gauge limits for a resource id: its override when one exists, otherwise the defaults.
    /// </summary>
    /// <param name="resourceId">Resource id from the data provider.</param>
    /// <returns>Max level and critical fraction to evaluate the resource against.</returns>
    public ResourceGaugeLimits GetLimits(string resourceId)
    {
        if (overrides != null && !string.IsNullOrEmpty(resourceId))
        {
            for (var i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].id == resourceId)
                    return new ResourceGaugeLimits(overrides[i].maxLevel, overrides[i].criticalFraction);
            }
        }

        return new ResourceGaugeLimits(defaultMaxLevel, defaultCriticalFraction);
    }
}
