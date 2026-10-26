using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sole hub for the prototype action menu. Layout, copy, modal fields, and pause live here.
/// Personal prototype — not approved Feature Plan work and not a T04 deliverable.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Action Menu Config", fileName = "ActionMenuConfig")]
public class ActionMenuConfig : ScriptableObject
{
    [Header("Layout")]
    [Tooltip("Diameter of the FAB and every bubble, in pixels.")]
    public float bubbleDiameter = 72f;

    [Tooltip("Gap between stacked category bubbles and between option bubbles, in pixels.")]
    public float stackSpacing = 12f;

    [Tooltip("Inset of the FAB from the bottom-right corner, in pixels. X is leftward, Y is upward.")]
    public Vector2 fabMargin = new Vector2(28f, 28f);

    [Tooltip("Z rotation of the FAB, in degrees, while the menu is open.")]
    public float openRotationDegrees = 45f;

    [Tooltip("How many option bubbles a row shows before the rest must be scrolled.")]
    public int maxVisibleOptions = 4;

    [Tooltip("Pixels per second the option row moves while the pointer is over a scroll edge.")]
    public float edgeScrollSpeed = 240f;

    [Tooltip("Width of each option-row scroll edge, in pixels.")]
    public float scrollEdgeWidth = 24f;

    [Header("Prototype Marker")]
    [Tooltip("Color of the dot on every category and option bubble.")]
    public Color prototypeDotColor = new Color(1f, 0.55f, 0.1f, 1f);

    [Tooltip("Diameter of the prototype dot, in pixels.")]
    public float prototypeDotSize = 14f;

    [Header("Catalog")]
    [Tooltip("Categories in stack order, nearest the FAB first.")]
    public List<ActionMenuCategory> categories = new();

    [Header("Modal")]
    [Tooltip("Pixel size of the settings panel.")]
    public Vector2 modalSize = new Vector2(420f, 320f);

    [Tooltip("Label on the button that closes the settings modal.")]
    public string modalCloseLabel = "Close";

    [Tooltip("Label drawn on the FAB.")]
    public string fabLabel = "+";

    [Header("Entity Spawn")]
    [Tooltip("Horizontal scatter radius used when the entity random-position value is on.")]
    public float entityScatterRadius = 4f;

    [Tooltip("Added to the island pivot height when spawning an entity visual.")]
    public float spawnSurfaceOffset = 0.75f;

    [Header("Pause")]
    [Tooltip("When true, SimulationClock skips ticks while the menu or a settings modal is open.")]
    public bool pauseSimulationWhileOpen;

    /// <summary>
    /// Finds the category whose id equals <paramref name="categoryId"/>.
    /// </summary>
    /// <param name="categoryId">Id from a handler or a placed bubble.</param>
    /// <param name="category">Matched entry, or null.</param>
    /// <returns>True when an entry matched.</returns>
    public bool TryGetCategory(string categoryId, out ActionMenuCategory category)
    {
        category = null;
        if (string.IsNullOrEmpty(categoryId) || categories == null)
            return false;

        for (var i = 0; i < categories.Count; i++)
        {
            var entry = categories[i];
            if (entry != null && entry.id == categoryId)
            {
                category = entry;
                return true;
            }
        }

        return false;
    }
}
