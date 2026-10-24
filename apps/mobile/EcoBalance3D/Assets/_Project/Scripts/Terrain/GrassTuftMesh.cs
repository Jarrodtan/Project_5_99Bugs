// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a clump of grass blades as one mesh, so a grass patch looks like grass instead of a box.
/// Each blade is a thin tapered triangle pair, leaning a little in a random direction.
/// One mesh is shared by every patch (each patch gets a random spin), so it costs one draw per patch.
/// Use a material with back-face culling off so blades show from both sides.
/// </summary>
public static class GrassTuftMesh
{
    public static Mesh Build(int seed, int blades = 140, float radius = 2.2f, float height = 0.7f, float width = 0.09f)
    {
        var rng = new System.Random(seed);
        float R() => (float)rng.NextDouble();

        var verts = new List<Vector3>(blades * 4);
        var normals = new List<Vector3>(blades * 4);
        var tris = new List<int>(blades * 6);

        for (int i = 0; i < blades; i++)
        {
            // Denser in the middle, thinner at the edge, so the clump has a soft outline.
            float r = radius * Mathf.Sqrt(R()) * (0.4f + 0.6f * R());
            float a = R() * Mathf.PI * 2f;
            var root = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);

            float h = height * (0.5f + 0.7f * R()) * (1f - 0.4f * r / radius);   // shorter at the edge
            float facing = R() * Mathf.PI;
            var side = new Vector3(Mathf.Cos(facing), 0f, Mathf.Sin(facing)) * (width * (0.7f + 0.6f * R()));
            var lean = new Vector3(R() - 0.5f, 0f, R() - 0.5f) * (h * 0.6f);
            var normal = Vector3.Cross(side, Vector3.up).normalized;

            int v = verts.Count;
            verts.Add(root - side);
            verts.Add(root + side);
            verts.Add(root + side * 0.4f + Vector3.up * h * 0.55f + lean * 0.35f);
            verts.Add(root + Vector3.up * h + lean);                               // tip
            for (int k = 0; k < 4; k++) normals.Add(Vector3.Lerp(normal, Vector3.up, 0.6f).normalized);

            tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            tris.Add(v); tris.Add(v + 2); tris.Add(v + 3);
        }

        var mesh = new Mesh { name = "Grass Tuft" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);   // mostly up, so blades are lit like a lawn rather than flickering
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
