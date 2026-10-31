using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sole camera hub. Input bindings, rotation/zoom clamps, damping, defaults, and UI-drag
/// gating live here — never hardcode them in <see cref="OrbitCameraController"/>.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Camera Config", fileName = "CameraConfig")]
public class CameraConfig : ScriptableObject
{
    [Header("Input")]
    [Tooltip("Button held to rotate the camera (e.g. Player/CameraDrag).")]
    public InputActionReference dragAction;

    [Tooltip("Pointer delta while dragging (e.g. Player/CameraDragDelta).")]
    public InputActionReference dragDeltaAction;

    [Tooltip("Scroll/zoom input (e.g. Player/CameraZoom).")]
    public InputActionReference zoomAction;

    [Tooltip("XZ pan stick (e.g. Player/CameraPan). WASD / arrows / gamepad.")]
    public InputActionReference panAction;

    [Header("Rotation")]
    [Tooltip("Degrees of yaw/pitch applied per unit of drag delta.")]
    public float rotationSpeed = 0.2f;

    [Tooltip("Minimum pitch angle in degrees (looking upward / shallow).")]
    [Range(-89f, 89f)]
    public float minPitch = 10f;

    [Tooltip("Maximum pitch angle in degrees (looking downward / steep).")]
    [Range(-89f, 89f)]
    public float maxPitch = 80f;

    [Header("Zoom")]
    [Tooltip("Distance multiplier per mouse-wheel notch when zooming in (e.g. 0.85 = 85% of prior distance).")]
    [Range(0.5f, 0.99f)]
    public float zoomNotchFactor = 0.85f;

    [Tooltip("Minimum camera distance from the orbit pivot.")]
    public float minDistance = 20f;

    [Tooltip("Maximum camera distance from the orbit pivot.")]
    public float maxDistance = 400f;

    [Header("Pan")]
    [Tooltip("World units per second when pan stick is fully pressed, at panSpeedReferenceDistance.")]
    public float panSpeed = 40f;

    [Tooltip("Orbit distance at which panSpeed applies as authored. Scale = currentDistance / this.")]
    public float panSpeedReferenceDistance = 180f;

    [Tooltip("Max XZ distance the orbit pivot may drift from the island pivot.")]
    public float maxPanRadius = 100f;

    [Tooltip("Smooth time for pan-offset catch-up. At or below zero snaps instantly.")]
    public float panDamping = 0.08f;

    [Header("Smoothing")]
    [Tooltip("Smooth time for yaw/pitch catch-up. At or below zero snaps instantly.")]
    public float rotationDamping = 0.08f;

    [Tooltip("Smooth time for distance catch-up. At or below zero snaps instantly.")]
    public float zoomDamping = 0.1f;

    [Tooltip("Smooth time for pivot and default-view re-center after OrbitPivotChanged.")]
    public float pivotRecenterDamping = 0.2f;

    [Header("Defaults")]
    [Tooltip("Yaw used on Start and after island-swap re-center.")]
    public float defaultYaw;

    [Tooltip("Pitch used on Start and after island-swap re-center.")]
    public float defaultPitch = 40f;

    [Tooltip("Distance used on Start and after island-swap re-center.")]
    public float defaultDistance = 180f;

    [Header("Behavior")]
    [Tooltip("When true, a drag that starts over UI is ignored until released.")]
    public bool ignorePointerOverUI = true;
}
