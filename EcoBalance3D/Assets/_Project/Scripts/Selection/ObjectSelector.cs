using UnityEngine;

/// <summary>
/// Orchestrates selection: pointer → caster → rules → <see cref="SelectionContext"/>.
/// Does not store a GameObject as the selection and does not expose a singleton.
/// </summary>
public class ObjectSelector : MonoBehaviour
{
    [Tooltip("Picking tunables and Input Action references.")]
    [SerializeField]
    SelectionConfig config;

    [Tooltip("Shared selection session that owns Current and Changed events.")]
    [SerializeField]
    SelectionContext context;

    [Tooltip("Optional camera override. When empty and config.useMainCamera is true, Camera.main is used.")]
    [SerializeField]
    Camera cameraOverride;

    ISelectionPointerSource pointerSource;
    ISelectionCaster caster;

    /// <summary>
    /// Injects pointer and caster implementations. Call before Enable if replacing defaults.
    /// </summary>
    /// <param name="pointer">Pointer source to use.</param>
    /// <param name="selectionCaster">Caster to use.</param>
    public void SetModules(ISelectionPointerSource pointer, ISelectionCaster selectionCaster)
    {
        pointerSource = pointer;
        caster = selectionCaster;
    }

    void Awake()
    {
        if (pointerSource == null)
            pointerSource = new InputActionPointerSource();
        if (caster == null)
            caster = new PhysicsSelectionCaster();
    }

    void OnEnable()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(ObjectSelector)} requires a {nameof(SelectionConfig)}.", this);
            return;
        }

        if (context == null)
        {
            Debug.LogError($"{nameof(ObjectSelector)} requires a {nameof(SelectionContext)}.", this);
            return;
        }

        context.BindLifetime();
        pointerSource.Enable(config);
    }

    void OnDisable()
    {
        pointerSource?.Disable();
    }

    void Update()
    {
        if (config == null || context == null || pointerSource == null || caster == null)
            return;

        if (!pointerSource.TryGetPress(out var screenPosition))
            return;

        if (config.ignorePointerOverUI && PointerOverUI.IsOver())
            return;

        var camera = ResolveCamera();
        if (camera == null)
        {
            Debug.LogError($"{nameof(ObjectSelector)}: no camera available for picking.", this);
            return;
        }

        SelectionHandle hitHandle = SelectionHandle.None;
        if (caster.TryGetSelectable(camera, screenPosition, config, out var selectable) && selectable != null)
            hitHandle = selectable.Handle;

        var next = SelectionRules.Resolve(context.Current, hitHandle, config);
        if (next.IsValid)
            context.Select(next);
        else
            context.Clear();
    }

    Camera ResolveCamera()
    {
        if (cameraOverride != null)
            return cameraOverride;

        if (config != null && config.useMainCamera)
            return Camera.main;

        return null;
    }
}
