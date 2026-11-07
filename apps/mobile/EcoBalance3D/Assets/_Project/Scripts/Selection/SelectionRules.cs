/// <summary>
/// Pure select/deselect policy driven by <see cref="SelectionConfig"/> behaviour flags.
/// </summary>
public static class SelectionRules
{
    /// <summary>
    /// Resolves the next selection handle from the current handle and an optional hit.
    /// </summary>
    /// <param name="current">Currently selected handle.</param>
    /// <param name="hitOrNone">Handle under the press, or <see cref="SelectionHandle.None"/> on a miss.</param>
    /// <param name="config">Behaviour flags (empty-space deselect, toggle on reselect).</param>
    /// <returns>Handle that should become current after this press.</returns>
    public static SelectionHandle Resolve(
        SelectionHandle current,
        SelectionHandle hitOrNone,
        SelectionConfig config)
    {
        if (config == null)
            return hitOrNone;

        if (!hitOrNone.IsValid)
        {
            if (config.deselectOnEmptySpace)
                return SelectionHandle.None;
            return current;
        }

        if (config.toggleDeselectOnReselect && current.IsValid && current == hitOrNone)
            return SelectionHandle.None;

        return hitOrNone;
    }
}
