// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>A visible grass patch. Registers itself so grazers can find the nearest one.</summary>
public class GrassPatch : MonoBehaviour
{
    public static readonly List<GrassPatch> Active = new List<GrassPatch>();

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    /// <summary>Nearest active patch within <paramref name="maxDistance"/> (flat XZ distance), or null.</summary>
    public static GrassPatch Nearest(Vector3 from, float maxDistance)
    {
        GrassPatch best = null;
        float bestSqr = maxDistance * maxDistance;
        foreach (var patch in Active)
        {
            Vector3 d = patch.transform.position - from;
            float sqr = d.x * d.x + d.z * d.z;
            if (sqr < bestSqr) { bestSqr = sqr; best = patch; }
        }
        return best;
    }
}
// [AI-GENERATED END]
