// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The head (front end) or tail (back end) of a static four-legged model, found from its vertices:
/// everything above the hips in the last <c>lengthFraction</c> of the body at that end. It bends
/// around a joint (neck or tail root) where it meets the body, blending in so the mesh doesn't tear.
/// Same rig space as <see cref="LegLayout"/>.
/// </summary>
public class Appendage
{
    const int MinPoints = 3;

    public bool Valid { get; private set; }
    public bool IsHead { get; private set; }
    /// <summary>The neck / tail-root joint it bends around.</summary>
    public Vector3 Joint { get; private set; }

    Vector3 outward;   // from the body towards the tip (forward for a head, backward for a tail)
    Vector3 pitchAxis; // + angle bends the tip down
    float start, blend;
    float hipY;

    public static Appendage Analyze(IReadOnlyList<Vector3> points, LegLayout body, bool head, float lengthFraction, float blendFraction)
    {
        var a = new Appendage { IsHead = head };
        if (body == null || !body.Valid || points == null) return a;

        a.outward = head ? body.Forward : -body.Forward;
        a.hipY = body.HipY;
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var p in points)
        {
            float along = Vector3.Dot(p - body.Center, a.outward);
            lo = Mathf.Min(lo, along);
            hi = Mathf.Max(hi, along);
        }
        float length = hi - lo;
        if (length <= 0f) return a;
        a.start = hi - Mathf.Clamp01(lengthFraction) * length;
        a.blend = Mathf.Max(1e-5f, blendFraction * length);

        // Joint: where the head/tail meets the body, at the average height of the part.
        float ySum = 0f;
        int count = 0;
        foreach (var p in points)
        {
            if (p.y < a.hipY || Vector3.Dot(p - body.Center, a.outward) < a.start) continue;
            ySum += p.y;
            count++;
        }
        if (count < MinPoints) return a;
        a.Joint = new Vector3(body.Center.x, ySum / count, body.Center.z) + a.outward * a.start;
        // Rotating the outward direction by + this axis tips it down (cross(outward, down) x outward = down).
        a.pitchAxis = Vector3.Cross(a.outward, Vector3.down);
        a.Valid = true;
        return a;
    }

    /// <summary>
    /// How much <paramref name="p"/> follows the head/tail: 0 at the joint (and for the body and legs),
    /// rising smoothly to 1 a blend-length beyond it.
    /// </summary>
    public float Weight(Vector3 p, Vector3 center)
    {
        if (!Valid || p.y < hipY) return 0f;
        float t = Mathf.Clamp01((Vector3.Dot(p - center, outward) - start) / blend);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// <paramref name="p"/> after bending <paramref name="pitchDeg"/> (+ = tip down) and turning
    /// <paramref name="yawDeg"/> (+ = clockwise seen from above), scaled by <paramref name="weight"/>.
    /// </summary>
    public Vector3 Rotate(Vector3 p, float pitchDeg, float yawDeg, float weight)
    {
        var rotation = Quaternion.AngleAxis(yawDeg * weight, Vector3.up) * Quaternion.AngleAxis(pitchDeg * weight, pitchAxis);
        return Joint + rotation * (p - Joint);
    }
}
// [AI-GENERATED END]
