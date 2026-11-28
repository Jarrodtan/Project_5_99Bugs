// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Test helper: saves what the player would see (the 3D world and every UI canvas, in sorting order) as a PNG,
/// also in batch mode where ScreenCapture has no screen to read. Overlay canvases are drawn for the shot by a
/// temporary camera into a 1920x1200 texture (the UI's reference size), then put back as they were.
/// </summary>
public static class ScreenshotUtil
{
    const int ShotLayer = 31;

    /// <summary>Folder for the shots: the ECOBALANCE_SHOTS environment variable, or null (no shots taken).</summary>
    public static string Folder => System.Environment.GetEnvironmentVariable("ECOBALANCE_SHOTS");

    public static void Capture(string name, int width = 1920, int height = 1200)
    {
        var folder = Folder;
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var prevActive = RenderTexture.active;

        // World first (the main camera), or a plain background.
        var main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
        {
            var prev = main.targetTexture;
            main.targetTexture = rt;
            main.Render();
            main.targetTexture = prev;
        }
        else
        {
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.15f, 0.17f, 0.2f));
        }

        // Then every root overlay canvas, through a camera that only sees them.
        var cam = new GameObject("[ShotCamera]").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Depth;
        cam.cullingMask = 1 << ShotLayer;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.depth = 100;

        var changed = new List<(Canvas c, RenderMode mode, Camera cam, float plane)>();
        var layers = new List<(GameObject go, int layer)>();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            changed.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
            foreach (var t in c.GetComponentsInChildren<Transform>(true)) { layers.Add((t.gameObject, t.gameObject.layer)); t.gameObject.layer = ShotLayer; }
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 10f;
        }
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());

        foreach (var (c, mode, wc, plane) in changed)
        {
            if (c == null) continue;
            c.renderMode = mode; c.worldCamera = wc; c.planeDistance = plane;
        }
        foreach (var (go, layer) in layers) if (go != null) go.layer = layer;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(cam.gameObject);
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Canvas.ForceUpdateCanvases();
    }
}
