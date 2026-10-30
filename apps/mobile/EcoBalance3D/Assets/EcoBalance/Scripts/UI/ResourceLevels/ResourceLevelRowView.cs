using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One Display 2 row: resource name, numeric level, a level bar, and a pulsing warning icon when critical.
/// Style and strings come from <see cref="ResourceLevelUiConfig"/>; content from <see cref="ResourceLevelState"/>.
/// </summary>
public class ResourceLevelRowView : MonoBehaviour
{
    [SerializeField]
    LayoutElement layoutElement;

    [SerializeField]
    TMP_Text nameText;

    [SerializeField]
    TMP_Text valueText;

    [SerializeField]
    TMP_Text warningText;

    [SerializeField]
    Image barBackground;

    [SerializeField]
    Image barFill;

    ResourceLevelUiConfig config;
    ResourceLevelStatus status;
    float targetFraction;
    float shownFraction;
    bool hasShownValue;

    /// <summary>
    /// Wires UI references created by the panel bootstrap. Call once after constructing the row hierarchy.
    /// </summary>
    public void Initialize(
        LayoutElement layout,
        TMP_Text resourceName,
        TMP_Text value,
        TMP_Text warning,
        Image barBack,
        Image barFront)
    {
        layoutElement = layout;
        nameText = resourceName;
        valueText = value;
        warningText = warning;
        barBackground = barBack;
        barFill = barFront;
    }

    /// <summary>
    /// Applies fonts, sizes, and column layout from the shared UI config.
    /// </summary>
    /// <param name="uiConfig">UI config asset (required).</param>
    public void ApplyStyle(ResourceLevelUiConfig uiConfig)
    {
        config = uiConfig;
        if (config == null)
            return;

        var rowHeight = config.textLineHeight + config.barGap + config.barHeight;
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = rowHeight;
            layoutElement.minHeight = rowHeight;
        }

        ApplyTextStyle(nameText, TextAlignmentOptions.MidlineLeft);
        ApplyTextStyle(valueText, TextAlignmentOptions.MidlineRight);
        ApplyTextStyle(warningText, TextAlignmentOptions.Center);
        if (warningText != null)
            warningText.fontStyle = FontStyles.Bold;

        var ratio = Mathf.Clamp01(config.labelColumnWidthRatio);
        PlaceInTextLine(nameText, 0f, ratio, 0f, 0f);
        PlaceInTextLine(valueText, ratio, 1f, 0f, -config.iconColumnWidth);
        PlaceInTextLine(warningText, 1f, 1f, -config.iconColumnWidth, 0f);

        if (barBackground != null)
        {
            var rect = barBackground.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, config.barHeight);
            barBackground.color = config.barBackgroundColor;
        }

        if (barFill != null)
        {
            var rect = barFill.rectTransform;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            PaintFill();
        }
    }

    /// <summary>
    /// Paints a resource snapshot. The bar slides toward the new level over time.
    /// </summary>
    /// <param name="state">Row data from <see cref="ResourceLevelReader"/>.</param>
    public void Bind(ResourceLevelState state)
    {
        if (config == null)
            return;

        status = state.Status;
        targetFraction = state.Fraction;
        if (!hasShownValue || config.barFillLerpSpeed <= 0f)
            shownFraction = targetFraction;
        hasShownValue = status != ResourceLevelStatus.Unavailable;

        if (nameText != null)
            nameText.text = state.DisplayName;

        if (valueText != null)
        {
            valueText.text = status == ResourceLevelStatus.Unavailable
                ? config.unavailableText
                : string.Format(config.valueFormat, state.Level, state.MaxLevel, state.Fraction * 100f);
        }

        var critical = status == ResourceLevelStatus.Critical;
        var unavailable = status == ResourceLevelStatus.Unavailable;
        if (nameText != null)
            nameText.color = unavailable ? config.unavailableColor : critical ? config.criticalColor : config.labelColor;
        if (valueText != null)
            valueText.color = unavailable ? config.unavailableColor : critical ? config.criticalColor : config.valueColor;
        if (warningText != null)
            warningText.text = critical ? config.warningGlyph : string.Empty;

        PaintFill();
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
        if (config == null)
            return;

        // Unscaled so the bar and warning keep animating while the simulation is paused via timeScale.
        if (!Mathf.Approximately(shownFraction, targetFraction))
        {
            var step = 1f - Mathf.Exp(-config.barFillLerpSpeed * Time.unscaledDeltaTime);
            shownFraction = Mathf.Lerp(shownFraction, targetFraction, step);
            if (Mathf.Abs(shownFraction - targetFraction) < 0.001f)
                shownFraction = targetFraction;
        }

        PaintFill();
    }

    void PaintFill()
    {
        if (config == null)
            return;

        var critical = status == ResourceLevelStatus.Critical;
        var alpha = critical ? PulseAlpha() : 1f;

        if (barFill != null)
        {
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(shownFraction), 1f);
            var fill = critical ? config.criticalColor : config.barFillColor;
            fill.a *= alpha;
            barFill.color = fill;
        }

        if (warningText != null)
        {
            var icon = config.criticalColor;
            icon.a *= alpha;
            warningText.color = icon;
        }
    }

    float PulseAlpha()
    {
        if (config.criticalPulsesPerSecond <= 0f)
            return 1f;

        var wave = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * config.criticalPulsesPerSecond * 2f * Mathf.PI);
        return Mathf.Lerp(config.criticalPulseMinAlpha, 1f, wave);
    }

    void PlaceInTextLine(TMP_Text text, float anchorMinX, float anchorMaxX, float leftOffset, float rightOffset)
    {
        if (text == null)
            return;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(anchorMinX, 1f);
        rect.anchorMax = new Vector2(anchorMaxX, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(leftOffset, -config.textLineHeight);
        rect.offsetMax = new Vector2(rightOffset, 0f);
    }

    void ApplyTextStyle(TMP_Text text, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.fontSize = config.rowFontSize;
        if (config.font != null)
            text.font = config.font;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }
}
