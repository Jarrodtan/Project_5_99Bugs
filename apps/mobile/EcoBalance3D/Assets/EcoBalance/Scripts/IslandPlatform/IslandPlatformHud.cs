using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Sample Scene readouts that Island Platform did not place by hand:
/// the Himalaya weather line and the animal card (hunger, action, target, fact).
/// The population tabs and the debug overlay are separate scene objects.
/// </summary>
[DefaultExecutionOrder(-50)]
public class IslandPlatformHud : MonoBehaviour
{
    [SerializeField]
    WeatherManager weather;

    [SerializeField]
    AnimalInfoUI animalInfo;

    void Awake()
    {
        if (weather != null && weather.txt == null)
            weather.txt = CreateWeatherLabel();

        if (animalInfo != null && animalInfo.panel == null)
            BuildAnimalCard(animalInfo);
    }

    static TextMeshProUGUI CreateWeatherLabel()
    {
        var canvas = CreateOverlay("Weather Canvas", 30);
        var rect = CreateRect("Weather", canvas.transform);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 56f);
        rect.sizeDelta = new Vector2(980f, 120f);

        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.fontSize = 40f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.text = "Himalayas Live Temp:\nCondition:";
        return text;
    }

    static void BuildAnimalCard(AnimalInfoUI info)
    {
        var canvas = CreateOverlay("Animal Info Canvas", 40);
        var card = CreateRect("AnimalInfoUI", canvas.transform);
        card.anchorMin = new Vector2(0f, 0.5f);
        card.anchorMax = new Vector2(0f, 0.5f);
        card.pivot = new Vector2(0f, 0.5f);
        card.anchoredPosition = new Vector2(340f, -20f);
        card.sizeDelta = new Vector2(360f, 340f);

        var background = card.gameObject.AddComponent<Image>();
        background.color = new Color(0.82f, 0.82f, 0.82f, 0.96f);

        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var ink = new Color(0.38f, 0.38f, 0.38f, 1f);
        info.panel = card.gameObject;
        info.nameText = AddLine(card, "NameText", 32f, FontStyles.Normal, ink, 40f);
        info.hungerText = AddLine(card, "HungerText", 26f, FontStyles.Normal, ink, 34f);
        info.actionText = AddLine(card, "ActionText", 26f, FontStyles.Normal, ink, 34f);
        info.targetText = AddLine(card, "TargetText", 26f, FontStyles.Normal, ink, 34f);
        info.factText = AddLine(card, "DidYouKnow", 24f, FontStyles.Normal, ink, 96f);
        card.gameObject.SetActive(false);
    }

    static TMP_Text AddLine(RectTransform parent, string name, float size, FontStyles style, Color color, float minHeight)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = minHeight;
        element.preferredHeight = minHeight;
        return text;
    }

    static Canvas CreateOverlay(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void ApplyFont(TMP_Text text)
    {
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
    }
}
