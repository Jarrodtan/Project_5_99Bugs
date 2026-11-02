using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives a placeholder button (Unity's plain white square: no text and no icon) a text label, so Back,
/// Previous and Next read as buttons until the designers drop in icons. A button that already has a label
/// or a sprite of its own is left alone, so adding the real icon in the scene switches this off.
/// </summary>
public static class ButtonCaption
{
    static readonly Color Ink = new(0.12f, 0.16f, 0.2f);

    /// <returns>The new label, or null when the button already had one or has an icon.</returns>
    public static TextMeshProUGUI Ensure(Button button, string text, float size = 30f)
    {
        if (button == null) return null;
        var existing = button.transform.Find("Caption");
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
        if (button.GetComponentInChildren<TMP_Text>(true) != null || button.GetComponentInChildren<Text>(true) != null) return null;
        if (button.TryGetComponent(out Image image) && image.sprite != null && !IsBuiltIn(image.sprite)) return null;

        var label = new GameObject("Caption", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(button.transform, false);
        var rt = label.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6f, 4f); rt.offsetMax = new Vector2(-6f, -4f);
        label.text = text;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = size * 0.5f;
        return label;
    }

    // Unity's default UI sprites: what a freshly made button gets before anyone assigns art.
    static bool IsBuiltIn(Sprite s) => s.name == "UISprite" || s.name == "Background" || s.name == "InputFieldBackground";
}
