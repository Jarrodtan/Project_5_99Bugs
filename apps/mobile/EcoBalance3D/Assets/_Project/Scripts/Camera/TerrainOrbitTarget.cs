// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using UnityEngine;

/// <summary>
/// Orbit pivot for the Himalaya terrain: the centre of the built terrain.
/// Put it on the same object as <see cref="HimalayaWorldBuilder"/> and drag that object
/// into OrbitCameraController's Orbit Target slot.
/// </summary>
public class TerrainOrbitTarget : MonoBehaviour, IOrbitTarget
{
    [SerializeField] HimalayaWorldBuilder world;

    public event Action OrbitPivotChanged;

    void OnEnable() { if (world != null) world.Built += RaisePivotChanged; }
    void OnDisable() { if (world != null) world.Built -= RaisePivotChanged; }

    void RaisePivotChanged() => OrbitPivotChanged?.Invoke();

    public Vector3 GetPivotPoint()
    {
        // Before the terrain exists there is no centre yet; orbit this object instead.
        if (world == null || !world.IsReady) return transform.position;
        return world.TerrainBuilder.WorldPosition(0.5f, 0.5f);
    }
}
