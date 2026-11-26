using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a <see cref="StatListStyle"/> to panel, title, and row UI. Every labeled-stat list goes through
/// these methods, so geometry, fonts, and colors can't drift between panels.
/// </summary>
public static class StatListStyling
{
    /// <summary>
    /// Formats one row as "Label: value" using the style's format string.
    /// </summary>
    public static string FormatLabelValue(StatListStyle style, string label, object value)
    {
        return string.Format(style.labelValueFormat ?? "{0}: {1}", label, value);
    }

    /// <summary>
    /// Panel background plus layout-group padding and spacing (for panels built on a VerticalLayoutGroup).
    /// </summary>
    public static void ApplyPanel(StatListStyle style, Image background, VerticalLayoutGroup layout)
    {
        if (background != null)
            background.color = style.backgroundColor;

        if (layout != null)
        {
            var pad = style.panelPadding;
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = style.rowSpacing;
        }
    }

    /// <summary>
    /// Heading text style. When <paramref name="layout"/> is given, also sets its preferred height.
    /// </summary>
    public static void ApplyTitle(StatListStyle style, TMP_Text title, LayoutElement layout = null)
    {
        ApplyText(style, title, style.titleFontSize, style.titleColor, TextAlignmentOptions.MidlineLeft);
        if (layout != null)
            layout.preferredHeight = TitleHeight(style);
    }

    /// <summary>
    /// Row geometry and text: fixed height, background, main text inset on the left, and an optional
    /// trailing indicator column on the right (reserved even when <paramref name="trailing"/> is null).
    /// </summary>
    public static void ApplyRow(StatListStyle style, LayoutElement layout, Image background, TMP_Text text, TMP_Text trailing = null)
    {
        if (layout != null)
        {
            layout.preferredHeight = style.rowHeight;
            layout.minHeight = style.rowHeight;
        }

        if (background != null)
            background.color = style.rowBackgroundColor;

        if (text != null)
        {
            ApplyText(style, text, style.rowFontSize, style.textColor, TextAlignmentOptions.MidlineLeft);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(style.rowTextInset, 0f);
            rect.offsetMax = new Vector2(-style.rowTextInset - style.trailingColumnWidth, 0f);
        }

        if (trailing != null)
        {
            ApplyText(style, trailing, style.rowFontSize, style.textColor, TextAlignmentOptions.Center);
            var rect = trailing.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-style.rowTextInset - style.trailingColumnWidth, 0f);
            rect.offsetMax = new Vector2(-style.rowTextInset, 0f);
        }
    }

    /// <summary>
    /// Font, size, color, alignment, and single-line ellipsis for any text in a stat list.
    /// </summary>
    public static void ApplyText(StatListStyle style, TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.fontSize = fontSize;
        text.color = color;
        if (style.font != null)
            text.font = style.font;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    /// <summary>Height reserved for the heading (pixels).</summary>
    public static float TitleHeight(StatListStyle style) => style.titleFontSize + style.rowSpacing;

    /// <summary>
    /// Total panel height for a title plus <paramref name="rowCount"/> rows, matching a
    /// VerticalLayoutGroup panel styled by <see cref="ApplyPanel"/>.
    /// </summary>
    public static float PanelHeight(StatListStyle style, int rowCount, bool hasTitle = true)
    {
        var children = rowCount + (hasTitle ? 1 : 0);
        var content = (hasTitle ? TitleHeight(style) : 0f) + rowCount * style.rowHeight;
        return style.panelPadding * 2f + content + Mathf.Max(0, children - 1) * style.rowSpacing;
    }
}
