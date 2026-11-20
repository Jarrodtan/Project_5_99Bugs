using UnityEngine;
using UnityEngine.UI;

/// [AI-GENERATED] Claude Code (Claude Opus 5.5): filled dot inside an option's circle so the picked option reads as a
/// selected radio button, not only a green tint. Used by the Guided event card and the prediction questions.
/// The dot sprite is drawn once in code, so no image asset is needed.
public static class SelectedDot
{
    static Sprite sprite;

    public static void Set(Transform circle, bool on)
    {
        if (circle == null) return;
        var dot = circle.Find("SelectedDot");
        if (dot == null)
        {
            if (!on) return;
            var go = new GameObject("SelectedDot", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(circle, false);
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = Sprite;
            img.color = new Color(0.18f, 0.5f, 0.36f);   // same green as the Confirm buttons
            img.raycastTarget = false;
            dot = rt;
        }
        dot.gameObject.SetActive(on);
    }

    static Sprite Sprite
    {
        get
        {
            if (sprite != null) return sprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float r = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
