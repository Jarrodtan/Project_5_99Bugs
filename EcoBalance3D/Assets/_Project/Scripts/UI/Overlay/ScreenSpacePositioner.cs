using UnityEngine;

/// <summary>
/// Places the panel in Screen Space Overlay via <see cref="Camera.WorldToScreenPoint"/>.
/// Hides when the anchor is behind the camera (screen z &lt; 0). All offsets/clamp from config.
/// </summary>
public sealed class ScreenSpacePositioner : IOverlayPositioner
{
    OverlayPositionContext context;
    Transform target;

    /// <inheritdoc />
    public OverlayMode Mode => OverlayMode.ScreenSpace;

    /// <inheritdoc />
    public void Bind(OverlayPositionContext positionContext)
    {
        context = positionContext;
        if (context == null || context.PanelRoot == null || context.ScreenCanvas == null)
            return;

        context.WorldCanvas.gameObject.SetActive(false);
        context.ScreenCanvas.gameObject.SetActive(true);

        var panel = context.PanelRoot;
        panel.SetParent(context.ScreenCanvas.transform, false);
        panel.localRotation = Quaternion.identity;
        panel.localScale = Vector3.one;
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = context.Config.panelSize;
    }

    /// <inheritdoc />
    public void SetTarget(Transform worldAnchor) => target = worldAnchor;

    /// <inheritdoc />
    public void ClearTarget() => target = null;

    /// <inheritdoc />
    public bool UpdatePosition()
    {
        if (context == null || context.Config == null || context.PanelRoot == null || target == null)
            return false;

        var camera = context.ResolveCamera?.Invoke();
        if (camera == null)
            return false;

        var config = context.Config;
        var worldPoint = target.position + config.worldAnchorOffset;
        var screenPos = camera.WorldToScreenPoint(worldPoint);
        if (screenPos.z < 0f)
            return false;

        screenPos.x += config.screenPixelOffset.x;
        screenPos.y += config.screenPixelOffset.y;

        if (config.clampToScreen)
            screenPos = ClampToScreen(screenPos, context.PanelRoot.sizeDelta, config.screenClampPadding);

        context.PanelRoot.position = screenPos;
        return true;
    }

    static Vector3 ClampToScreen(Vector3 screenPos, Vector2 panelSize, Vector2 padding)
    {
        var halfWidth = panelSize.x * 0.5f;
        var height = panelSize.y;
        var minX = padding.x + halfWidth;
        var maxX = Screen.width - padding.x - halfWidth;
        var minY = padding.y;
        var maxY = Screen.height - padding.y - height;

        if (maxX < minX)
            screenPos.x = Screen.width * 0.5f;
        else
            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);

        if (maxY < minY)
            screenPos.y = Screen.height * 0.5f;
        else
            screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

        return screenPos;
    }
}
