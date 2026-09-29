using System;
using UnityEngine;

/// <summary>
/// A HUD panel that can live inside <see cref="TabbedStatsPanel"/> as one tab instead of on its own canvas.
/// Showing/hiding is display-only: content keeps reading its data while its tab is not selected.
/// Implementations share <see cref="TabContentFrame"/> for embedding and visibility.
/// </summary>
public interface ITabbedPanelContent
{
    /// <summary>
    /// Text shown on this panel's tab button.
    /// </summary>
    string TabLabel { get; }

    /// <summary>
    /// Whether the tab button should be in the tab bar right now. Always-on tabs return true;
    /// contextual tabs (e.g. Inspect) return true only while they have something to show.
    /// </summary>
    bool IsTabAvailable { get; }

    /// <summary>
    /// Raised when <see cref="IsTabAvailable"/> changes.
    /// </summary>
    event Action TabAvailabilityChanged;

    /// <summary>
    /// Raised when the content wants its tab selected (e.g. Inspect on every new selection).
    /// The host honours it only while the tab is available.
    /// </summary>
    event Action TabFocusRequested;

    /// <summary>
    /// Builds (or moves) the panel's UI under <paramref name="host"/> instead of its own canvas.
    /// The panel stretches to the host's width and hides its own title, since the tab names it.
    /// </summary>
    /// <param name="host">Content area of the tab host, top-anchored.</param>
    void EmbedIn(RectTransform host);

    /// <summary>
    /// Shows or hides the panel visually. Must not pause data reads or animations.
    /// </summary>
    /// <param name="shown">True when this panel's tab is selected.</param>
    void SetShown(bool shown);
}
