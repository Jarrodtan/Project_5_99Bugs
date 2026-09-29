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

    [Header("Rotation")]
    [Tooltip("Degrees of yaw/pitch applied per unit of drag delta.")]
    public float rotationSpeed = 0.2f;

    [Tooltip("Minimum pitch angle in degrees.")]
    public float minPitch = 10f;

    [Tooltip("Maximum pitch angle in degrees.")]
    public float maxPitch = 80f;

    [Header("Zoom")]
    [Tooltip("Distance change per unit of zoom input.")]
    public float zoomSpeed = 1f;

    [Tooltip("Minimum camera distance from the orbit pivot.")]
    public float minDistance = 5f;

    [Tooltip("Maximum camera distance from the orbit pivot.")]
    public float maxDistance = 40f;

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
    public float defaultPitch = 35f;

    [Tooltip("Distance used on Start and after island-swap re-center.")]
    public float defaultDistance = 15f;

    [Header("Behavior")]
    [Tooltip("When true, a drag that starts over UI is ignored until released.")]
    public bool ignorePointerOverUI = true;
}
