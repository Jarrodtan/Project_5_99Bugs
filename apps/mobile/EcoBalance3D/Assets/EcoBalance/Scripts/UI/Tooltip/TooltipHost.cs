using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One screen-space overlay tooltip for the whole app. Built at runtime; never raycasts.
/// </summary>
public class TooltipHost : MonoBehaviour
{
    static TooltipHost instance;
    static Sprite whiteSprite;

    TooltipStyle style;
    Canvas canvas;
    RectTransform frame;
    Image background;
    RectTransform body;
    RectTransform anchor;
    object owner;
    bool shown;

    /// <summary>Active host, or null before <see cref="Ensure"/>.</summary>
    public static TooltipHost Instance => instance;

    /// <summary>Style last passed to <see cref="Ensure"/>.</summary>
    public TooltipStyle Style => style;

    /// <summary>
    /// Finds or creates the shared host and applies <paramref name="tooltipStyle"/>.
    /// </summary>
    /// <param name="tooltipStyle">Look and timing knobs. Required.</param>
    /// <returns>The shared host.</returns>
    public static TooltipHost Ensure(TooltipStyle tooltipStyle)
    {
        if (tooltipStyle == null)
        {
            Debug.LogError($"{nameof(TooltipHost)}.{nameof(Ensure)} requires a {nameof(TooltipStyle)}.");
            return instance;
        }

        if (instance == null)
        {
            var existing = FindAnyObjectByType<TooltipHost>();
            if (existing != null)
                instance = existing;
            else
            {
                var go = new GameObject(nameof(TooltipHost));
                DontDestroyOnLoad(go);
                instance = go.AddComponent<TooltipHost>();
            }
        }

        instance.ApplyStyle(tooltipStyle);
        return instance;
    }

    /// <summary>
    /// Shows content beside <paramref name="target"/>, owned by <paramref name="contentOwner"/>.
    /// A new owner replaces the previous tooltip.
    /// </summary>
    /// <param name="target">UI rect to follow.</param>
    /// <param name="content">Body to place in the frame.</param>
    /// <param name="contentOwner">Trigger or other owner used by <see cref="Hide"/>.</param>
    public void Show(RectTransform target, ITooltipContent content, object contentOwner)
    {
        if (style == null || target == null || content == null || contentOwner == null)
            return;

        EnsureBuilt();
        content.Apply(body);

        var pad = style.padding;
        var contentSize = body.sizeDelta;
        frame.sizeDelta = new Vector2(contentSize.x + pad * 2f, contentSize.y + pad * 2f);
        body.anchoredPosition = new Vector2(pad, -pad);

        background.sprite = WhiteSprite();
        background.type = Image.Type.Simple;
        background.color = style.backgroundColor;
        background.raycastTarget = false;

        anchor = target;
        owner = contentOwner;
        shown = true;
        if (!frame.gameObject.activeSelf)
            frame.gameObject.SetActive(true);
        canvas.enabled = true;
        UpdatePosition();
    }

    /// <summary>
    /// Hides the tooltip when <paramref name="contentOwner"/> still owns it.
    /// </summary>
    /// <param name="contentOwner">Owner that called <see cref="Show"/>.</param>
    public void Hide(object contentOwner)
    {
        if (contentOwner == null || !ReferenceEquals(owner, contentOwner))
            return;

        Clear();
    }

    /// <summary>Hides regardless of owner.</summary>
    public void HideImmediate()
    {
        Clear();
    }

    void LateUpdate()
    {
        if (!shown)
            return;

        if (anchor == null || !anchor.gameObject.activeInHierarchy)
        {
            Clear();
            return;
        }

        UpdatePosition();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void ApplyStyle(TooltipStyle tooltipStyle)
    {
        style = tooltipStyle;
        EnsureBuilt();
        if (canvas != null)
            canvas.sortingOrder = style.canvasSortOrder;
        if (background != null)
            background.color = style.backgroundColor;
    }

    void EnsureBuilt()
    {
        if (frame != null)
            return;

        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = style != null ? style.canvasSortOrder : 200;
        if (gameObject.GetComponent<CanvasScaler>() == null)
            gameObject.AddComponent<CanvasScaler>();

        frame = CreateRect("TooltipFrame", transform);
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        background = frame.gameObject.AddComponent<Image>();
        background.sprite = WhiteSprite();
        background.raycastTarget = false;
        background.color = style != null ? style.backgroundColor : new Color(0.08f, 0.1f, 0.12f, 0.92f);

        body = CreateRect("Body", frame);
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(0f, 1f);
        body.pivot = new Vector2(0f, 1f);
        body.anchoredPosition = Vector2.zero;

        frame.gameObject.SetActive(false);
    }

    void UpdatePosition()
    {
        if (style == null || frame == null || anchor == null)
            return;

        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[2];
        for (var i = 1; i < 4; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        var target = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        var size = frame.sizeDelta;
        var center = TooltipPlacement.ComputeScreenCenter(
            target,
            size,
            style.gap,
            new Vector2(Screen.width, Screen.height),
            style.screenPadding,
            out _);
        frame.position = new Vector3(center.x, center.y, 0f);
    }

    void Clear()
    {
        shown = false;
        owner = null;
        anchor = null;
        if (frame != null)
            frame.gameObject.SetActive(false);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        return whiteSprite;
    }
}
