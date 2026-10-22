// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Swings the legs of static meshes by moving their vertices (CPU), using a <see cref="LegLayout"/>
/// found from the meshes themselves. Each mesh gets its own copy, so the shared model asset is never
/// changed. Only leg vertices are recomputed, and nothing is uploaded while the legs are still.
/// Meshes must be readable (model import setting Read/Write).
/// </summary>
public class LegDeformer
{
    class Part
    {
        public Mesh mesh;
        public Matrix4x4 fromRig;
        public Vector3[] vertices;   // working copy, mesh space; body vertices never change
        public int[] legIndex;       // vertex index of each leg vertex
        public Vector3[] legRig;     // its rest position in rig space
        public int[] leg;
        public float[] weight;
    }

    readonly List<Part> parts = new List<Part>();
    readonly float[] lastAngles = new float[4];
    bool atRest = true;

    public LegLayout Layout { get; private set; }
    public bool Valid => Layout != null && Layout.Valid && parts.Count > 0;
    public int LegVertexCount { get; private set; }

    /// <summary>Builds from every readable MeshFilter under <paramref name="model"/>, in <paramref name="rigRoot"/>'s local space.</summary>
    public static LegDeformer Build(Transform rigRoot, Transform model, float legHeightFraction, float legBlendFraction)
    {
        var deformer = new LegDeformer();
        var filters = new List<MeshFilter>();
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null && mf.sharedMesh.isReadable) filters.Add(mf);
        if (filters.Count == 0) return deformer;

        var toRig = new Matrix4x4[filters.Count];
        var all = new List<Vector3>();
        for (int i = 0; i < filters.Count; i++)
        {
            toRig[i] = rigRoot.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
            foreach (var v in filters[i].sharedMesh.vertices) all.Add(toRig[i].MultiplyPoint3x4(v));
        }
        deformer.Layout = LegLayout.Analyze(all, legHeightFraction, legBlendFraction);
        if (!deformer.Layout.Valid) return deformer;

        for (int i = 0; i < filters.Count; i++)
        {
            var source = filters[i].sharedMesh.vertices;
            var idx = new List<int>();
            var rig = new List<Vector3>();
            var legs = new List<int>();
            var weights = new List<float>();
            for (int v = 0; v < source.Length; v++)
            {
                var p = toRig[i].MultiplyPoint3x4(source[v]);
                if (!deformer.Layout.Classify(p, out int leg, out float w) || w <= 0f) continue;
                idx.Add(v); rig.Add(p); legs.Add(leg); weights.Add(w);
            }
            if (idx.Count == 0) continue;

            var mesh = filters[i].mesh; // per-object copy
            mesh.MarkDynamic();
            deformer.parts.Add(new Part
            {
                mesh = mesh,
                fromRig = toRig[i].inverse,
                vertices = source,
                legIndex = idx.ToArray(),
                legRig = rig.ToArray(),
                leg = legs.ToArray(),
                weight = weights.ToArray(),
            });
            deformer.LegVertexCount += idx.Count;
        }
        return deformer;
    }

    /// <summary>Poses the four legs (degrees, + = foot forward; index = LegLayout.FrontLeft...).</summary>
    public void Apply(float[] angles)
    {
        if (!Valid) return;
        bool still = true;
        bool changed = false;
        for (int l = 0; l < 4; l++)
        {
            if (Mathf.Abs(angles[l]) > 0.01f) still = false;
            if (Mathf.Abs(angles[l] - lastAngles[l]) > 0.01f) changed = true;
        }
        if (!changed || (still && atRest)) return;

        foreach (var part in parts)
        {
            for (int k = 0; k < part.legIndex.Length; k++)
            {
                int leg = part.leg[k];
                var rig = Layout.Rotate(part.legRig[k], leg, angles[leg], part.weight[k]);
                part.vertices[part.legIndex[k]] = part.fromRig.MultiplyPoint3x4(rig);
            }
            part.mesh.vertices = part.vertices;
        }
        System.Array.Copy(angles, lastAngles, 4);
        atRest = still;
    }

    /// <summary>Destroys the per-object mesh copies.</summary>
    public void Dispose()
    {
        foreach (var part in parts)
            if (part.mesh != null) Object.Destroy(part.mesh);
        parts.Clear();
    }
}
// [AI-GENERATED END]
