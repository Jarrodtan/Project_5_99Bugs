// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - SCRUM-380
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows who the family are: a name tag above every living family member (the main animal's is gold with a star)
/// and a gold ring on the ground under the main animal, so it can be found from far away.
/// Added by <see cref="FamilyManager"/>.
/// </summary>
public class FamilyMarkers : MonoBehaviour
{
    [Header("Name tags")]
    public float tagFontSize = 30f;
    [Tooltip("Background of the main animal's tag (others use Tag Background).")]
    public Color mainTagColor = new Color(1f, 0.82f, 0.25f, 0.95f);
    public Color mainTagText = new Color(0.12f, 0.1f, 0.05f);
    public Color tagColor = Color.white;
    public Color tagBackground = new Color(0.08f, 0.1f, 0.14f, 0.7f);
    [Tooltip("Screen pixels (at 1920x1200) between the top of the animal and its tag.")]
    public float tagGap = 18f;

    [Header("Main animal ring")]
    public Color ringColor = new Color(1f, 0.82f, 0.25f, 0.9f);
    [Tooltip("Ring size compared with the animal's footprint.")]
    public float ringScale = 1.4f;
    public float ringMinRadius = 0.6f;

    Canvas canvas;
    CanvasScaler scaler;
    readonly Dictionary<FamilyMember, (RectTransform box, TextMeshProUGUI text)> tags = new Dictionary<FamilyMember, (RectTransform, TextMeshProUGUI)>();
    readonly List<FamilyMember> stale = new List<FamilyMember>();
    LineRenderer ring;

    void Start()
    {
        var go = new GameObject("Family Name Tags", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;   // under the HUD, cards and popups
        scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void LateUpdate()
    {
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): no name tags or ring before Start (they flashed on load).
        bool hide = GuidedChallenge.IntroPending;
        if (canvas != null && canvas.enabled == hide) canvas.enabled = !hide;
        if (ring != null && ring.enabled && hide) ring.enabled = false;
        if (hide) return;
        var cam = Camera.main;
        UpdateTags(cam);
        UpdateRing();
    }

    // ---------- Name tags ----------

    void UpdateTags(Camera cam)
    {
        var members = FamilyManager.Members;

        // Tags of members who died or were removed go.
        stale.Clear();
        foreach (var kv in tags)
            if (kv.Key == null || !kv.Key.IsAlive || !Contains(members, kv.Key)) stale.Add(kv.Key);
        foreach (var m in stale)
        {
            if (tags[m].box != null) Destroy(tags[m].box.gameObject);
            tags.Remove(m);
        }

        float uiScale = canvas != null ? canvas.scaleFactor : 1f;
        placed.Clear();
        // The main animal first, so its tag stays on the animal and the others move out of its way.
        for (int pass = 0; pass < 2; pass++)
            foreach (var m in members)
            {
                if (m == null || !m.IsAlive || m.IsMain != (pass == 0)) continue;
                if (!tags.TryGetValue(m, out var tag)) { tag = MakeTag(m); tags[m] = tag; }

                bool show = cam != null;
                if (show)
                {
                    Vector3 screen = cam.WorldToScreenPoint(TopOf(m));
                    show = screen.z > 0f;   // in front of the camera
                    if (show) Place(tag.box, new Vector2(screen.x, screen.y + tagGap * uiScale), uiScale);
                }
                if (tag.box.gameObject.activeSelf != show) tag.box.gameObject.SetActive(show);
            }
    }

    readonly List<Rect> placed = new List<Rect>();

    // Family members stand close together: a tag that would cover one already placed moves up above it.
    void Place(RectTransform box, Vector2 bottomCentre, float uiScale)
    {
        Vector2 size = box.rect.size * uiScale;
        var r = new Rect(bottomCentre.x - size.x * 0.5f, bottomCentre.y, size.x, size.y);
        for (int tries = 0; tries < 6; tries++)
        {
            bool moved = false;
            foreach (var other in placed)
                if (r.Overlaps(other)) { r.y = other.yMax + 4f * uiScale; moved = true; }
            if (!moved) break;
        }
        placed.Add(r);
        box.position = new Vector3(r.center.x, r.y, 0f);
    }

    (RectTransform, TextMeshProUGUI) MakeTag(FamilyMember m)
    {
        var box = new GameObject($"Tag {m.DisplayName}", typeof(RectTransform), typeof(Image),
                                 typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        box.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)box.transform;
        rt.pivot = new Vector2(0.5f, 0f);   // sits on top of the point above the animal
        var bg = box.GetComponent<Image>();
        bg.color = m.IsMain ? mainTagColor : tagBackground;   // the main animal's tag is gold
        bg.raycastTarget = false;
        var layout = box.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 4, 4);
        layout.childControlWidth = layout.childControlHeight = true;
        var fit = box.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var text = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(box.transform, false);
        text.text = m.DisplayName;   // (no star: the game font has no ★ glyph)
        text.fontSize = m.IsMain ? tagFontSize * 1.2f : tagFontSize;
        text.fontStyle = m.IsMain ? FontStyles.Bold : FontStyles.Normal;
        text.color = m.IsMain ? mainTagText : tagColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        if (m.IsMain) box.transform.SetAsLastSibling();   // drawn over the others when they overlap
        return (rt, text);
    }

    static bool Contains(IReadOnlyList<FamilyMember> list, FamilyMember m)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == m) return true;
        return false;
    }

    // Top of the animal's model (it grows, so measured every frame).
    static Vector3 TopOf(FamilyMember m)
    {
        var renderers = m.GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds b = default;
        foreach (var r in renderers)
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        }
        return found ? new Vector3(b.center.x, b.max.y, b.center.z) : m.transform.position + Vector3.up;
    }

    // ---------- Ring under the main animal ----------

    void UpdateRing()
    {
        var main = FamilyManager.MainAnimal;
        if (ring == null)
        {
            var go = new GameObject("Main Animal Ring");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 40;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ring.material = new Material(shader);
            ring.material.color = ringColor;
            ring.startColor = ring.endColor = ringColor;
        }
        ring.enabled = main != null;
        if (main == null) return;

        // Footprint of the model, so the ring grows with the lamb / cub.
        float radius = ringMinRadius;
        foreach (var r in main.GetComponentsInChildren<Renderer>())
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                radius = Mathf.Max(radius, Mathf.Max(r.bounds.extents.x, r.bounds.extents.z) * ringScale);
        ring.widthMultiplier = Mathf.Clamp(radius * 0.12f, 0.06f, 0.3f);

        Terrain.GetActiveTerrains(terrains);
        Vector3 centre = main.transform.position;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i / (float)ring.positionCount * Mathf.PI * 2f;
            var p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            p.y = GroundHeight(p, centre.y) + 0.08f;   // hug the ground so it doesn't float on slopes
            ring.SetPosition(i, p);
        }
    }

    readonly List<Terrain> terrains = new List<Terrain>();

    float GroundHeight(Vector3 p, float fallback)
    {
        foreach (var t in terrains)
        {
            if (t == null || t.terrainData == null) continue;
            Vector3 o = t.GetPosition(), size = t.terrainData.size;
            if (p.x < o.x || p.z < o.z || p.x > o.x + size.x || p.z > o.z + size.z) continue;
            return o.y + t.SampleHeight(p);
        }
        return fallback;
    }

    void OnDestroy()
    {
        if (ring != null && ring.material != null) Destroy(ring.material);
    }
}
