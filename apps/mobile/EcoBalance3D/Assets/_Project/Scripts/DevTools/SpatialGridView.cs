// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Developer view of the EcoRegistry spatial grid (SCRUM-265), Editor and development builds only.
/// Outlines every grid cell that holds an animal or resource, coloured by how many it holds
/// (green 1, yellow 2-3, red 4+), and lists which species use the grid. Toggle with G.
/// Added by <see cref="SimDebugOverlay"/>; starts hidden.
/// </summary>
public class SpatialGridView : MonoBehaviour
{
    public EcosystemSimulator sim;
    bool visible;
    Material lineMat;
    GUIStyle label;
    readonly Dictionary<Vector2Int, int> cells = new Dictionary<Vector2Int, int>();

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) visible = !visible;
        if (visible) EcoRegistry.CountPerCell(cells);
    }

    // Runs after the camera renders (also under URP), so the lines draw over the island.
    void OnRenderObject()
    {
        if (!visible || cells.Count == 0) return;
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        float s = EcoRegistry.CellSize;
        float y = sim != null ? sim.transform.position.y + 0.2f : 0.2f;
        lineMat.SetPass(0);
        GL.Begin(GL.LINES);
        foreach (var kv in cells)
        {
            GL.Color(kv.Value >= 4 ? Color.red : kv.Value >= 2 ? Color.yellow : Color.green);
            float x0 = kv.Key.x * s, z0 = kv.Key.y * s, x1 = x0 + s, z1 = z0 + s;
            Line(x0, z0, x1, z0, y); Line(x1, z0, x1, z1, y);
            Line(x1, z1, x0, z1, y); Line(x0, z1, x0, z0, y);
        }
        GL.End();
    }

    static void Line(float xa, float za, float xb, float zb, float y)
    {
        GL.Vertex3(xa, y, za);
        GL.Vertex3(xb, y, zb);
    }

    void OnGUI()
    {
        label ??= new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = Color.white } };
        // No on-screen hint, so recordings stay clean; press G to show the grid.
        if (!visible)
            return;

        var text = $"Spatial grid ({EcoRegistry.CellSize} m cells, grid from {EcoRegistry.GridThreshold}+)\n";
        if (sim != null)
            foreach (var kv in sim.populations)
                text += $"{kv.Key.name}: {EcoRegistry.Count(kv.Key)} -> {(EcoRegistry.UsesGrid(kv.Key) ? "GRID" : "plain scan")}\n";
        GUI.Label(new Rect(10, Screen.height - 160, 700, 150), text, label);
    }

    void OnDestroy()
    {
        if (lineMat != null) Destroy(lineMat);
    }
}
#endif
// [AI-GENERATED END]
