using System;
using UnityEngine;

/// <summary>
/// Shared wiring bag for overlay positioners. Keeps modules decoupled from MonoBehaviour fields.
/// </summary>
public sealed class OverlayPositionContext
{
    /// <summary>Central UI + overlay knobs.</summary>
    public InspectionUiConfig Config { get; }

    /// <summary>Content root that is reparented between canvases.</summary>
    public RectTransform PanelRoot { get; }

    /// <summary>Screen Space Overlay canvas.</summary>
    public Canvas ScreenCanvas { get; }

    /// <summary>World Space canvas.</summary>
    public Canvas WorldCanvas { get; }

    /// <summary>Resolves the camera used for projection and billboarding.</summary>
    public Func<Camera> ResolveCamera { get; }

    /// <summary>
    /// Creates a context for positioner modules.
    /// </summary>
    public OverlayPositionContext(
        InspectionUiConfig config,
        RectTransform panelRoot,
        Canvas screenCanvas,
        Canvas worldCanvas,
        Func<Camera> resolveCamera)
    {
        Config = config;
        PanelRoot = panelRoot;
        ScreenCanvas = screenCanvas;
        WorldCanvas = worldCanvas;
        ResolveCamera = resolveCamera;
    }
}
