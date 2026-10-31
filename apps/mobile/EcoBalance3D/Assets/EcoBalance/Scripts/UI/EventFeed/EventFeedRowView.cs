using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One Display 3 feed entry: wrapped event text over a background that highlights when the entry is new.
/// Style and strings come from <see cref="EventFeedUiConfig"/>; content from <see cref="EventFeedEntry"/>.
/// </summary>
public class EventFeedRowView : MonoBehaviour
{
    [SerializeField]
    LayoutElement layoutElement;

    [SerializeField]
    VerticalLayoutGroup rowLayout;

    [SerializeField]
    Image background;

    [SerializeField]
    TMP_Text text;

    EventFeedUiConfig config;
    float seenAt = float.NegativeInfinity;

    /// <summary>
    /// Wires UI references created by the panel bootstrap. Call once after constructing the row hierarchy.
    /// </summary>
    public void Initialize(LayoutElement layout, VerticalLayoutGroup layoutGroup, Image rowBackground, TMP_Text entryText)
    {
        layoutElement = layout;
        rowLayout = layoutGroup;
        background = rowBackground;
        text = entryText;
    }

    /// <summary>
    /// Applies fonts, height, and insets from the shared UI config.
    /// </summary>
    /// <param name="uiConfig">UI config asset (required).</param>
    public void ApplyStyle(EventFeedUiConfig uiConfig)
    {
        config = uiConfig;
        if (config == null)
            return;

        if (layoutElement != null)
            layoutElement.minHeight = config.rowMinHeight;

        if (rowLayout != null)
        {
            var inset = Mathf.RoundToInt(config.rowTextInset);
            rowLayout.padding = new RectOffset(inset, inset, 0, 0);
        }

        if (text != null)
        {
            text.fontSize = config.rowFontSize;
            if (config.font != null)
                text.font = config.font;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        PaintBackground();
    }

    /// <summary>
    /// Paints one feed entry.
    /// </summary>
    /// <param name="entry">Row data from <see cref="EventFeedReader"/>.</param>
    /// <param name="alpha">Text alpha multiplier (older entries fade).</param>
    public void Bind(EventFeedEntry entry, float alpha)
    {
        if (config == null)
            return;

        if (text != null)
        {
            text.text = string.Format(config.entryFormat ?? "{0}", entry.Text);
            var color = config.textColor;
            color.a *= alpha;
            text.color = color;
        }

        seenAt = entry.SeenAt;
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
        if (config == null || !IsHighlighting())
            return;

        PaintBackground();
    }

    bool IsHighlighting()
    {
        return config.highlightDurationSeconds > 0f
               && Time.unscaledTime - seenAt < config.highlightDurationSeconds + Time.unscaledDeltaTime;
    }

    void PaintBackground()
    {
        if (background == null || config == null)
            return;

        if (config.highlightDurationSeconds <= 0f || float.IsNegativeInfinity(seenAt))
        {
            background.color = config.rowBackgroundColor;
            return;
        }

        // Unscaled so the highlight still fades while the simulation is paused via timeScale.
        var t = Mathf.Clamp01((Time.unscaledTime - seenAt) / config.highlightDurationSeconds);
        background.color = Color.Lerp(config.newEntryHighlightColor, config.rowBackgroundColor, t);
    }
}
