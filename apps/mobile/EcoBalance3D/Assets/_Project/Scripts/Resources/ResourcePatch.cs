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

    public void Configure(ResourceDefinition value)
    {
        resource = value;
        amount = MaxAmount * startFill;
        UpdateVisual();
    }
    // [AI-GENERATED END]

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (amount >= MaxAmount) return;
        amount = Mathf.Min(MaxAmount, amount + regrowPerSecond * Time.deltaTime);
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

    void UpdateVisual()
    {
        float fill = MaxAmount > 0f ? amount / MaxAmount : 0f;
        visual.localScale = fullScale * Mathf.Lerp(emptyScale, 1f, fill);
    }

    // Nearest edible patch of one of the wanted resources within radius (radius <= 0 = unlimited).
    public static ResourcePatch FindNearest(Vector3 from, IList<ResourceDefinition> wanted, float radius)
    {
        ResourcePatch best = null;
        float bestSqr = radius > 0f ? radius * radius : float.PositiveInfinity;

        foreach (var patch in All)
        {
            if (patch == null || !patch.IsEdible || !wanted.Contains(patch.resource)) continue;

            float sqr = (patch.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = patch;
            }
        }
        return best;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();
}
