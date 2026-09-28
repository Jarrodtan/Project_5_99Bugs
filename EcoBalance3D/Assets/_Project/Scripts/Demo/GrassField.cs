// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual layer for a resource: scatters grass patches and shows as many as the resource
/// amount allows (100% grass = all patches, 50% = half). Grazers walk to visible patches.
/// </summary>
public class GrassField : MonoBehaviour
{
    public ResourceDefinition resource;
    public GameObject patchPrefab;
    [Min(1)] public int patchCount = 30;
    [Min(1f)] public float radius = 10f;
    public int seed = 7;

    readonly List<GameObject> patches = new List<GameObject>();

    /// <summary>How many of <paramref name="total"/> patches to show for the given amount.</summary>
    public static int VisibleCount(float current, float max, int total)
    {
        if (total <= 0 || max <= 0f) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(total * Mathf.Clamp01(current / max)), 0, total);
    }

    void Awake()
    {
        if (patchPrefab == null) return;

        // Fixed seed so the island looks the same every run.
        var rng = new System.Random(seed);
        float y = patchPrefab.transform.localScale.y;
        for (int i = 0; i < patchCount; i++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float dist = Mathf.Sqrt((float)rng.NextDouble()) * radius;
            var pos = transform.position + new Vector3(Mathf.Cos(angle) * dist, y, Mathf.Sin(angle) * dist);
            var patch = Instantiate(patchPrefab, pos, Quaternion.identity, transform);
            patch.name = $"GrassPatch_{i:00}";
            patches.Add(patch);
        }
    }

    public void Refresh()
    {
        if (resource == null) return;
        int visible = VisibleCount(resource.currentAmount, resource.maxAmount, patches.Count);
        for (int i = 0; i < patches.Count; i++)
            if (patches[i] != null && patches[i].activeSelf != (i < visible))
                patches[i].SetActive(i < visible);
    }
}
// [AI-GENERATED END]
