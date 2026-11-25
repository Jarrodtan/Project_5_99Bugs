// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attaches ThoughtBubble to every animal at runtime and picks who may show one
/// (followed family + nearby, max 8). Also draws the dotted destination line for
/// the followed animal. Display only — never writes AnimalBrain state.
/// </summary>
public class ThoughtBubbleManager : MonoBehaviour
{
    public static ThoughtBubbleManager Instance { get; private set; }

    const int MaxBubbles = 8;
    const float NearbyRadius = 25f;
    const float FamilyRadius = 30f;
    const float PathWidth = 0.15f;
    const float PathEndEpsilon = 0.5f;

    static readonly Color PathFood = new Color(0.25f, 0.75f, 0.3f, 0.9f);
    static readonly Color PathWater = new Color(0.25f, 0.55f, 0.95f, 0.9f);
    static readonly Color PathHunt = new Color(0.9f, 0.2f, 0.2f, 0.9f);

    TerrainCameraController camController;
    readonly List<ThoughtBubble> bubbles = new List<ThoughtBubble>(64);
    readonly Dictionary<AnimalBrain, ThoughtBubble> byBrain = new Dictionary<AnimalBrain, ThoughtBubble>(64);
    readonly List<(AnimalBrain brain, float distSqr)> candidates = new List<(AnimalBrain, float)>(64);
    readonly HashSet<AnimalBrain> visibleSet = new HashSet<AnimalBrain>();
    static readonly Comparison<(AnimalBrain brain, float distSqr)> DistComparer =
        (a, b) => a.distSqr.CompareTo(b.distSqr);

    LineRenderer pathLine;
    Texture2D dashTexture;
    Material pathMaterial;
    Vector3 lastPathStart;
    Vector3 lastPathEnd;
    Color lastPathColor;
    bool pathShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[ThoughtBubbleManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<ThoughtBubbleManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsurePathLine();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
        if (pathMaterial != null) Destroy(pathMaterial);
        if (dashTexture != null) Destroy(dashTexture);
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        camController = null;
        bubbles.Clear();
        byBrain.Clear();
        HidePath();
    }

    void LateUpdate()
    {
        if (camController == null)
            camController = FindAnyObjectByType<TerrainCameraController>();

        EnsureBubbles();
        UpdateVisibility();
        UpdatePathLine();
    }

    void EnsureBubbles()
    {
        var all = AnimalBrain.All;
        for (int i = 0; i < all.Count; i++)
        {
            var brain = all[i];
            if (brain == null || brain.IsCaught) continue;
            if (byBrain.TryGetValue(brain, out var existing) && existing != null) continue;

            var go = new GameObject("ThoughtBubble");
            go.transform.SetParent(brain.transform, false);
            var bubble = go.AddComponent<ThoughtBubble>();
            bubble.Bind(brain);
            byBrain[brain] = bubble;
            bubbles.Add(bubble);
        }

        for (int i = bubbles.Count - 1; i >= 0; i--)
        {
            var bubble = bubbles[i];
            if (bubble == null)
            {
                bubbles.RemoveAt(i);
                continue;
            }
            var owner = bubble.Brain;
            if (owner == null || owner.IsCaught)
            {
                if (owner != null) byBrain.Remove(owner);
                Destroy(bubble.gameObject);
                bubbles.RemoveAt(i);
            }
        }
    }

    void UpdateVisibility()
    {
        visibleSet.Clear();
        candidates.Clear();

        AnimalBrain followed = ResolveFollowedBrain();
        var cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        float nearbySqr = NearbyRadius * NearbyRadius;
        float familySqr = FamilyRadius * FamilyRadius;

        if (followed != null && !followed.IsCaught && AnimalThought.GetKind(followed) != AnimalThought.Kind.None)
            visibleSet.Add(followed);

        // Family of the followed animal (same species nearby, or FamilySource list).
        if (followed != null && !followed.IsCaught)
        {
            bool usedFamilySource = false;
            var familySource = CameraModeController.FamilySource;
            if (familySource != null)
            {
                var list = familySource();
                if (list != null && list.Count > 0)
                {
                    bool containsFollowed = false;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] == null) continue;
                        var b = list[i].GetComponentInChildren<AnimalBrain>();
                        if (b == followed) { containsFollowed = true; break; }
                    }
                    if (containsFollowed)
                    {
                        usedFamilySource = true;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i] == null) continue;
                            var b = list[i].GetComponentInChildren<AnimalBrain>();
                            if (b == null || b.IsCaught) continue;
                            if (AnimalThought.GetKind(b) == AnimalThought.Kind.None) continue;
                            visibleSet.Add(b);
                        }
                    }
                }
            }

            if (!usedFamilySource)
            {
                Vector3 fp = followed.transform.position;
                var all = AnimalBrain.All;
                for (int i = 0; i < all.Count; i++)
                {
                    var b = all[i];
                    if (b == null || b == followed || b.IsCaught) continue;
                    if (b.species_data != followed.species_data) continue;
                    float d = (b.transform.position - fp).sqrMagnitude;
                    if (d > familySqr) continue;
                    if (AnimalThought.GetKind(b) == AnimalThought.Kind.None) continue;
                    visibleSet.Add(b);
                }
            }
        }

        // Nearby camera animals fill remaining slots, sorted by distance.
        if (cam != null)
        {
            var all = AnimalBrain.All;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (b == null || b.IsCaught) continue;
                if (visibleSet.Contains(b)) continue;
                if (AnimalThought.GetKind(b) == AnimalThought.Kind.None) continue;
                float d = (b.transform.position - camPos).sqrMagnitude;
                if (d > nearbySqr) continue;
                candidates.Add((b, d));
            }
            candidates.Sort(DistComparer);
        }

        // Cap: always keep followed, then family (already in set), then nearest.
        // If family alone exceeds MaxBubbles, trim by distance from followed / camera.
        if (visibleSet.Count > MaxBubbles)
            TrimVisibleSet(followed, camPos);

        for (int i = 0; i < candidates.Count && visibleSet.Count < MaxBubbles; i++)
            visibleSet.Add(candidates[i].brain);

        for (int i = 0; i < bubbles.Count; i++)
        {
            var bubble = bubbles[i];
            if (bubble == null || bubble.Brain == null) continue;
            bubble.SetWantVisible(visibleSet.Contains(bubble.Brain));
        }
    }

    void TrimVisibleSet(AnimalBrain followed, Vector3 camPos)
    {
        candidates.Clear();
        foreach (var b in visibleSet)
        {
            if (b == null || b == followed) continue;
            float d = followed != null
                ? (b.transform.position - followed.transform.position).sqrMagnitude
                : (b.transform.position - camPos).sqrMagnitude;
            candidates.Add((b, d));
        }
        candidates.Sort(DistComparer);
        visibleSet.Clear();
        if (followed != null) visibleSet.Add(followed);
        for (int i = 0; i < candidates.Count && visibleSet.Count < MaxBubbles; i++)
            visibleSet.Add(candidates[i].brain);
    }

    AnimalBrain ResolveFollowedBrain()
    {
        if (camController == null) return null;
        var target = camController.FollowTarget;
        if (target != null)
        {
            var brain = target.GetComponent<AnimalBrain>()
                     ?? target.GetComponentInChildren<AnimalBrain>()
                     ?? target.GetComponentInParent<AnimalBrain>();
            if (brain != null) return brain;
        }
        if (AnimalInfoUI.Instance != null) return AnimalInfoUI.Instance.SelectedAnimal;
        return null;
    }

    // ---------- Dotted destination path ----------

    void EnsurePathLine()
    {
        if (pathLine != null) return;
        var go = new GameObject("ThoughtPathLine");
        go.transform.SetParent(transform, false);
        pathLine = go.AddComponent<LineRenderer>();
        pathLine.positionCount = 2;
        pathLine.useWorldSpace = true;
        pathLine.widthMultiplier = PathWidth;
        pathLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathLine.receiveShadows = false;
        pathLine.textureMode = LineTextureMode.Tile;
        pathLine.alignment = LineAlignment.View;
        pathLine.numCapVertices = 2;

        dashTexture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        for (int x = 0; x < 8; x++)
            dashTexture.SetPixel(x, 0, x < 4 ? Color.white : Color.clear);
        dashTexture.Apply(false, true);

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        pathMaterial = new Material(shader);
        if (pathMaterial.HasProperty("_BaseMap")) pathMaterial.SetTexture("_BaseMap", dashTexture);
        if (pathMaterial.HasProperty("_MainTex")) pathMaterial.SetTexture("_MainTex", dashTexture);
        pathLine.material = pathMaterial;
        pathLine.enabled = false;
    }

    void UpdatePathLine()
    {
        if (pathLine == null) EnsurePathLine();

        AnimalBrain followed = ResolveFollowedBrain();
        if (followed == null || followed.IsCaught)
        {
            HidePath();
            return;
        }

        var action = followed.CurrentAction;
        bool actionable = action == AnimalBrain.AnimalAction.EatingGrass
                       || action == AnimalBrain.AnimalAction.DrinkingWater
                       || action == AnimalBrain.AnimalAction.Hunting;
        if (!actionable || !followed.TryGetDestination(out Vector3 dest))
        {
            HidePath();
            return;
        }

        Vector3 start = followed.transform.position;
        start.y += 0.3f;
        dest.y += 0.3f;
        Vector3 flat = dest - start;
        flat.y = 0f;
        if (flat.sqrMagnitude < PathEndEpsilon * PathEndEpsilon)
        {
            HidePath();
            return;
        }

        Color color = action == AnimalBrain.AnimalAction.DrinkingWater ? PathWater
                    : action == AnimalBrain.AnimalAction.Hunting ? PathHunt
                    : PathFood;

        if (!pathShown ||
            (start - lastPathStart).sqrMagnitude > 0.01f ||
            (dest - lastPathEnd).sqrMagnitude > 0.01f ||
            color != lastPathColor)
        {
            pathLine.SetPosition(0, start);
            pathLine.SetPosition(1, dest);
            pathLine.startColor = pathLine.endColor = color;
            if (pathMaterial != null)
            {
                if (pathMaterial.HasProperty("_BaseColor")) pathMaterial.SetColor("_BaseColor", color);
                if (pathMaterial.HasProperty("_Color")) pathMaterial.SetColor("_Color", color);
            }
            float len = Vector3.Distance(start, dest);
            if (pathMaterial != null)
                pathMaterial.mainTextureScale = new Vector2(Mathf.Max(1f, len / 1.2f), 1f);
            lastPathStart = start;
            lastPathEnd = dest;
            lastPathColor = color;
        }

        if (!pathLine.enabled) pathLine.enabled = true;
        pathShown = true;
    }

    void HidePath()
    {
        if (pathLine != null && pathLine.enabled) pathLine.enabled = false;
        pathShown = false;
    }
}
