using UnityEngine;

/// <summary>
/// Pluggable selection highlight presentation. Implementations must not edit the selectable's own materials.
/// </summary>
public interface ISelectionHighlightVisual
{
    /// <summary>
    /// Shows the highlight under <paramref name="target"/> using knobs from <paramref name="config"/>.
    /// </summary>
    /// <param name="target">Selected object's transform.</param>
    /// <param name="config">Shared highlight config (required).</param>
    void Apply(Transform target, SelectionHighlightConfig config);

    /// <summary>
    /// Removes the active highlight. Must tolerate the instance already being destroyed with its parent.
    /// </summary>
    void Clear();
}
