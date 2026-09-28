using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prototype island picker for portfolio/T04-adjacent work — not approved Fxx M1 evidence.
/// Builds one button per entry in <see cref="IslandPlatformConfig.islands"/>; no hardcoded labels.
/// </summary>
public class IslandSelectionUI : MonoBehaviour
{
    [Tooltip("Platform hub whose island catalog drives the buttons.")]
    [SerializeField]
    IslandPlatformConfig config;

    [Tooltip("Manager that owns swap. Buttons call RequestSwap only.")]
    [SerializeField]
    IslandManager islandManager;

    [Tooltip("Parent transform for generated buttons (e.g. a Vertical Layout Group).")]
    [SerializeField]
    Transform buttonContainer;

    [Tooltip("Optional button prefab with a child Text. When empty, a runtime button is created.")]
    [SerializeField]
    Button buttonPrefab;

    void Start()
    {
        RebuildButtons();
    }

    void Update()
    {
        if (buttonContainer == null || islandManager == null)
            return;

        var interactable = !islandManager.IsBusy;
        for (var i = 0; i < buttonContainer.childCount; i++)
        {
            var button = buttonContainer.GetChild(i).GetComponent<Button>();
            if (button != null)
                button.interactable = interactable;
        }
    }

    /// <summary>
    /// Clears and rebuilds buttons from the current catalog.
    /// </summary>
    public void RebuildButtons()
    {
        if (config == null || buttonContainer == null || islandManager == null)
        {
            Debug.LogError($"{nameof(IslandSelectionUI)} requires config, buttonContainer, and islandManager.", this);
            return;
        }

        for (var i = buttonContainer.childCount - 1; i >= 0; i--)
        {
            var child = buttonContainer.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        if (config.islands == null)
            return;

        for (var i = 0; i < config.islands.Count; i++)
        {
            var definition = config.islands[i];
            if (definition == null)
                continue;

            var button = CreateButton(definition);
            var captured = definition;
            button.onClick.AddListener(() => islandManager.RequestSwap(captured));
        }
    }

    Button CreateButton(IslandDefinition definition)
    {
        Button button;
        if (buttonPrefab != null)
        {
            button = Instantiate(buttonPrefab, buttonContainer);
        }
        else
        {
            var go = new GameObject(definition.displayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(buttonContainer, false);
            button = go.GetComponent<Button>();

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        button.gameObject.name = definition.displayName;
        var label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = definition.displayName;

        return button;
    }
}
