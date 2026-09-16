using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime map from <see cref="SelectionHandle"/> to live <see cref="IInspectableStatSource"/> providers.
/// </summary>
public static class InspectableStatStore
{
    static readonly Dictionary<SelectionHandle, IInspectableStatSource> Map = new();

    /// <summary>
    /// Registers or replaces the stat source for a handle.
    /// </summary>
    /// <param name="handle">Selectable handle.</param>
    /// <param name="source">Stat source to expose via query.</param>
    public static void Register(SelectionHandle handle, IInspectableStatSource source)
    {
        if (!handle.IsValid || source == null)
        {
            Debug.LogError($"{nameof(InspectableStatStore)}.Register: invalid handle or null source.");
            return;
        }

        Map[handle] = source;
    }

    /// <summary>
    /// Removes the stat source for a handle.
    /// </summary>
    /// <param name="handle">Handle to remove.</param>
    public static void Unregister(SelectionHandle handle)
    {
        if (!handle.IsValid)
            return;

        Map.Remove(handle);
    }

    /// <summary>
    /// Looks up a registered stat source.
    /// </summary>
    /// <param name="handle">Handle to resolve.</param>
    /// <param name="source">Found source, or null.</param>
    /// <returns>True when registered.</returns>
    public static bool TryGet(SelectionHandle handle, out IInspectableStatSource source)
    {
        source = null;
        if (!handle.IsValid)
            return false;

        return Map.TryGetValue(handle, out source) && source != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Map.Clear();
    }
}
