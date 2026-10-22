// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds the four legs of a static four-legged model from its vertices alone (no bones, no Blender):
/// the legs are everything in the lowest <c>legHeightFraction</c> of the model, split into four
/// corners along the body's long axis and its side axis. Each leg swings around a hip at the top
/// of its group. Points are in "rig space": the animal's root local space, +Y up, +Z forward.
/// </summary>
public class LegLayout
{
    public const int FrontLeft = 0, FrontRight = 1, BackLeft = 2, BackRight = 3;
    const int MinPointsPerLeg = 3;

    public bool Valid { get; private set; }
    /// <summary>0 = the body is longest along X, 2 = along Z.</summary>
    public int LongAxis { get; private set; } = 2;
    public float HipY { get; private set; }
    public float BlendHeight { get; private set; }
    public Vector3 Center { get; private set; }
    public readonly Vector3[] Hips = new Vector3[4];

    /// <summary>Direction the animal faces: along its long axis, preferring +Z / +X.</summary>
    public Vector3 Forward => LongAxis == 0 ? Vector3.right : Vector3.forward;
    Vector3 Side => LongAxis == 0 ? Vector3.forward : Vector3.right;

    /// <summary>Axis a leg rotates around so that a positive angle swings the foot forward.</summary>
    public Vector3 SwingAxis => Vector3.Cross(Vector3.down, Forward);

    public static LegLayout Analyze(IReadOnlyList<Vector3> points, float legHeightFraction, float legBlendFraction)
    {
        var layout = new LegLayout();
        if (points == null || points.Count == 0) return layout;

        Vector3 min = points[0], max = points[0];
        foreach (var p in points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        float height = max.y - min.y;
        if (height <= 0f) return layout;

        layout.Center = (min + max) * 0.5f;
        layout.LongAxis = (max.x - min.x) > (max.z - min.z) ? 0 : 2;
        layout.HipY = min.y + Mathf.Clamp01(legHeightFraction) * height;
        layout.BlendHeight = Mathf.Max(1e-5f, legBlendFraction * height);

        var sums = new Vector3[4];
        var counts = new int[4];
        foreach (var p in points)
        {
            if (p.y >= layout.HipY) continue;
            int leg = layout.Corner(p);
            sums[leg] += p;
            counts[leg]++;
        }
        for (int leg = 0; leg < 4; leg++)
        {
            if (counts[leg] < MinPointsPerLeg) return layout; // not a four-legged shape
            var c = sums[leg] / counts[leg];
            layout.Hips[leg] = new Vector3(c.x, layout.HipY, c.z);
        }
        layout.Valid = true;
        return layout;
    }

    int Corner(Vector3 p)
    {
        bool front = Vector3.Dot(p - Center, Forward) >= 0f;
        bool left = Vector3.Dot(p - Center, Side) < 0f;
        return front ? (left ? FrontLeft : FrontRight) : (left ? BackLeft : BackRight);
    }

    /// <summary>
    /// Which leg <paramref name="p"/> belongs to and how much it follows that leg (0 at the hip,
    /// rising smoothly to 1 a blend-height below it). Returns false for body points.
    /// </summary>
    public bool Classify(Vector3 p, out int leg, out float weight)
    {
        leg = -1;
        weight = 0f;
        if (!Valid || p.y >= HipY) return false;
        leg = Corner(p);
        float t = Mathf.Clamp01((HipY - p.y) / BlendHeight);
        weight = t * t * (3f - 2f * t);
        return true;
    }

    /// <summary><paramref name="p"/> after its leg rotates <paramref name="angleDeg"/> (+ = foot forward), scaled by <paramref name="weight"/>.</summary>
    public Vector3 Rotate(Vector3 p, int leg, float angleDeg, float weight)
        => Hips[leg] + Quaternion.AngleAxis(angleDeg * weight, SwingAxis) * (p - Hips[leg]);

    /// <summary>
    /// A trot: front-left with back-right, front-right with back-left, half a cycle apart.
    /// Plus a fixed front/back angle (e.g. bracing to eat, legs stiff when dead).
    /// </summary>
    public static float LegAngle(int leg, float phase, float swing, float frontAngle, float backAngle)
    {
        bool diagonalA = leg == FrontLeft || leg == BackRight;
        float s = Mathf.Sin(2f * Mathf.PI * (phase + (diagonalA ? 0f : 0.5f)));
        bool front = leg == FrontLeft || leg == FrontRight;
        return swing * s + (front ? frontAngle : backAngle);
    }
}
// [AI-GENERATED END]
