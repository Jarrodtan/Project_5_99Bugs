/// <summary>
/// Describes a transition of the current selection. Consumers can clear feedback on
/// <see cref="Previous"/> and apply feedback on <see cref="Current"/> without keeping their own cache.
/// </summary>
public readonly struct SelectionChange
{
    /// <summary>Handle that was selected before this change (may be <see cref="SelectionHandle.None"/>).</summary>
    public SelectionHandle Previous { get; }

    /// <summary>Handle that is selected after this change (may be <see cref="SelectionHandle.None"/>).</summary>
    public SelectionHandle Current { get; }

    /// <summary>
    /// Creates a selection change payload.
    /// </summary>
    /// <param name="previous">Previously selected handle.</param>
    /// <param name="current">Newly selected handle.</param>
    public SelectionChange(SelectionHandle previous, SelectionHandle current)
    {
        Previous = previous;
        Current = current;
    }
}
