using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic "Label: value" row for any <see cref="StatEntry"/> list, styled by <see cref="StatListStyle"/>
/// through <see cref="StatListStyling"/> — the same row geometry and text style as the Population tab.
/// </summary>
public class LabeledStatRowView : MonoBehaviour
{
    [SerializeField]
    LayoutElement layoutElement;

    [SerializeField]
    Image background;

    [SerializeField]
    TMP_Text text;

    StatListStyle style;

    /// <summary>
    /// Builds a row under <paramref name="parent"/>: background image, layout element, and one text child.
    /// </summary>
    /// <param name="parent">Rows container (typically driven by a VerticalLayoutGroup).</param>
    /// <param name="listStyle">Style to apply.</param>
    /// <returns>The new row view.</returns>
    public static LabeledStatRowView Create(Transform parent, StatListStyle listStyle)
    {
        var rowGo = new GameObject("StatRow");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<RectTransform>();
        var layout = rowGo.AddComponent<LayoutElement>();
        var rowBackground = rowGo.AddComponent<Image>();
        rowBackground.raycastTarget = false;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(rowGo.transform, false);
        textGo.AddComponent<RectTransform>();
        var rowText = textGo.AddComponent<TextMeshProUGUI>();

        var view = rowGo.AddComponent<LabeledStatRowView>();
        view.layoutElement = layout;
        view.background = rowBackground;
        view.text = rowText;
        view.ApplyStyle(listStyle);
        return view;
    }

    /// <summary>
    /// Applies row geometry and text style.
    /// </summary>
    /// <param name="listStyle">Shared stat list style.</param>
    public void ApplyStyle(StatListStyle listStyle)
    {
        style = listStyle;
        if (style != null)
            StatListStyling.ApplyRow(style, layoutElement, background, text);
    }

    /// <summary>
    /// Paints one stat as "Label: value".
    /// </summary>
    /// <param name="entry">Stat from <see cref="IInspectable.GetStats"/> (or any other source).</param>
    public void Bind(StatEntry entry)
    {
        if (style != null && text != null)
            text.text = StatListStyling.FormatLabelValue(style, entry.Label, entry.Value);
    }

    /// <summary>
    /// Shows or hides this row without destroying the pooled instance.
    /// </summary>
    /// <param name="visible">True to activate the row GameObject.</param>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
