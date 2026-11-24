// [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
using TMPro;
using UnityEngine;

/// <summary>
/// World-space thought icon above one animal. Billboarded like HeartPop.
/// Visibility and max-count are decided by ThoughtBubbleManager; this only draws.
/// </summary>
public class ThoughtBubble : MonoBehaviour
{
    const float FadeSeconds = 0.3f;
    const float HeadPadding = 0.35f;
    const float BaseScale = 1f;
    const float DistScaleDivisor = 18f;
    const float MinScale = 0.6f;
    const float MaxScale = 4f;

    static readonly Color ColorScared = new Color(0.95f, 0.25f, 0.2f, 1f);
    static readonly Color ColorWater = new Color(0.25f, 0.55f, 0.95f, 1f);
    static readonly Color ColorGrass = new Color(0.25f, 0.7f, 0.3f, 1f);
    static readonly Color ColorMeat = new Color(0.85f, 0.2f, 0.2f, 1f);
    static readonly Color ColorHeart = new Color(1f, 0.35f, 0.55f, 1f);
    static readonly Color BubbleWhite = new Color(1f, 1f, 1f, 0.92f);

    static Sprite sharedBubbleSprite;

    AnimalBrain brain;
    TextMeshPro text;
    SpriteRenderer bubbleBg;
    SphereCollider tapCollider;
    Renderer cachedRenderer;
    AnimalThought.Kind shownKind = AnimalThought.Kind.None;
    float alpha;
    bool wantVisible;
    float headHeight = 1.5f;

    public AnimalBrain Brain => brain;
    public AnimalThought.Kind CurrentKind => shownKind;

    public void Bind(AnimalBrain owner)
    {
        brain = owner;
        if (brain == null) return;
        cachedRenderer = brain.GetComponentInChildren<Renderer>();
        RefreshHeadHeight();
    }

    public void SetWantVisible(bool visible) => wantVisible = visible;

    void Awake()
    {
        var bgGo = new GameObject("BubbleBg");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localPosition = Vector3.zero;
        bgGo.transform.localScale = Vector3.one * 0.55f;
        bubbleBg = bgGo.AddComponent<SpriteRenderer>();
        bubbleBg.sprite = SharedBubbleSprite();
        bubbleBg.color = new Color(BubbleWhite.r, BubbleWhite.g, BubbleWhite.b, 0f);
        bubbleBg.sortingOrder = 50;

        text = gameObject.AddComponent<TextMeshPro>();
        text.text = "";
        text.fontSize = 8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.rectTransform.sizeDelta = new Vector2(1.6f, 1.6f);
        text.sortingOrder = 51;

        // So TerrainCameraController.Tap can hit this bubble and open the animal card.
        tapCollider = gameObject.AddComponent<SphereCollider>();
        tapCollider.isTrigger = true;
        tapCollider.radius = 0.7f;
        tapCollider.center = Vector3.zero;
    }

    void LateUpdate()
    {
        if (brain == null)
        {
            Destroy(gameObject);
            return;
        }

        var kind = AnimalThought.GetKind(brain);
        if (kind != shownKind)
        {
            shownKind = kind;
            ApplyGlyph(kind);
        }

        bool show = wantVisible && kind != AnimalThought.Kind.None && !brain.IsCaught;
        float dt = Time.unscaledDeltaTime;
        if (show) alpha = Mathf.MoveTowards(alpha, 1f, dt / FadeSeconds);
        else alpha = Mathf.MoveTowards(alpha, 0f, dt / FadeSeconds);

        if (tapCollider != null) tapCollider.enabled = show || alpha > 0.01f;

        if (alpha <= 0.001f)
        {
            if (text.enabled) text.enabled = false;
            if (bubbleBg.enabled) bubbleBg.enabled = false;
            return;
        }

        if (!text.enabled) text.enabled = true;
        if (!bubbleBg.enabled) bubbleBg.enabled = true;

        RefreshHeadHeight();
        Vector3 head = brain.transform.position;
        head.y += headHeight;
        transform.position = head;

        var cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            float worldScale = Mathf.Clamp(dist / DistScaleDivisor, MinScale, MaxScale) * BaseScale;
            // Parent animal may be scaled; keep the bubble a stable world size.
            Vector3 parentLossy = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float px = Mathf.Abs(parentLossy.x) < 0.001f ? 1f : parentLossy.x;
            float py = Mathf.Abs(parentLossy.y) < 0.001f ? 1f : parentLossy.y;
            float pz = Mathf.Abs(parentLossy.z) < 0.001f ? 1f : parentLossy.z;
            transform.localScale = new Vector3(worldScale / px, worldScale / py, worldScale / pz);
        }

        Color tc = text.color;
        tc.a = alpha;
        text.color = tc;
        Color bc = bubbleBg.color;
        bc.a = BubbleWhite.a * alpha;
        bubbleBg.color = bc;
    }

    void RefreshHeadHeight()
    {
        if (cachedRenderer == null)
            cachedRenderer = brain != null ? brain.GetComponentInChildren<Renderer>() : null;
        if (cachedRenderer != null)
            headHeight = cachedRenderer.bounds.max.y - brain.transform.position.y + HeadPadding;
        else
            headHeight = 1.5f;
    }

    void ApplyGlyph(AnimalThought.Kind kind)
    {
        switch (kind)
        {
            case AnimalThought.Kind.Scared:
                text.text = "!";
                text.color = new Color(ColorScared.r, ColorScared.g, ColorScared.b, alpha);
                break;
            case AnimalThought.Kind.Water:
                text.text = "~";
                text.color = new Color(ColorWater.r, ColorWater.g, ColorWater.b, alpha);
                break;
            case AnimalThought.Kind.Grass:
                text.text = "*";
                text.color = new Color(ColorGrass.r, ColorGrass.g, ColorGrass.b, alpha);
                break;
            case AnimalThought.Kind.Meat:
                text.text = "#";
                text.color = new Color(ColorMeat.r, ColorMeat.g, ColorMeat.b, alpha);
                break;
            case AnimalThought.Kind.Heart:
                text.text = "♥";
                text.color = new Color(ColorHeart.r, ColorHeart.g, ColorHeart.b, alpha);
                break;
            default:
                text.text = "";
                break;
        }
    }

    static Sprite SharedBubbleSprite()
    {
        if (sharedBubbleSprite != null) return sharedBubbleSprite;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        float r = n / 2f - 1f;
        float inner = r - 3f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - n / 2f;
                float dy = y + 0.5f - n / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = 0;
                if (d <= inner) a = 255;
                else if (d < r) a = (byte)(Mathf.Clamp01(r - d) * 255);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        sharedBubbleSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
        return sharedBubbleSprite;
    }
}
