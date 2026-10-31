using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A category or option bubble already placed in the hierarchy. The shell labels and sizes it.
/// Prototype UI — not approved Feature Plan work.
/// </summary>
public class ActionMenuBubble : MonoBehaviour
{
    [Tooltip("Matches a hub category id or an option id.")]
    [SerializeField]
    string bindingId = string.Empty;

    [Tooltip("Category bubble or option bubble.")]
    [SerializeField]
    ActionMenuBubbleRole role;

    [Tooltip("Label text on this bubble.")]
    [SerializeField]
    Text label;

    [Tooltip("Prototype-scope dot.")]
    [SerializeField]
    Image dot;

    [Tooltip("Optional icon image.")]
    [SerializeField]
    Image iconImage;

    [Tooltip("Click target.")]
    [SerializeField]
    Button button;

    /// <summary>Hub or option id this bubble is bound to.</summary>
    public string BindingId => bindingId;

    /// <summary>Category or option.</summary>
    public ActionMenuBubbleRole Role => role;

    /// <summary>Button on this bubble.</summary>
    public Button Button => button;

    /// <summary>Sets the visible label.</summary>
    public void ApplyLabel(string text)
    {
        if (label != null)
            label.text = text ?? string.Empty;
    }

    /// <summary>Hides the prototype dot. Color and size stay on the hub for a later pass.</summary>
    public void ApplyDot(Color color, float size)
    {
        if (dot == null)
            return;

        dot.enabled = false;
    }

    /// <summary>Fills the bubble with <paramref name="sprite"/> and hides the label. Hides the icon when the sprite is empty.</summary>
    public void ApplyIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;

        if (sprite == null)
        {
            if (label != null)
                label.enabled = true;
            return;
        }

        var iconRect = iconImage.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        if (label != null)
            label.enabled = false;

        var mask = GetComponent<Mask>();
        if (mask == null)
            mask = gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
    }

    /// <summary>Sets this bubble's rect to a square of <paramref name="diameter"/> pixels.</summary>
    public void ApplyDiameter(float diameter)
    {
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(diameter, diameter);
    }

    /// <summary>
    /// Sets the shared tooltip caption for this bubble. Empty text disables the tooltip.
    /// </summary>
    /// <param name="tooltipText">Category or option label shown on hover / long-press.</param>
    /// <param name="tooltipStyle">Shared look and timing for the host.</param>
    public void SetTooltip(string tooltipText, TooltipStyle tooltipStyle)
    {
        var trigger = GetComponent<TooltipTrigger>();
        if (string.IsNullOrEmpty(tooltipText) || tooltipStyle == null)
        {
            if (trigger != null)
                trigger.Text = string.Empty;
            return;
        }

        if (trigger == null)
            trigger = gameObject.AddComponent<TooltipTrigger>();
        trigger.Configure(tooltipText, tooltipStyle);
    }
}
