using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Tooltip("Tooltip shown on the FAB. Empty hides it.")]
    public string fabTooltip = "Actions";

    [Header("Entity Spawn")]
    [Tooltip("Fraction of the active terrain or island mesh used when random position is on. 1 covers the whole surface.")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("entityScatterRadius")]
    public float entitySpawnCoverage = 1f;

    [Tooltip("Added to the island pivot height when spawning an entity visual.")]
    public float spawnSurfaceOffset = 0.75f;

    [Tooltip("World-unit length of the vertical ray drawn at each spawned entity. Zero hides it.")]
    public float spawnRayLength = 0f;

    [Tooltip("World-unit width of the spawn ray.")]
    public float spawnRayWidth = 1.5f;

    [Tooltip("Color of the ray drawn at each entity spawn.")]
    public Color spawnRayColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("Seconds the light at a new entity stays on. Zero hides it.")]
    public float spawnLightSeconds = 0.5f;

    [Tooltip("Brightness of the spawn light, in lumens.")]
    public float spawnLightIntensity = 80000f;

    [Tooltip("Length of the vertical spawn beam, in world units.")]
    public float spawnLightRange = 40f;

    [Tooltip("Full angle of the vertical spawn beam, in degrees. Smaller is a thinner ray.")]
    [Range(1f, 80f)]
    public float spawnLightAngle = 12f;

    [Tooltip("Color of the spawn light.")]
    public Color spawnLightColor = new Color(1f, 0.95f, 0.75f, 1f);

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
