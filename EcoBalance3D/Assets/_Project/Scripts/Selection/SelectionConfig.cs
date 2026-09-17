using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tunables for click/tap picking. Assign the Input Action asset and action paths here —
/// never hardcode devices, layers, or distances in MonoBehaviours.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Selection Config", fileName = "SelectionConfig")]
public class SelectionConfig : ScriptableObject
{
    [Header("Input")]
    [Tooltip("Optional. Input Action asset for selection. Leave empty to use Project Settings → Input System Package → Input Actions (project-wide).")]
    public InputActionAsset actionsAsset;

    [Tooltip("Path to the button action used for click/tap (map/action), e.g. Player/Select.")]
    public string selectActionPath = "Player/Select";

    [Tooltip("Path to the Vector2 action for screen position (map/action), e.g. Player/PointerPosition.")]
    public string pointerPositionActionPath = "Player/PointerPosition";

    [Tooltip("Minimum seconds between accepted selection presses. Prevents one physical click from selecting then immediately clearing.")]
    public float pressDebounceSeconds = 0.2f;

    [Header("Raycast")]
    [Tooltip("Physics layers that can be hit for selection. Non-selectable colliders (e.g. ground) should be excluded.")]
    public LayerMask selectableLayers;

    [Tooltip("Maximum raycast distance from the camera.")]
    public float maxDistance = 500f;

    [Tooltip("Whether raycasts hit trigger colliders.")]
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Behavior")]
    [Tooltip("Clear selection when the press misses or hits a non-selectable collider.")]
    public bool deselectOnEmptySpace = true;

    [Tooltip("Clear selection when the currently selected object is pressed again.")]
    public bool toggleDeselectOnReselect = true;

    [Tooltip("Ignore presses when the pointer is over a UI EventSystem element.")]
    public bool ignorePointerOverUI = true;

    [Header("Camera")]
    [Tooltip("When true and ObjectSelector has no camera override, use Camera.main.")]
    public bool useMainCamera = true;

    [Header("Debug")]
    [Tooltip("Log selection changes to the Console.")]
    public bool logSelectionChanges = true;
}
