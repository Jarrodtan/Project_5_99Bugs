// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>Where the body parts are posed this frame (degrees).</summary>
public struct BodyPartAngles
{
    /// <summary>Per leg (index = LegLayout.FrontLeft...), + = foot forward.</summary>
    public float FrontLeft, FrontRight, BackLeft, BackRight;
    /// <summary>+ = head down / turned clockwise seen from above.</summary>
    public float HeadPitch, HeadYaw;
    public float TailYaw;

    public float Leg(int leg) => leg switch
    {
        LegLayout.FrontLeft => FrontLeft,
        LegLayout.FrontRight => FrontRight,
        LegLayout.BackLeft => BackLeft,
        _ => BackRight,
    };

    public bool AlmostEquals(BodyPartAngles o, float eps) =>
        Mathf.Abs(FrontLeft - o.FrontLeft) <= eps && Mathf.Abs(FrontRight - o.FrontRight) <= eps &&
        Mathf.Abs(BackLeft - o.BackLeft) <= eps && Mathf.Abs(BackRight - o.BackRight) <= eps &&
        Mathf.Abs(HeadPitch - o.HeadPitch) <= eps && Mathf.Abs(HeadYaw - o.HeadYaw) <= eps &&
        Mathf.Abs(TailYaw - o.TailYaw) <= eps;
}

/// <summary>
/// Bends the legs, head and tail of static meshes by moving their vertices (CPU), using the
/// <see cref="LegLayout"/> and head/tail <see cref="Appendage"/>s found from the meshes themselves.
/// Each mesh gets its own copy, so the shared model asset is never changed. Only the vertices that
/// belong to a part are recomputed, and nothing is uploaded when the pose hasn't changed.
/// Meshes must be readable (model import setting Read/Write).
/// </summary>
public class BodyDeformer
{
    const byte Leg = 1, Head = 2, Tail = 3;

    class Part
    {
        public Mesh mesh;
        public Matrix4x4 fromRig;
        public Vector3[] vertices;   // working copy, mesh space; unmoving vertices never change
        public int[] index;          // vertex index of each moving vertex
        public Vector3[] rest;       // its rest position in rig space
        public byte[] kind;          // Leg / Head / Tail
        public byte[] leg;           // which leg, for Leg vertices
        public float[] weight;
    }

    readonly List<Part> parts = new List<Part>();
    BodyPartAngles last;

    public LegLayout Layout { get; private set; }
    public Appendage HeadPart { get; private set; }
    public Appendage TailPart { get; private set; }
    public bool Valid => Layout != null && Layout.Valid && parts.Count > 0;
    public int MovingVertexCount { get; private set; }

    public struct Settings
    {
        public float legHeightFraction, legBlendFraction;
        public bool facesBackward;
        public bool head, tail;
        public float headLengthFraction, tailLengthFraction, headTailBlendFraction;
    }

    /// <summary>Builds from every readable MeshFilter under <paramref name="model"/>, in <paramref name="rigRoot"/>'s local space.</summary>
    public static BodyDeformer Build(Transform rigRoot, Transform model, Settings settings)
    {
        var deformer = new BodyDeformer();
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
        var layout = LegLayout.Analyze(all, settings.legHeightFraction, settings.legBlendFraction, settings.facesBackward);
        deformer.Layout = layout;
        if (!layout.Valid) return deformer;
        if (settings.head) deformer.HeadPart = Appendage.Analyze(all, layout, true, settings.headLengthFraction, settings.headTailBlendFraction);
        if (settings.tail) deformer.TailPart = Appendage.Analyze(all, layout, false, settings.tailLengthFraction, settings.headTailBlendFraction);

        for (int i = 0; i < filters.Count; i++)
        {
            var source = filters[i].sharedMesh.vertices;
            var index = new List<int>();
            var rest = new List<Vector3>();
            var kind = new List<byte>();
            var legs = new List<byte>();
            var weights = new List<float>();
            for (int v = 0; v < source.Length; v++)
            {
                var p = toRig[i].MultiplyPoint3x4(source[v]);
                if (!deformer.Classify(p, out byte k, out int leg, out float w)) continue;
                index.Add(v); rest.Add(p); kind.Add(k); legs.Add((byte)Mathf.Max(0, leg)); weights.Add(w);
            }
            if (index.Count == 0) continue;

            var mesh = filters[i].mesh; // per-object copy
            mesh.MarkDynamic();
            deformer.parts.Add(new Part
            {
                mesh = mesh,
                fromRig = toRig[i].inverse,
                vertices = source,
                index = index.ToArray(),
                rest = rest.ToArray(),
                kind = kind.ToArray(),
                leg = legs.ToArray(),
                weight = weights.ToArray(),
            });
            deformer.MovingVertexCount += index.Count;
        }
        return deformer;
    }

    /// <summary>Legs win below the hips; above them the head or tail, whichever claims the point.</summary>
    bool Classify(Vector3 p, out byte kind, out int leg, out float weight)
    {
        kind = 0;
        if (Layout.Classify(p, out leg, out weight)) { kind = Leg; return weight > 0f; }
        leg = -1;
        if (HeadPart != null && (weight = HeadPart.Weight(p, Layout.Center)) > 0f) { kind = Head; return true; }
        if (TailPart != null && (weight = TailPart.Weight(p, Layout.Center)) > 0f) { kind = Tail; return true; }
        return false;
    }

    /// <summary>Poses the legs, head and tail. Does nothing if the pose hasn't changed.</summary>
    public void Apply(BodyPartAngles angles)
    {
        if (!Valid || angles.AlmostEquals(last, 0.01f)) return;
        foreach (var part in parts)
        {
            for (int k = 0; k < part.index.Length; k++)
            {
                var rest = part.rest[k];
                Vector3 posed = part.kind[k] switch
                {
                    Leg => Layout.Rotate(rest, part.leg[k], angles.Leg(part.leg[k]), part.weight[k]),
                    Head => HeadPart.Rotate(rest, angles.HeadPitch, angles.HeadYaw, part.weight[k]),
                    _ => TailPart.Rotate(rest, 0f, angles.TailYaw, part.weight[k]),
                };
                part.vertices[part.index[k]] = part.fromRig.MultiplyPoint3x4(posed);
            }
            part.mesh.vertices = part.vertices;
        }
        last = angles;
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
