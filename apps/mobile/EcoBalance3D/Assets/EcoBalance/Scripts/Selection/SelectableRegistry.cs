using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime map from <see cref="SelectionHandle"/> to live <see cref="ISelectable"/> instances,
/// plus a central id allocator. Does not track which handle is currently selected.
/// </summary>
public static class SelectableRegistry
{
    static readonly Dictionary<SelectionHandle, ISelectable> Map = new();
    static int nextId = 1;

    /// <summary>Raised when a handle is removed from the registry (disable/destroy).</summary>
    public static event Action<SelectionHandle> Unregistered;

    /// <summary>
    /// Allocates a unique positive instance id for a selectable that did not specify one.
    /// </summary>
    /// <returns>A non-zero id unique for this play session.</returns>
    public static int AllocateId() => nextId++;

    /// <summary>
    /// Registers a selectable under its handle. Replaces any previous entry for the same handle.
    /// </summary>
    /// <param name="selectable">Selectable to register.</param>
    public static void Register(ISelectable selectable)
    {
        if (selectable == null)
            return;

        var handle = selectable.Handle;
        if (!handle.IsValid)
        {
            Debug.LogError("SelectableRegistry.Register: handle is invalid.");
            return;
        }

        Map[handle] = selectable;
    }

    /// <summary>
    /// Removes a selectable from the registry and raises <see cref="Unregistered"/>.
    /// </summary>
    /// <param name="selectable">Selectable to remove.</param>
    public static void Unregister(ISelectable selectable)
    {
        if (selectable == null)
            return;

        var handle = selectable.Handle;
        if (!handle.IsValid)
            return;

        if (!Map.TryGetValue(handle, out var existing) || !ReferenceEquals(existing, selectable))
            return;

        Map.Remove(handle);
        Unregistered?.Invoke(handle);
    }

    /// <summary>
    /// Looks up a live selectable by handle.
    /// </summary>
    /// <param name="handle">Handle to resolve.</param>
    /// <param name="selectable">Resolved selectable, or null if missing/destroyed.</param>
    /// <returns>True when a live Unity object was found.</returns>
    public static bool TryGet(SelectionHandle handle, out ISelectable selectable)
    {
        selectable = null;
        if (!handle.IsValid)
            return false;

        if (!Map.TryGetValue(handle, out var found) || found == null)
            return false;

        if (found is UnityEngine.Object unityObj && unityObj == null)
        {
            Map.Remove(handle);
            return false;
        }

        selectable = found;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Map.Clear();
        nextId = 1;
        Unregistered = null;
    }
}
