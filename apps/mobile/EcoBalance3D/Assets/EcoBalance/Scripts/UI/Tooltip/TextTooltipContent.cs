using TMPro;
using UnityEngine;

/// <summary>
/// Text body for <see cref="TooltipHost"/>. Creates or reuses a wrapping TMP label under the frame body.
/// </summary>
public sealed class TextTooltipContent : ITooltipContent
{
    const string LabelName = "TextBody";

    readonly string text;
    readonly TooltipStyle style;

    /// <summary>
    /// Builds a text body from <paramref name="text"/> styled by <paramref name="style"/>.
    /// </summary>
    /// <param name="text">Caption to show. Empty strings are still applied; the host decides whether to show.</param>
    /// <param name="style">Shared tooltip knobs.</param>
    public TextTooltipContent(string text, TooltipStyle style)
    {
        this.text = text ?? string.Empty;
        this.style = style;
    }

    /// <inheritdoc />
    public void Apply(RectTransform body)
    {
        if (body == null || style == null)
            return;

        for (var i = 0; i < body.childCount; i++)
        {
            var child = body.GetChild(i);
            child.gameObject.SetActive(child.name == LabelName);
        }

        var label = FindOrCreateLabel(body);
        label.gameObject.SetActive(true);
        label.text = text;
        label.fontSize = style.fontSize;
        label.color = style.textColor;
        if (style.font != null)
            label.font = style.font;
        else if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.enableAutoSizing = false;

        var preferred = label.GetPreferredValues(text, style.maxWidth, float.PositiveInfinity);
        var width = Mathf.Clamp(preferred.x, 8f, style.maxWidth);
        var height = Mathf.Max(style.fontSize, preferred.y);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(width, height);

        body.sizeDelta = new Vector2(width, height);
    }

    static TextMeshProUGUI FindOrCreateLabel(RectTransform body)
    {
        var existing = body.Find(LabelName);
        if (existing != null)
        {
            var label = existing.GetComponent<TextMeshProUGUI>();
            if (label != null)
                return label;
        }

        var go = new GameObject(LabelName, typeof(RectTransform));
        go.transform.SetParent(body, false);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
