// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using TMPro;
using UnityEngine;

/// <summary>
/// A small pink heart that floats up, sways, faces the camera and fades out.
/// Used when two animals mate, so children can see the moment happen.
/// </summary>
public class HeartPop : MonoBehaviour
{
    const float Lifetime = 2f;
    const float RiseSpeed = 0.8f;
    static readonly Color Pink = new Color(1f, 0.35f, 0.55f, 1f);

    TextMeshPro text;
    float age;
    float sway;
    float startDelay;

    /// <summary>Spawns <paramref name="count"/> hearts around a point, slightly staggered.</summary>
    public static void Spawn(Vector3 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Heart");
            go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            var heart = go.AddComponent<HeartPop>();
            heart.startDelay = i * 0.25f;
            heart.sway = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Awake()
    {
        text = gameObject.AddComponent<TextMeshPro>();
        text.text = "♥";
        text.fontSize = 10f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(Pink.r, Pink.g, Pink.b, 0f);
        text.rectTransform.sizeDelta = new Vector2(2f, 2f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = (age - startDelay) / Lifetime;
        if (t < 0f) return;
        if (t >= 1f) { Destroy(gameObject); return; }

        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime)
                            + Vector3.right * (Mathf.Sin(sway + age * 4f) * 0.3f * Time.deltaTime);

        // Pop in, then fade out.
        float alpha = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
        float scale = Mathf.Lerp(0.6f, 1.1f, Mathf.Min(1f, t * 4f));
        text.color = new Color(Pink.r, Pink.g, Pink.b, alpha);
        transform.localScale = Vector3.one * scale;

        var cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}
