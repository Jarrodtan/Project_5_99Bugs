using System.Collections.Generic;

/// <summary>
/// Tab-switching rules shared by every tab in <see cref="TabbedStatsPanel"/>, always-on or contextual:
/// exactly one available tab is selected; unavailable tabs can't be selected; a tab that becomes
/// available (or asks for focus) can take it; and when the selected tab becomes unavailable, selection falls back to
/// the previously selected tab (or the first available one).
/// Display-only — it never touches the data behind the tabs.
/// </summary>
public class TabSelectionModel
{
    readonly List<bool> available = new();
    readonly bool focusOnReveal;
    int previousIndex = -1;

    /// <summary>
    /// Creates a model with every tab available and none selected.
    /// </summary>
    /// <param name="tabCount">Number of tabs.</param>
    /// <param name="focusOnReveal">True to select a tab when it becomes available.</param>
    public TabSelectionModel(int tabCount, bool focusOnReveal)
    {
        for (var i = 0; i < tabCount; i++)
            available.Add(true);
        this.focusOnReveal = focusOnReveal;
    }

    /// <summary>Number of tabs.</summary>
    public int Count => available.Count;

    /// <summary>Selected tab index, or -1 when no tab is available.</summary>
    public int SelectedIndex { get; private set; } = -1;

    /// <summary>
    /// Whether tab <paramref name="index"/> is in the tab bar.
    /// </summary>
    /// <param name="index">Tab index.</param>
    /// <returns>False for out-of-range indices.</returns>
    public bool IsAvailable(int index)
    {
        return index >= 0 && index < available.Count && available[index];
    }

    /// <summary>
    /// Selects tab <paramref name="index"/>. Ignored when it is out of range or unavailable.
    /// </summary>
    /// <param name="index">Tab index.</param>
    /// <returns>True when the selection changed.</returns>
    public bool Select(int index)
    {
        if (!IsAvailable(index) || index == SelectedIndex)
            return false;

        previousIndex = SelectedIndex;
        SelectedIndex = index;
        return true;
    }

    /// <summary>
    /// Selects <paramref name="preferred"/> when available, otherwise the first available tab.
    /// Use once at startup, after initial availability is set.
    /// </summary>
    /// <param name="preferred">Preferred tab index.</param>
    public void SelectDefault(int preferred)
    {
        if (!Select(preferred) && !IsAvailable(SelectedIndex))
            SelectedIndex = FirstAvailable();
    }

    /// <summary>
    /// Selects tab <paramref name="index"/> on the content's behalf (e.g. a new object was selected for
    /// Inspect). Honoured only when focus-on-reveal is enabled and the tab is available.
    /// </summary>
    /// <param name="index">Tab index.</param>
    /// <returns>True when the selection changed.</returns>
    public bool RequestFocus(int index)
    {
        return focusOnReveal && Select(index);
    }

    /// <summary>
    /// Shows or hides tab <paramref name="index"/> in the tab bar, applying focus and fallback rules.
    /// </summary>
    /// <param name="index">Tab index.</param>
    /// <param name="isAvailable">New availability.</param>
    /// <param name="allowFocus">False to suppress focus-on-reveal (e.g. during startup).</param>
    /// <returns>True when availability or selection changed.</returns>
    public bool SetAvailable(int index, bool isAvailable, bool allowFocus = true)
    {
        if (index < 0 || index >= available.Count || available[index] == isAvailable)
            return false;

        available[index] = isAvailable;

        if (isAvailable)
        {
            if (focusOnReveal && allowFocus)
                Select(index);
            else if (SelectedIndex < 0)
                SelectedIndex = index;
            return true;
        }

        if (index == SelectedIndex)
        {
            SelectedIndex = IsAvailable(previousIndex) ? previousIndex : FirstAvailable();
            previousIndex = -1;
        }

        return true;
    }

    int FirstAvailable()
    {
        for (var i = 0; i < available.Count; i++)
        {
            if (available[i])
                return i;
        }

        return -1;
    }
}
