using UnityEngine;

/// <summary>
/// Places a World Space canvas at the selected object, billboards to the camera, and applies
/// scale/sorting from <see cref="InspectionUiConfig"/>.
/// </summary>
public sealed class WorldSpacePositioner : IOverlayPositioner
{
    OverlayPositionContext context;
    Transform target;

    /// <inheritdoc />
    public OverlayMode Mode => OverlayMode.WorldSpace;

    /// <inheritdoc />
    public void Bind(OverlayPositionContext positionContext)
    {
        context = positionContext;
        if (context == null || context.PanelRoot == null || context.WorldCanvas == null)
            return;

        context.ScreenCanvas.gameObject.SetActive(false);
        context.WorldCanvas.gameObject.SetActive(true);

        var config = context.Config;
        var worldCanvas = context.WorldCanvas;
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = config.worldSpaceSortingOrder;
        if (!string.IsNullOrEmpty(config.worldSpaceSortingLayerName))
            worldCanvas.sortingLayerName = config.worldSpaceSortingLayerName;

        var camera = context.ResolveCamera?.Invoke();
        if (camera != null)
            worldCanvas.worldCamera = camera;

        var scale = config.worldSpaceScale;
        worldCanvas.transform.localScale = new Vector3(scale, scale, scale);

        var panel = context.PanelRoot;
        panel.SetParent(worldCanvas.transform, false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = config.panelSize;
        panel.anchoredPosition = Vector2.zero;
        panel.localRotation = Quaternion.identity;
        panel.localScale = Vector3.one;

        var rect = worldCanvas.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = config.panelSize;
    }

    /// <inheritdoc />
    public void SetTarget(Transform worldAnchor) => target = worldAnchor;

    /// <inheritdoc />
    public void ClearTarget() => target = null;

    /// <inheritdoc />
    public bool UpdatePosition()
    {
        if (context == null || context.Config == null || context.WorldCanvas == null || target == null)
            return false;

        var camera = context.ResolveCamera?.Invoke();
        if (camera == null)
            return false;

        var config = context.Config;
        var canvasTransform = context.WorldCanvas.transform;
        canvasTransform.position = target.position + config.worldAnchorOffset;
        canvasTransform.rotation = camera.transform.rotation;

        var scale = config.worldSpaceScale;
        canvasTransform.localScale = new Vector3(scale, scale, scale);
        context.WorldCanvas.sortingOrder = config.worldSpaceSortingOrder;
        if (!string.IsNullOrEmpty(config.worldSpaceSortingLayerName))
            context.WorldCanvas.sortingLayerName = config.worldSpaceSortingLayerName;

        context.WorldCanvas.worldCamera = camera;
        return true;
    }
}
