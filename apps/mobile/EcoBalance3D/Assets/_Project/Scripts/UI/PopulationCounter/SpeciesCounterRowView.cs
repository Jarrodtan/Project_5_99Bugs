using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One Display 1 row: "Name: count" text, a trend arrow, and a background that flashes on change.
/// Row look comes from the shared <see cref="StatListStyle"/> (same as the Inspect tab); states and strings from <see cref="PopulationCounterUiConfig"/>; content from <see cref="SpeciesCounterState"/>.
/// </summary>
public class SpeciesCounterRowView : MonoBehaviour
{
    [SerializeField]
    LayoutElement layoutElement;

    [SerializeField]
    Image background;

    [SerializeField]
    TMP_Text countText;

    [SerializeField]
    TMP_Text arrowText;

    PopulationCounterUiConfig config;
    Color restingBackground;
    Color flashColor;
    float flashRemaining;

    /// <summary>
    /// Wires UI references created by the panel bootstrap. Call once after constructing the row hierarchy.
    /// </summary>
    public void Initialize(LayoutElement layout, Image rowBackground, TMP_Text count, TMP_Text arrow)
    {
        layoutElement = layout;
        background = rowBackground;
        countText = count;
        arrowText = arrow;
    }

    /// <summary>
    /// Applies row geometry and text style from the shared <see cref="StatListStyle"/>; the trend arrow
    /// uses the style's trailing column.
    /// </summary>
    /// <param name="uiConfig">UI config asset (required, with a list style assigned).</param>
    public void ApplyStyle(PopulationCounterUiConfig uiConfig)
    {
        config = uiConfig;
        if (config == null || config.listStyle == null)
            return;

        StatListStyling.ApplyRow(config.listStyle, layoutElement, background, countText, arrowText);
        PaintBackground();
    }

    /// <summary>
    /// Paints a species snapshot. Starts a flash when <see cref="SpeciesCounterState.Changed"/> is true.
    /// </summary>
    /// <param name="state">Row data from <see cref="SpeciesPopulationTracker"/>.</param>
    public void Bind(SpeciesCounterState state)
    {
        if (config == null || config.listStyle == null)
            return;

        if (countText != null)
        {
            countText.text = FormatText(state);
            countText.color = state.Status switch
            {
                SpeciesCounterStatus.Extinct => config.extinctTextColor,
                SpeciesCounterStatus.Unavailable => config.unavailableTextColor,
                _ => config.listStyle.textColor
            };
        }

        var trendColor = state.Trend == PopulationTrend.Up ? config.increaseColor : config.decreaseColor;
        if (arrowText != null)
        {
            arrowText.text = state.Trend switch
            {
                PopulationTrend.Up => config.upArrow,
                PopulationTrend.Down => config.downArrow,
                _ => string.Empty
            };
            arrowText.color = trendColor;
        }

        restingBackground = state.Status == SpeciesCounterStatus.Extinct
            ? config.extinctBackgroundColor
            : config.listStyle.rowBackgroundColor;

        if (state.Changed && config.flashDurationSeconds > 0f)
        {
            flashColor = trendColor;
            flashColor.a = config.flashBackgroundAlpha;
            flashRemaining = config.flashDurationSeconds;
        }

        PaintBackground();
    }

    /// <summary>
    /// Shows or hides this row without destroying the pooled instance.
    /// </summary>
    /// <param name="visible">True to activate the row GameObject.</param>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    void Update()
    {
        if (flashRemaining <= 0f)
            return;

        // Unscaled so a flash still fades while the simulation is paused via timeScale.
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
        PaintBackground();
    }

    void PaintBackground()
    {
        if (background == null)
            return;

        if (flashRemaining <= 0f || config == null || config.flashDurationSeconds <= 0f)
        {
            background.color = restingBackground;
            return;
        }

        var t = flashRemaining / config.flashDurationSeconds;
        background.color = Color.Lerp(restingBackground, flashColor, t);
    }

    string FormatText(SpeciesCounterState state)
    {
        return state.Status switch
        {
            SpeciesCounterStatus.Extinct => string.Format(config.extinctFormat, state.DisplayName, state.Count),
            SpeciesCounterStatus.Unavailable => string.Format(config.unavailableFormat, state.DisplayName),
            _ => StatListStyling.FormatLabelValue(config.listStyle, state.DisplayName, state.Count)
        };
    }
}
