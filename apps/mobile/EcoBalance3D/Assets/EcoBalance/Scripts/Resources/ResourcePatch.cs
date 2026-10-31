using System.Collections.Generic;
using UnityEngine;

// One physical patch of a resource in the scene (a tuft of Grass, a puddle of Water).
// ResourceDefinition stays the shared, ecosystem-wide data asset; each ResourcePatch is
// its own instance with its own amount, so eating one patch doesn't drain the others.
// Put several in a scene, all pointing at the same Grass asset.
public class ResourcePatch : MonoBehaviour
{
    // Every active patch, so animals can search without scanning the whole scene.
    public static readonly List<ResourcePatch> All = new();

    [Tooltip("Which resource this patch is (e.g. Grass). Its Max Amount is this patch's cap.")]
    [SerializeField] ResourceDefinition resource;

    [Tooltip("How full the patch starts, 0 = empty, 1 = full.")]
    [Range(0f, 1f)]
    [SerializeField] float startFill = 1f;

    [Tooltip("Amount regrown per second while below max.")]
    [SerializeField] float regrowPerSecond = 2f;

    [Tooltip("Below this amount the patch counts as eaten out and animals ignore it until it regrows.")]
    [SerializeField] float minEdibleAmount = 10f;

    [Tooltip("Optional. Object scaled to show how full the patch is. Defaults to this object.")]
    [SerializeField] Transform visual;

    [Tooltip("Visual scale when empty, as a fraction of full size.")]
    [Range(0f, 1f)]
    [SerializeField] float emptyScale = 0.2f;

    float amount;
    Vector3 fullScale;

    public ResourceDefinition Resource => resource;
    public float Amount => amount;
    public bool IsEdible => amount >= minEdibleAmount;
    float MaxAmount => resource != null ? resource.maxAmount : 100f;

    void Awake()
    {
        if (visual == null) visual = transform;
        fullScale = visual.localScale;
        amount = MaxAmount * startFill;
        UpdateVisual();
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Sets the resource for a patch created from code (see ResourcePatchSpawner) and refills it.
    /// <summary>Amount regrown per second, for patches created from code.</summary>
    public float RegrowPerSecond { get => regrowPerSecond; set => regrowPerSecond = Mathf.Max(0f, value); }   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
    /// <summary>Below this amount animals ignore the patch, for patches created from code.</summary>
    public float MinEdibleAmount { get => minEdibleAmount; set => minEdibleAmount = Mathf.Max(0f, value); }   // [AI-GENERATED] Claude Code (Claude Opus 5.5)

    public void Configure(ResourceDefinition value)
    {
        resource = value;
        amount = MaxAmount * startFill;
        UpdateVisual();
    }
    // [AI-GENERATED END]

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Season / weather effect on regrowth (set by SeasonWeather): 1 = normal, 0 = nothing grows.
    public static float PlantGrowthMultiplier = 1f;
    public static float WaterGrowthMultiplier = 1f;

    bool IsWater => resource != null && resource.name.ToLowerInvariant().Contains("water");
    // [AI-GENERATED END]

    void Update()
    {
        if (amount >= MaxAmount) return;
        float mult = IsWater ? WaterGrowthMultiplier : PlantGrowthMultiplier * GuidedEffects.PlantRegen;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
        amount = Mathf.Min(MaxAmount, amount + regrowPerSecond * mult * Time.deltaTime);
        UpdateVisual();
    }

    // Takes up to `requested` from the patch, returns how much was actually taken.
    public float Consume(float requested)
    {
        float taken = Mathf.Min(requested, amount);
        amount -= taken;
        UpdateVisual();
        return taken;
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): Guided "help the grass recover" (+points of the patch's max), capped at full.
    public void Restore(float pointsOfHundred)
    {
        amount = Mathf.Min(MaxAmount, amount + MaxAmount * pointsOfHundred / 100f);
        UpdateVisual();
    }

    /// <summary>Average fullness of every grass (non-water) patch, 0-100. The "Vegetation %" the research uses.</summary>
    public static float VegetationPercent()
    {
        float sum = 0f; int n = 0;
        foreach (var p in All)
        {
            if (p == null || p.IsWater || p.resource == null) continue;
            sum += p.MaxAmount > 0f ? p.amount / p.MaxAmount : 0f; n++;
        }
        return n > 0 ? sum / n * 100f : 0f;
    }

    void UpdateVisual()
    {
        float fill = MaxAmount > 0f ? amount / MaxAmount : 0f;
        visual.localScale = fullScale * Mathf.Lerp(emptyScale, 1f, fill);
    }

    // Nearest edible patch of one of the wanted resources within radius (radius <= 0 = unlimited).
    public static ResourcePatch FindNearest(Vector3 from, IList<ResourceDefinition> wanted, float radius, System.Func<ResourcePatch, bool> skip = null)  // [AI-GENERATED] Claude Code (Claude Opus 5.5): skip = e.g. unreachable
    {
        ResourcePatch best = null;
        float bestSqr = radius > 0f ? radius * radius : float.PositiveInfinity;

        foreach (var patch in All)
        {
            if (patch == null || !patch.IsEdible || !wanted.Contains(patch.resource) || (skip != null && skip(patch))) continue;

            float sqr = (patch.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = patch;
            }
        }
        return best;
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // Like FindNearest, but each patch's distance gets extraCost(patch position) added, so an animal can
    // prefer a farther patch over a near one it thinks is dangerous. The radius still uses the real distance.
    public static ResourcePatch FindBest(Vector3 from, IList<ResourceDefinition> wanted, float radius, System.Func<Vector3, float> extraCost, System.Func<ResourcePatch, bool> skip = null)
    {
        if (extraCost == null) return FindNearest(from, wanted, radius, skip);

        ResourcePatch best = null;
        float bestCost = float.PositiveInfinity;
        float radiusSqr = radius > 0f ? radius * radius : float.PositiveInfinity;

        foreach (var patch in All)
        {
            if (patch == null || !patch.IsEdible || !wanted.Contains(patch.resource) || (skip != null && skip(patch))) continue;

            Vector3 p = patch.transform.position;
            float sqr = (p - from).sqrMagnitude;
            if (sqr > radiusSqr) continue;

            float cost = Mathf.Sqrt(sqr) + extraCost(p);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = patch;
            }
        }
        return best;
    }
    // [AI-GENERATED END]

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();
}
