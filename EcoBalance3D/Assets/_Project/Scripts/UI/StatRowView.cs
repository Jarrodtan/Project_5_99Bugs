using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One inspector row: label + value. Style comes from <see cref="InspectionUiConfig"/>; content from <see cref="StatEntry"/>.
/// </summary>
public class StatRowView : MonoBehaviour
{
    [SerializeField]
    RectTransform root;

    [SerializeField]
    LayoutElement layoutElement;

    [SerializeField]
    TMP_Text labelText;

    [SerializeField]
    TMP_Text valueText;

    [SerializeField]
    RectTransform labelRect;

    [SerializeField]
    RectTransform valueRect;

    /// <summary>
    /// Wires UI references created by the panel bootstrap. Call once after constructing the row hierarchy.
    /// </summary>
    public void Initialize(
        RectTransform rowRoot,
        LayoutElement layout,
        TMP_Text label,
        TMP_Text value,
        RectTransform labelTransform,
        RectTransform valueTransform)
    {
        root = rowRoot;
        layoutElement = layout;
        labelText = label;
        valueText = value;
        labelRect = labelTransform;
        valueRect = valueTransform;
    }

    /// <summary>
    /// Applies fonts, colors, height, and column split from the shared UI config.
    /// </summary>
    /// <param name="config">UI config asset (required).</param>
    public void ApplyStyle(InspectionUiConfig config)
    {
        if (config == null)
            return;

        if (layoutElement != null)
        {
            layoutElement.preferredHeight = config.rowHeight;
            layoutElement.minHeight = config.rowHeight;
        }

        ApplyTextStyle(labelText, config, config.labelColor);
        ApplyTextStyle(valueText, config, config.valueColor);

        var ratio = Mathf.Clamp01(config.labelColumnWidthRatio);
        if (labelRect != null)
        {
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(ratio, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        if (valueRect != null)
        {
            valueRect.anchorMin = new Vector2(ratio, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Copies label/value from a <see cref="StatEntry"/>. Does not invent strings.
    /// </summary>
    /// <param name="entry">Row data from <see cref="IInspectable"/>.</param>
    public void Bind(StatEntry entry)
    {
        if (labelText != null)
            labelText.text = entry.Label;
        if (valueText != null)
            valueText.text = entry.Value;
    }

    /// <summary>
    /// Shows or hides this row without destroying the pooled instance.
    /// </summary>
    /// <param name="visible">True to activate the row GameObject.</param>
    public void SetVisible(bool visible)
    {
        if (root != null)
            root.gameObject.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    static void ApplyTextStyle(TMP_Text text, InspectionUiConfig config, Color color)
    {
        if (text == null)
            return;

        text.fontSize = config.rowFontSize;
        text.color = color;
        if (config.font != null)
            text.font = config.font;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }
}
