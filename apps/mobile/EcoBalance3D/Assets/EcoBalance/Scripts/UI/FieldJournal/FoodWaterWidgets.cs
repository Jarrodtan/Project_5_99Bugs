// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using UnityEngine;
using UnityEngine.UI;

/// <summary>A horizontal colour gradient through evenly spaced stops (the famine / thirst meters: green to red).</summary>
// uGUI 2 no longer adds a CanvasRenderer for custom Graphics; without one nothing is drawn.
[RequireComponent(typeof(CanvasRenderer))]
public class GradientBarGraphic : MaskableGraphic
{
    public Color[] stops =
    {
        new Color32(0x2E, 0x9E, 0x4F, 0xFF), new Color32(0xC9, 0xD2, 0x2E, 0xFF),
        new Color32(0xF2, 0x9A, 0x1F, 0xFF), new Color32(0xE8, 0x4A, 0x3B, 0xFF),
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (stops == null || stops.Length == 0) return;
        var r = rectTransform.rect;
        if (stops.Length == 1) { Quad(vh, r.xMin, r.xMax, r, stops[0], stops[0]); return; }
        for (int i = 0; i < stops.Length - 1; i++)
        {
            float x0 = Mathf.Lerp(r.xMin, r.xMax, i / (float)(stops.Length - 1));
            float x1 = Mathf.Lerp(r.xMin, r.xMax, (i + 1) / (float)(stops.Length - 1));
            Quad(vh, x0, x1, r, stops[i], stops[i + 1]);
        }
    }

    static void Quad(VertexHelper vh, float x0, float x1, Rect r, Color left, Color right)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector2(x0, r.yMin), left, Vector2.zero);
        vh.AddVert(new Vector2(x0, r.yMax), left, Vector2.zero);
        vh.AddVert(new Vector2(x1, r.yMax), right, Vector2.zero);
        vh.AddVert(new Vector2(x1, r.yMin), right, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }
}

/// <summary>A round dot with a ring round it (the needle on the famine / thirst meters).</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class RingDotGraphic : MaskableGraphic
{
    public Color ring = new(0.12f, 0.16f, 0.2f);
    public float ringWidth = 5f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float radius = Mathf.Min(r.width, r.height) * 0.5f;
        Disc(vh, r.center, radius, ring);
        Disc(vh, r.center, Mathf.Max(0f, radius - ringWidth), color);
    }

    static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
    {
        const int sides = 28;
        int center = vh.currentVertCount;
        vh.AddVert(c, color, Vector2.zero);
        for (int k = 0; k <= sides; k++)
        {
            float a = k * Mathf.PI * 2f / sides;
            vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            if (k > 0) vh.AddTriangle(center, center + k, center + k + 1);
        }
    }
}
