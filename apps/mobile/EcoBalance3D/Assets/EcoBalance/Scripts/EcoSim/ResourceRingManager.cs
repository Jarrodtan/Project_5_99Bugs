// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quiet static ground rings under every ResourcePatch: soft blue for water, soft green for edible food.
/// Eaten-out food hides its ring until it regrows. Display only — no pulse (too distracting on a map full of patches).
/// </summary>
public class ResourceRingManager : MonoBehaviour
{
    public static ResourceRingManager Instance { get; private set; }

    const float BaseRadius = 2.2f;
    const float RingWidth = 0.35f;
    const float HeightOffset = 0.15f;
    const int Segments = 40;
    const string HintPrefKey = "EcoBalance.RingHint";
    const string HintText = "Blue rings = water, green rings = food.";

    // Low alpha so the map stays readable; colour is enough to tell water from food.
    static readonly Color WaterColor = new Color(0.3f, 0.55f, 0.9f, 0.4f);
    static readonly Color FoodColor = new Color(0.35f, 0.7f, 0.35f, 0.35f);

    readonly Dictionary<ResourcePatch, LineRenderer> rings = new Dictionary<ResourcePatch, LineRenderer>(128);
    readonly HashSet<ResourcePatch> livePatches = new HashSet<ResourcePatch>();
    readonly List<ResourcePatch> removeBuffer = new List<ResourcePatch>(32);
    readonly Vector3[] circleScratch = new Vector3[Segments];

    Material waterMaterial;
    Material foodMaterial;
    bool hintDone;
    bool hintQueued;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[ResourceRingManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<ResourceRingManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        hintDone = PlayerPrefs.GetInt(HintPrefKey, 0) == 1;
        EnsureMaterial();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
        ClearAllRings();
        if (waterMaterial != null) Destroy(waterMaterial);
        if (foodMaterial != null) Destroy(foodMaterial);
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        ClearAllRings();
        // Prefer once per install; still retry announce if mascot was busy earlier in this run.
        hintDone = PlayerPrefs.GetInt(HintPrefKey, 0) == 1;
        hintQueued = false;
    }

    void LateUpdate()
    {
        SyncRings();
        UpdateRings();
        TryAnnounceHint();
    }

    void EnsureMaterial()
    {
        if (waterMaterial != null) return;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        waterMaterial = MakeTinted(shader, WaterColor);
        foodMaterial = MakeTinted(shader, FoodColor);
    }

    static Material MakeTinted(Shader shader, Color color)
    {
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }

    void SyncRings()
    {
        livePatches.Clear();
        var all = ResourcePatch.All;
        for (int i = 0; i < all.Count; i++)
        {
            var patch = all[i];
            if (patch == null) continue;
            livePatches.Add(patch);
            if (!rings.ContainsKey(patch))
                rings[patch] = CreateRing(patch);
        }

        removeBuffer.Clear();
        foreach (var kv in rings)
        {
            if (kv.Key == null || !livePatches.Contains(kv.Key))
                removeBuffer.Add(kv.Key);
        }
        for (int i = 0; i < removeBuffer.Count; i++)
        {
            var key = removeBuffer[i];
            if (rings.TryGetValue(key, out var lr) && lr != null)
                Destroy(lr.gameObject);
            rings.Remove(key);
        }
    }

    LineRenderer CreateRing(ResourcePatch patch)
    {
        var go = new GameObject("ResourceRing");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = Segments;
        lr.widthMultiplier = RingWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 2;
        lr.sharedMaterial = foodMaterial;
        lr.startColor = lr.endColor = FoodColor;
        // Patches never move: build the circle once here, not every frame (on the 10x map that was
        // about 1,000 rings x 40 points rebuilt each frame, the main cause of the lag).
        Vector3 centre = patch.transform.position;
        centre.y += HeightOffset;
        for (int i = 0; i < Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            circleScratch[i] = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * BaseRadius;
        }
        lr.SetPositions(circleScratch);
        return lr;
    }

    void UpdateRings()
    {
        foreach (var kv in rings)
        {
            var patch = kv.Key;
            var lr = kv.Value;
            if (patch == null || lr == null) continue;

            bool water = patch.IsWater;
            bool show = water || patch.IsEdible;
            if (!show)
            {
                if (lr.enabled) lr.enabled = false;
                continue;
            }
            if (!lr.enabled) lr.enabled = true;

            Material want = water ? waterMaterial : foodMaterial;
            if (lr.sharedMaterial != want)
            {
                lr.sharedMaterial = want;
                lr.startColor = lr.endColor = water ? WaterColor : FoodColor;
            }
        }
    }

    void TryAnnounceHint()
    {
        if (hintDone || hintQueued) return;
        if (rings.Count == 0) return;
        if (Mascot.Announce(HintText))
        {
            PlayerPrefs.SetInt(HintPrefKey, 1);
            PlayerPrefs.Save();
            hintDone = true;
            hintQueued = true;
        }
        // Announce returns false when no mascot: keep trying until one exists.
    }

    void ClearAllRings()
    {
        foreach (var kv in rings)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        rings.Clear();
    }
}
