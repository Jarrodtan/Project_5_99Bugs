using UnityEngine;

/// <summary>
/// Default adapter that marks a GameObject as selectable. Future agents may implement
/// <see cref="ISelectable"/> directly and skip this component. Does not reference Config or Context.
/// </summary>
[DisallowMultipleComponent]
public class Selectable : MonoBehaviour, ISelectable
{
    [Tooltip("Type asset whose id is stored on the selection handle.")]
    [SerializeField]
    SelectableTypeDefinition type;

    [Tooltip("Instance id. Leave at 0 to allocate a unique id when this object enables.")]
    [SerializeField]
    int id;

    SelectionHandle handle;
    bool registered;

    /// <inheritdoc />
    public SelectionHandle Handle => handle;

    /// <summary>Type asset assigned to this selectable (may be null before Configure).</summary>
    public SelectableTypeDefinition TypeDefinition => type;

    /// <summary>
    /// Assigns type and instance id before (or after) enable. Prefer calling while the GameObject is inactive
    /// so the first <see cref="OnEnable"/> registers the intended handle.
    /// </summary>
    /// <param name="typeDefinition">Type asset whose id is stored on the handle.</param>
    /// <param name="instanceId">Instance id. Pass 0 to allocate on enable.</param>
    public void Configure(SelectableTypeDefinition typeDefinition, int instanceId)
    {
        if (registered)
            SelectableRegistry.Unregister(this);

        registered = false;
        type = typeDefinition;
        id = instanceId;
        handle = SelectionHandle.None;

        if (isActiveAndEnabled)
            RegisterIfReady();
    }

    void OnEnable() => RegisterIfReady();

    void OnDisable()
    {
        if (!registered)
            return;

        SelectableRegistry.Unregister(this);
        registered = false;
    }

    void RegisterIfReady()
    {
        if (type == null || string.IsNullOrEmpty(type.id))
        {
            Debug.LogError($"{nameof(Selectable)} on '{name}' needs a SelectableTypeDefinition with a non-empty id.", this);
            return;
        }

        if (id == 0)
            id = SelectableRegistry.AllocateId();

        handle = new SelectionHandle(id, type.id);
        SelectableRegistry.Register(this);
        registered = true;
    }
}
