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

    /// <summary>Shows <paramref name="sprite"/> when assigned; hides the icon image otherwise.</summary>
    public void ApplyIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    /// <summary>Sets this bubble's rect to a square of <paramref name="diameter"/> pixels.</summary>
    public void ApplyDiameter(float diameter)
    {
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(diameter, diameter);
    }
}
