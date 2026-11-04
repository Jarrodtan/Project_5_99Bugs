using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central island platform hub. Catalog, world anchor, transition, wall geometry, and gizmo
/// knobs live here — never hardcode them in MonoBehaviours that read this config.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Island Platform Config", fileName = "IslandPlatformConfig")]
public class IslandPlatformConfig : ScriptableObject
{
    [Header("Catalog")]
    [Tooltip("All authored islands. Add a new IslandDefinition asset here — do not hardcode islands in C#.")]
    public List<IslandDefinition> islands = new();

    [Tooltip("Island spawned when no id is supplied. Leave empty until authored.")]
    public IslandDefinition defaultIsland;

    [Header("Placement")]
    [Tooltip("Fixed world position for the island root (orbit/rotation pivot).")]
    public Vector3 worldAnchor = Vector3.zero;

    [Header("Transition")]
    [Tooltip("Seconds for island fade-out / fade-in during a swap.")]
    public float transitionDurationSeconds = 0.4f;

    [Header("Physical Boundary Wall")]
    [Tooltip("Number of box segments in the containment ring. Used only when boundary.radius > 0.")]
    public int wallSegmentCount = 16;

    [Tooltip("World-space height of each wall segment.")]
    public float wallHeight = 4f;

    [Tooltip("World-space thickness of each wall segment.")]
    public float wallThickness = 0.5f;

    [Header("Logical Bounds")]
    [Tooltip("When true, IslandContainment clamps registered IIslandBoundable positions each LateUpdate.")]
    public bool enforceLogicalBounds = true;

    [Header("Debug")]
    [Tooltip("Log island lookups to the Console.")]
    public bool logIslandLookups;

    [Tooltip("When true, IslandManager draws Scene gizmos for the orbit pivot and visual center.")]
    public bool drawAnchorGizmos = true;

    [Tooltip("Scene gizmo sphere radius at the island root (orbit pivot).")]
    public float gizmoPivotRadius = 0.15f;

    [Tooltip("Scene gizmo wire-sphere radius at the visual center.")]
    public float gizmoCenterRadius = 0.25f;

    /// <summary>
    /// Looks up an island by its <see cref="IslandDefinition.id"/> string.
    /// </summary>
    /// <param name="id">Caller-supplied island id to match.</param>
    /// <param name="island">Matched definition, or null.</param>
    /// <returns>True when a matching non-null island was found.</returns>
    public bool TryGetIsland(string id, out IslandDefinition island)
    {
        island = null;
        if (string.IsNullOrEmpty(id) || islands == null)
            return false;

        for (var i = 0; i < islands.Count; i++)
        {
            var candidate = islands[i];
            if (candidate != null &&
                string.Equals(candidate.id, id, StringComparison.Ordinal))
            {
                island = candidate;
                if (logIslandLookups)
                    Debug.Log($"IslandPlatformConfig: found island id '{id}'.", this);
                return true;
            }
        }

        if (logIslandLookups)
            Debug.Log($"IslandPlatformConfig: no island for id '{id}'.", this);
        return false;
    }
}
