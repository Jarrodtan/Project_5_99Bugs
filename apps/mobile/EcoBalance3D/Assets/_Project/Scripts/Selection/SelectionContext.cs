using System;
using UnityEngine;

/// <summary>
/// Runtime selection session: current handle and change events. Inspector Panel and highlight
/// subscribe here — not to ObjectSelector. Does not own picking or raycast tunables.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Selection Context", fileName = "SelectionContext")]
public class SelectionContext : ScriptableObject
{
    [Tooltip("Optional. When assigned, selection changes are logged if config.logSelectionChanges is true.")]
    [SerializeField]
    SelectionConfig config;

    SelectionHandle current;

    /// <summary>Handle of the currently selected object, or <see cref="SelectionHandle.None"/>.</summary>
    public SelectionHandle Current => current;

    /// <summary>Raised after the current handle changes. Previous may be None; Current None means cleared.</summary>
    public event Action<SelectionChange> Changed;

    void OnEnable()
    {
        current = SelectionHandle.None;
        BindLifetime();
    }

    void OnDisable()
    {
        UnbindLifetime();
        Changed = null;
        current = SelectionHandle.None;
    }

    /// <summary>
    /// Ensures this context listens for registry unregisters. Safe to call repeatedly (e.g. from ObjectSelector).
    /// </summary>
    public void BindLifetime()
    {
        SelectableRegistry.Unregistered -= OnSelectableUnregistered;
        SelectableRegistry.Unregistered += OnSelectableUnregistered;
    }

    /// <summary>
    /// Stops listening for registry unregisters.
    /// </summary>
    public void UnbindLifetime()
    {
        SelectableRegistry.Unregistered -= OnSelectableUnregistered;
    }

    /// <summary>
    /// Sets the current selection to <paramref name="handle"/>. No-ops if already selected.
    /// </summary>
    /// <param name="handle">Handle to select. Pass a valid handle; use <see cref="Clear"/> to deselect.</param>
    public void Select(SelectionHandle handle)
    {
        if (!handle.IsValid)
        {
            Clear();
            return;
        }

        if (current == handle)
            return;

        Apply(handle);
    }

    /// <summary>
    /// Clears the current selection. No-ops if already clear.
    /// </summary>
    public void Clear()
    {
        if (!current.IsValid)
            return;

        Apply(SelectionHandle.None);
    }

    void Apply(SelectionHandle next)
    {
        var previous = current;
        current = next;
        var change = new SelectionChange(previous, current);

        if (config != null && config.logSelectionChanges)
        {
            if (current.IsValid)
                Debug.Log($"[Selection] Selected {current}");
            else
                Debug.Log($"[Selection] Cleared (was {previous})");
        }

        Changed?.Invoke(change);
    }

    void OnSelectableUnregistered(SelectionHandle handle)
    {
        if (current.IsValid && current == handle)
            Clear();
    }
}
