// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small, cheerful UI effects for children, shared by the ending screen, the Ending Gallery and the badge cards:
/// confetti (or gentle snow for the sad endings), pop-in, count-up numbers, a wiggle for locked cards, and a
/// code-drawn illustration per ending (doc: Ending_Illustration; placeholder art that final artwork can replace).
/// Everything runs on unscaled time, so it also plays while the run is paused.
/// </summary>
public static class EndingCelebration
{
    static readonly Color[] Party =
    {
        new Color32(0xFF, 0xC8, 0x3D, 0xFF), new Color32(0x4C, 0xC9, 0x7A, 0xFF), new Color32(0x3D, 0xA5, 0xFF, 0xFF),
        new Color32(0xFF, 0x6B, 0x8B, 0xFF), new Color32(0xB2, 0x7B, 0xFF, 0xFF), new Color32(0xFF, 0x8F, 0x3D, 0xFF),
    };

    /// <summary>Endings that read as good news get confetti; the others get slow snowflakes (never a "you failed").</summary>
    public static bool IsHappy(int ending) => ending == 3 || ending == 4;

    /// <summary>Confetti only when it is good news for the child too: a thriving / fragile ending with Miloy or Snowy
    /// still alive. If the focal animal died, the ending is gentle snow even when the ecosystem is fine.</summary>
    public static bool Celebrate(GuidedScreens.Result r) => IsHappy(r.ending) && !r.focalDied;

    // ---------- Ending screen ----------

    /// <summary>The ending screen comes alive: illustration, title pop, counting stats, "New ending!" sticker, badges, confetti.</summary>
    public static void Play(Transform root, GuidedScreens.Result result, GuidedEndings.Ending ending)
    {
        if (root == null) return;
        var runner = Runner(root);
        // The space between the subtitle and the three cards: the ending's picture on the left, badges on the right.
        var picture = Panel(root, "EndingPicture", new Vector2(0.06f, 0.465f), new Vector2(0.5f, 0.738f));
        var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
        inner.SetParent(picture, false);
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one; inner.offsetMin = new Vector2(12f, 12f); inner.offsetMax = new Vector2(-12f, -12f);
        inner.GetComponent<Image>().sprite = RoundedSprite.Get(); inner.GetComponent<Image>().type = Image.Type.Sliced;
        inner.GetComponent<Mask>().showMaskGraphic = false;
        Illustrate(inner, result.ending, true);
        runner.StartCoroutine(PopIn(picture, 0.05f));
        var badges = Panel(root, "EndingBadges", new Vector2(0.53f, 0.465f), new Vector2(0.94f, 0.738f));
        runner.StartCoroutine(PopIn(badges, 0.2f));

        // Miloy / Snowy's line is the heart of the screen: big and clear.
        if (GuidedScreens.Find(root, "CompletionSubtitleTxt") is Transform sub && sub.TryGetComponent(out TMP_Text subText))
        {
            subText.enableAutoSizing = true; subText.fontSizeMax = 40f; subText.fontSizeMin = 26f;
            subText.fontStyle = FontStyles.Bold; subText.color = new Color(0.22f, 0.16f, 0.1f);
            subText.textWrappingMode = TextWrappingModes.NoWrap;
            var srt = subText.rectTransform; srt.sizeDelta = new Vector2(Mathf.Max(srt.sizeDelta.x, 1500f), Mathf.Max(srt.sizeDelta.y, 56f));
        }
        var title = GuidedScreens.Find(root, "EndingTitleTxt") as RectTransform;
        if (title != null) runner.StartCoroutine(PopIn(title, 0.15f));
        CountUp(runner, GuidedScreens.Find(GuidedScreens.Find(root, "GrassCard"), "ValueTxt"), result.vegetation, "%", 0.4f);
        CountUp(runner, GuidedScreens.Find(GuidedScreens.Find(root, "AnimalCard01"), "ValueTxt"), result.sheep, "", 0.55f);
        CountUp(runner, GuidedScreens.Find(GuidedScreens.Find(root, "AnimalCard02"), "ValueTxt"), result.leopards, "", 0.7f);
        if (result.isNew && title != null) Sticker(runner, title, "NEW ENDING!", 1.0f);
        BadgeRow(runner, badges, result);
        runner.StartCoroutine(Later(0.3f, () => Burst(root, 120, !Celebrate(result))));
    }

    // The badges panel: a heading, the three medals (earned in colour, new ones pop and sparkle), and the progress.
    static void BadgeRow(MonoBehaviour runner, RectTransform panel, GuidedScreens.Result result)
    {
        var head = Text(panel, "Badges", 40f, new Color(0.22f, 0.16f, 0.1f), FontStyles.Bold);
        Stretch(head.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.97f));
        var row = new GameObject("BadgeRow", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(panel, false);
        Stretch(row, new Vector2(0.03f, 0.3f), new Vector2(0.97f, 0.78f));
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f; h.childAlignment = TextAnchor.MiddleCenter; h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        var fresh = new HashSet<string>(result.newBadges ?? new string[0]);
        int k = 0;
        foreach (var m in GuidedAchievements.All)
        {
            bool got = GuidedAchievements.IsUnlocked(m.id);
            var chip = BadgeChip(row, m, got, 1.05f);
            if (fresh.Contains(m.badge)) { runner.StartCoroutine(PopIn((RectTransform)chip.transform, 1.2f + 0.25f * k++)); Sparkle(runner, (RectTransform)chip.transform); }
        }
        var progress = Text(panel, $"{GuidedAchievements.Progress()}   ·   {GuidedEndings.Progress()}", 26f, new Color(0.42f, 0.33f, 0.2f), FontStyles.Normal);
        progress.name = "EndingProgress";
        Stretch(progress.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.27f));
    }

    // A cream card like Shu Min's, stretched between two corners of the screen (0..1), above the scene's own art.
    static RectTransform Panel(Transform root, string name, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(root, false);
        Stretch(rt, min, max);
        var img = rt.GetComponent<Image>();
        img.sprite = RoundedSprite.Get(); img.type = Image.Type.Sliced;
        img.color = new Color(0.98f, 0.95f, 0.86f);
        img.raycastTarget = false;
        var edge = rt.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(0.45f, 0.32f, 0.18f, 0.9f); edge.effectDistance = new Vector2(3f, -3f);
        return rt;
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>A round medal with the badge's name under it (greyed with a lock when not earned yet).</summary>
    public static GameObject BadgeChip(Transform parent, GuidedAchievements.Mission m, bool got, float size = 1f)
    {
        var chip = new GameObject(m.badge, typeof(RectTransform));
        chip.transform.SetParent(parent, false);
        var le = chip.AddComponent<LayoutElement>(); le.preferredWidth = 260f * size; le.preferredHeight = 116f * size;
        var medal = Child(chip.transform, "Medal", new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(72f, 72f) * size);
        var disc = medal.gameObject.AddComponent<BadgeMedalGraphic>();
        disc.fill = got ? GuidedAchievements.Colour(m.id) : new Color(0.78f, 0.79f, 0.8f);
        disc.ring = got ? new Color32(0xFF, 0xD5, 0x4F, 0xFF) : new Color(0.66f, 0.67f, 0.68f);
        disc.raycastTarget = false;
        var icon = Text(medal, got ? GuidedAchievements.Symbol(m.id) : "?", 40f * size, Color.white, FontStyles.Bold);
        icon.rectTransform.anchorMin = Vector2.zero; icon.rectTransform.anchorMax = Vector2.one; icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
        var label = Text(chip.transform, m.badge, 22f * size, got ? new Color(0.22f, 0.16f, 0.1f) : new Color(0.55f, 0.56f, 0.58f), got ? FontStyles.Bold : FontStyles.Normal);
        var lr = label.rectTransform;
        lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(1f, 0f); lr.pivot = new Vector2(0.5f, 0f);
        lr.sizeDelta = new Vector2(0f, 38f * size); lr.anchoredPosition = Vector2.zero;
        return chip;
    }

    // ---------- Illustration (Ending_Illustration placeholder) ----------

    /// <summary>
    /// Draws the ending's little landscape into <paramref name="frame"/> (its first plain Image, or the frame itself):
    /// sky, sun or clouds, snowy mountains, grass and the animals that are left. Locked endings get a dark silhouette
    /// with a big question mark (doc: locked entries hide the illustration).
    /// </summary>
    public static void Illustrate(Transform frame, int ending, bool unlocked, bool behind = false)
    {
        if (frame == null) return;
        var old = frame.Find("Illustration");
        if (old != null) Object.Destroy(old.gameObject);
        var target = PictureSpot(frame) ?? frame as RectTransform;
        if (target == null) return;
        var rt = new GameObject("Illustration", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(target, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (behind) rt.SetAsFirstSibling();
        var g = rt.gameObject.AddComponent<EndingLandscapeGraphic>();
        g.raycastTarget = false;
        g.Set(ending, unlocked);
        if (!unlocked)
        {
            var q = Text(rt, "?", 90f, new Color(1f, 1f, 1f, 0.85f), FontStyles.Bold);
            q.rectTransform.anchorMin = Vector2.zero; q.rectTransform.anchorMax = Vector2.one; q.rectTransform.offsetMin = q.rectTransform.offsetMax = Vector2.zero;
        }
        else if (behind)
        {
            // On the ending screen the picture sits behind the stat cards, softened so the numbers stay readable.
            g.color = new Color(1f, 1f, 1f, 0.55f);
        }
    }

    // The mock-up's picture box inside a card: an Image with no text and no button under it.
    static RectTransform PictureSpot(Transform card)
    {
        foreach (var img in card.GetComponentsInChildren<Image>(true))
        {
            if (img.transform == card || img.GetComponentInChildren<TMP_Text>(true) != null) continue;
            var n = img.name;
            if (n.StartsWith("Image") || n.Contains("Illustration") || n.Contains("Picture")) return img.rectTransform;
        }
        return null;
    }

    // ---------- Little animations ----------

    public static void Wiggle(RectTransform rt)
    {
        if (rt == null) return;
        Runner(rt).StartCoroutine(WiggleRoutine(rt));
        UIClickSound.PlayClick();
    }

    static IEnumerator WiggleRoutine(RectTransform rt)
    {
        var start = rt.localRotation;
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            if (rt == null) yield break;
            rt.localRotation = start * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * 6f * (1f - t / 0.45f));
            yield return null;
        }
        if (rt != null) rt.localRotation = start;
    }

    public static IEnumerator PopIn(RectTransform rt, float delay)
    {
        if (rt == null) yield break;
        var full = rt.localScale;
        rt.localScale = Vector3.zero;
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            if (rt == null) yield break;
            float k = t / 0.5f;
            float s = 1f + Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 0.35f;   // overshoot and settle
            rt.localScale = full * Mathf.Min(1f, k * 3f) * s;
            yield return null;
        }
        if (rt != null) rt.localScale = full;
    }

    static void CountUp(MonoBehaviour runner, Transform t, float to, string suffix, float delay)
    {
        if (t == null || !t.TryGetComponent(out TMP_Text text)) return;
        runner.StartCoroutine(CountRoutine(text, to, suffix, delay));
    }

    static IEnumerator CountRoutine(TMP_Text text, float to, string suffix, float delay)
    {
        text.text = "0" + suffix;
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        const float D = 0.9f;
        for (float t = 0f; t < D; t += Time.unscaledDeltaTime)
        {
            if (text == null) yield break;
            float k = 1f - Mathf.Pow(1f - t / D, 3f);
            text.text = Mathf.RoundToInt(to * k) + suffix;
            yield return null;
        }
        if (text != null) text.text = Mathf.RoundToInt(to) + suffix;
    }

    static void Sticker(MonoBehaviour runner, RectTransform near, string text, float delay)
    {
        var box = new GameObject("NewSticker", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        box.SetParent(near.parent, false);
        box.anchorMin = box.anchorMax = near.anchorMin;
        box.pivot = new Vector2(0.5f, 0.5f);
        box.anchoredPosition = near.anchoredPosition + new Vector2(near.rect.width * 0.5f - 40f, near.rect.height * 0.5f + 6f);
        box.sizeDelta = new Vector2(250f, 64f);
        box.localRotation = Quaternion.Euler(0f, 0f, -8f);
        var img = box.GetComponent<Image>();
        img.color = new Color32(0xFF, 0x5A, 0x7A, 0xFF);
        img.raycastTarget = false;
        var t = Text(box, text, 30f, Color.white, FontStyles.Bold);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        runner.StartCoroutine(PopIn(box, delay));
        runner.StartCoroutine(Pulse(box, delay + 0.5f));
    }

    static IEnumerator Pulse(RectTransform rt, float delay)
    {
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        var full = rt != null ? rt.localScale : Vector3.one;
        for (float t = 0f; rt != null; t += Time.unscaledDeltaTime)
        {
            rt.localScale = full * (1f + 0.05f * Mathf.Sin(t * 5f));
            yield return null;
        }
    }

    /// <summary>Twinkling stars around a new badge.</summary>
    public static void Sparkle(MonoBehaviour runner, RectTransform around)
    {
        for (int i = 0; i < 6; i++) runner.StartCoroutine(Twinkle(around, i));
    }

    static IEnumerator Twinkle(RectTransform around, int i)
    {
        var star = Text(around, "*", 46f, Party[i % Party.Length], FontStyles.Bold);
        var rt = star.rectTransform;
        rt.sizeDelta = new Vector2(50f, 50f);
        float angle = i * 60f * Mathf.Deg2Rad;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f);
        rt.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 70f;
        for (float t = -i * 0.12f; star != null; t += Time.unscaledDeltaTime)
        {
            float s = Mathf.Max(0f, Mathf.Sin(t * 4f));
            rt.localScale = Vector3.one * s;
            yield return null;
        }
    }

    static IEnumerator Later(float delay, System.Action action)
    {
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        action();
    }

    // ---------- Confetti / snow ----------

    /// <summary>A burst of confetti over <paramref name="root"/>'s canvas; <paramref name="snow"/> = slow, quiet snowflakes instead.</summary>
    public static void Burst(Transform root, int count, bool snow = false)
    {
        if (root == null) return;
        var canvas = root.GetComponentInParent<Canvas>();
        var parent = canvas != null ? canvas.rootCanvas.transform : root;
        var layer = new GameObject(snow ? "Snowfall" : "Confetti", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(parent, false);
        layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one; layer.offsetMin = layer.offsetMax = Vector2.zero;
        layer.SetAsLastSibling();
        var c = layer.gameObject.AddComponent<Canvas>();   // above everything on that screen, never catching taps
        c.overrideSorting = true;
        c.sortingOrder = 200;
        layer.gameObject.AddComponent<ConfettiLayer>().Begin(snow ? 60 : count, snow, Party);
    }

    // ---------- Helpers ----------

    static MonoBehaviour Runner(Transform t)
    {
        var root = t.root;
        return root.GetComponent<CelebrationRunner>() ?? root.gameObject.AddComponent<CelebrationRunner>();
    }

    static RectTransform Child(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    internal static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, FontStyles style)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var font = ScreenFlow.ShuminFont("Chewy");
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Min(size, 18f);
        t.raycastTarget = false;
        return t;
    }
}

/// <summary>Hosts the coroutines of <see cref="EndingCelebration"/> (on the screen's root, so they stop with it).</summary>
public class CelebrationRunner : MonoBehaviour { }

/// <summary>Falling confetti pieces (or snowflakes): one UI mesh, updated every frame, gone after a few seconds.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ConfettiLayer : MaskableGraphic
{
    struct Piece { public Vector2 pos, vel; public float rot, spin, size, wobble; public Color color; }
    readonly List<Piece> pieces = new List<Piece>();
    bool snow;
    float age, life;

    public void Begin(int count, bool isSnow, Color[] palette)
    {
        snow = isSnow;
        raycastTarget = false;
        life = snow ? 7f : 4.5f;
        var r = rectTransform.rect;
        for (int i = 0; i < count; i++)
        {
            var p = new Piece
            {
                size = snow ? Random.Range(12f, 24f) : Random.Range(14f, 26f),
                rot = Random.Range(0f, 360f),
                spin = Random.Range(-360f, 360f),
                wobble = Random.Range(0f, 10f),
                color = snow ? Color.Lerp(new Color(0.55f, 0.74f, 0.93f), new Color(0.8f, 0.9f, 1f), Random.value) * new Color(1f, 1f, 1f, Random.Range(0.75f, 0.95f)) /* soft blue: shows on the cream screens */ : palette[i % palette.Length],
            };
            if (snow) { p.pos = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(0f, r.height)); p.vel = new Vector2(0f, -Random.Range(60f, 120f)); }
            else
            {
                // Two cannons from the bottom corners, aimed up and in.
                bool left = i % 2 == 0;
                p.pos = new Vector2(left ? r.xMin + 60f : r.xMax - 60f, r.yMin + 40f);
                float ang = Random.Range(55f, 80f) * Mathf.Deg2Rad;
                float speed = Random.Range(900f, 1500f);
                p.vel = new Vector2(Mathf.Cos(ang) * speed * (left ? 1f : -1f), Mathf.Sin(ang) * speed);
            }
            pieces.Add(p);
        }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        age += dt;
        if (age > life) { Destroy(gameObject); return; }
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (snow) p.pos += new Vector2(Mathf.Sin(age * 1.5f + p.wobble) * 30f * dt, p.vel.y * dt);
            else
            {
                p.vel.y -= 1300f * dt;                       // gravity
                p.vel *= 1f - 0.9f * dt;                       // air
                p.vel.y = Mathf.Max(p.vel.y, -260f);           // flutter down slowly
                p.pos += p.vel * dt + new Vector2(Mathf.Sin(age * 6f + p.wobble) * 40f * dt, 0f);
            }
            p.rot += p.spin * dt;
            pieces[i] = p;
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float fade = Mathf.Clamp01((life - age) / 1f);
        foreach (var p in pieces)
        {
            var c = p.color; c.a *= fade;
            float w = p.size, h = snow ? p.size : p.size * 0.55f * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(p.rot * Mathf.Deg2Rad * 2f)));
            var q = Quaternion.Euler(0f, 0f, p.rot);
            int k = vh.currentVertCount;
            vh.AddVert((Vector3)p.pos + q * new Vector3(-w, -h) * 0.5f, c, Vector2.zero);
            vh.AddVert((Vector3)p.pos + q * new Vector3(-w, h) * 0.5f, c, Vector2.zero);
            vh.AddVert((Vector3)p.pos + q * new Vector3(w, h) * 0.5f, c, Vector2.zero);
            vh.AddVert((Vector3)p.pos + q * new Vector3(w, -h) * 0.5f, c, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2);
            vh.AddTriangle(k + 2, k + 3, k);
        }
    }
}

/// <summary>A round medal: gold ring and a coloured middle.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class BadgeMedalGraphic : MaskableGraphic
{
    public Color fill = Color.white, ring = Color.yellow;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float rad = Mathf.Min(r.width, r.height) * 0.5f;
        Disc(vh, r.center, rad, ring);
        Disc(vh, r.center, rad * 0.8f, fill);
    }

    static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
    {
        const int sides = 36;
        int center = vh.currentVertCount;
        vh.AddVert(c, color, Vector2.zero);
        for (int k = 0; k <= sides; k++)
        {
            float a = k * Mathf.PI * 2f / sides;
            vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            if (k > 0) vh.AddTriangle(center, center + k, center + k + 1);
        }
    }
}

/// <summary>
/// The code-drawn ending picture: sky gradient, sun (or grey clouds), snowy mountains, rolling grass whose colour
/// follows the ending, and little sheep / leopard shapes for the animals that are left. Locked = dark silhouette.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class EndingLandscapeGraphic : MaskableGraphic
{
    int ending;
    bool unlocked;

    public void Set(int endingIndex, bool isUnlocked)
    {
        ending = endingIndex;
        unlocked = isUnlocked;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        if (r.width < 2f || r.height < 2f) return;
        var e = GuidedEndings.All[Mathf.Clamp(ending, 0, GuidedEndings.Count - 1)];
        Color Tint(Color c) => unlocked ? c * color : new Color(0.28f, 0.3f, 0.36f, color.a) * Mathf.Lerp(0.8f, 1.2f, c.grayscale);

        // Sky: lighter at the horizon.
        var skyTop = Tint(e.sky * 0.85f); skyTop.a = color.a;
        var skyLow = Tint(Color.Lerp(e.sky, Color.white, 0.45f)); skyLow.a = color.a;
        Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), skyLow, skyTop);

        // Sun or clouds.
        bool happy = EndingCelebration.IsHappy(ending) || ending == 1;
        var sunPos = new Vector2(Mathf.Lerp(r.xMin, r.xMax, 0.8f), Mathf.Lerp(r.yMin, r.yMax, 0.78f));
        float u = Mathf.Min(r.width, r.height);
        if (happy) Disc(vh, sunPos, u * 0.11f, Tint(new Color(1f, 0.86f, 0.35f)));
        else
        {
            var cloud = Tint(new Color(0.86f, 0.87f, 0.9f));
            Disc(vh, sunPos, u * 0.08f, cloud); Disc(vh, sunPos + new Vector2(u * 0.09f, -u * 0.01f), u * 0.065f, cloud); Disc(vh, sunPos + new Vector2(-u * 0.08f, -u * 0.02f), u * 0.06f, cloud);
        }

        // Mountains with snow caps.
        float baseY = Mathf.Lerp(r.yMin, r.yMax, 0.38f);
        Mountain(vh, r, 0.18f, 0.78f, 0.34f, baseY, Tint(e.hills * 0.9f), Tint(Color.white));
        Mountain(vh, r, 0.52f, 0.92f, 0.42f, baseY, Tint(e.hills), Tint(Color.white));
        Mountain(vh, r, 0.86f, 0.70f, 0.30f, baseY, Tint(e.hills * 0.85f), Tint(Color.white));

        // Rolling ground.
        var g0 = Tint(e.ground); var g1 = Tint(e.ground * 0.82f);
        const int steps = 24;
        for (int i = 0; i < steps; i++)
        {
            float x0 = Mathf.Lerp(r.xMin, r.xMax, i / (float)steps), x1 = Mathf.Lerp(r.xMin, r.xMax, (i + 1) / (float)steps);
            float y0 = baseY + Mathf.Sin(i * 0.7f) * u * 0.025f, y1 = baseY + Mathf.Sin((i + 1) * 0.7f) * u * 0.025f;
            int k = vh.currentVertCount;
            vh.AddVert(new Vector2(x0, r.yMin), g1, Vector2.zero);
            vh.AddVert(new Vector2(x0, y0), g0, Vector2.zero);
            vh.AddVert(new Vector2(x1, y1), g0, Vector2.zero);
            vh.AddVert(new Vector2(x1, r.yMin), g1, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k + 2, k + 3, k);
        }
        if (!unlocked) return;

        // Animals left: sheep (white fluff) and leopards (spotted), as the ending describes.
        bool sheep = ending == 1 || ending == 3 || ending == 4, cats = ending == 2 || ending == 3 || ending == 4;
        float gy = Mathf.Lerp(r.yMin, baseY, 0.45f);
        if (sheep) for (int i = 0; i < (ending == 4 ? 2 : 4); i++) Sheep(vh, new Vector2(Mathf.Lerp(r.xMin, r.xMax, 0.14f + i * 0.11f), gy + (i % 2) * u * 0.05f), u * 0.05f);
        if (cats) for (int i = 0; i < (ending == 2 ? 2 : 1); i++) Leopard(vh, new Vector2(Mathf.Lerp(r.xMin, r.xMax, 0.76f - i * 0.13f), gy + u * 0.02f), u * 0.055f);
    }

    void Mountain(VertexHelper vh, Rect r, float cx, float h, float halfW, float baseY, Color body, Color snow)
    {
        var peak = new Vector2(Mathf.Lerp(r.xMin, r.xMax, cx), Mathf.Lerp(baseY, r.yMax, h));
        var left = new Vector2(Mathf.Lerp(r.xMin, r.xMax, cx - halfW), baseY);
        var right = new Vector2(Mathf.Lerp(r.xMin, r.xMax, cx + halfW), baseY);
        Tri(vh, left, peak, right, body);
        float k = 0.28f;
        Tri(vh, Vector2.Lerp(peak, left, k), peak, Vector2.Lerp(peak, right, k), snow);
    }

    void Sheep(VertexHelper vh, Vector2 p, float s)
    {
        var wool = new Color(0.97f, 0.97f, 0.95f, color.a); var face = new Color(0.35f, 0.3f, 0.28f, color.a);
        Disc(vh, p, s, wool); Disc(vh, p + new Vector2(-s * 0.7f, s * 0.2f), s * 0.7f, wool); Disc(vh, p + new Vector2(s * 0.7f, s * 0.15f), s * 0.7f, wool);
        Disc(vh, p + new Vector2(s * 1.35f, s * 0.35f), s * 0.42f, face);
        Quad(vh, p + new Vector2(-s * 0.7f, -s * 1.3f), p + new Vector2(-s * 0.5f, -s * 0.6f), face, face);
        Quad(vh, p + new Vector2(s * 0.5f, -s * 1.3f), p + new Vector2(s * 0.7f, -s * 0.6f), face, face);
    }

    void Leopard(VertexHelper vh, Vector2 p, float s)
    {
        var fur = new Color(0.86f, 0.84f, 0.78f, color.a); var spot = new Color(0.32f, 0.3f, 0.3f, color.a);
        Disc(vh, p, s * 0.85f, fur); Disc(vh, p + new Vector2(s * 0.9f, 0f), s * 0.8f, fur);
        Disc(vh, p + new Vector2(-s * 0.9f, s * 0.35f), s * 0.6f, fur);   // head
        Disc(vh, p + new Vector2(-s * 1.2f, s * 0.85f), s * 0.18f, fur); Disc(vh, p + new Vector2(-s * 0.65f, s * 0.85f), s * 0.18f, fur);   // ears
        for (int i = 0; i < 4; i++) Disc(vh, p + new Vector2(s * (0.1f + i * 0.4f), s * ((i % 2) * 0.35f - 0.1f)), s * 0.13f, spot);
        Quad(vh, p + new Vector2(s * 1.6f, s * 0.1f), p + new Vector2(s * 2.6f, s * 0.35f), fur, fur);   // tail
        foreach (float lx in new[] { -0.55f, -0.2f, 0.75f, 1.1f })                                        // legs
            Quad(vh, p + new Vector2(s * lx, -s * 1.25f), p + new Vector2(s * (lx + 0.22f), -s * 0.5f), fur, fur);
        Disc(vh, p + new Vector2(-s * 1.15f, s * 0.4f), s * 0.08f, spot);   // eye
    }

    static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color bottom, Color top)
    {
        int k = vh.currentVertCount;
        vh.AddVert(new Vector2(min.x, min.y), bottom, Vector2.zero);
        vh.AddVert(new Vector2(min.x, max.y), top, Vector2.zero);
        vh.AddVert(new Vector2(max.x, max.y), top, Vector2.zero);
        vh.AddVert(new Vector2(max.x, min.y), bottom, Vector2.zero);
        vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k + 2, k + 3, k);
    }

    static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int k = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(k, k + 1, k + 2);
    }

    static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
    {
        const int sides = 20;
        int center = vh.currentVertCount;
        vh.AddVert(c, color, Vector2.zero);
        for (int k = 0; k <= sides; k++)
        {
            float a = k * Mathf.PI * 2f / sides;
            vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            if (k > 0) vh.AddTriangle(center, center + k, center + k + 1);
        }
    }
}
