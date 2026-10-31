using System.Collections;
using UnityEngine;

/// <summary>
/// Coroutine scale (and renderer alpha when materials support it) fade for island swaps.
/// Duration comes from <see cref="IslandPlatformConfig.transitionDurationSeconds"/>.
/// </summary>
public class IslandFadeTransition : MonoBehaviour, IIslandTransition
{
    /// <inheritdoc />
    public IEnumerator Transition(Transform islandRoot, bool visible, IslandPlatformConfig config)
    {
        if (islandRoot == null || config == null)
            yield break;

        var duration = Mathf.Max(0f, config.transitionDurationSeconds);
        var renderers = islandRoot.GetComponentsInChildren<Renderer>(true);
        var startScale = islandRoot.localScale;
        var endScale = visible ? Vector3.one : Vector3.zero;
        if (!visible)
            startScale = islandRoot.localScale;
        else
        {
            islandRoot.localScale = Vector3.zero;
            startScale = Vector3.zero;
            endScale = Vector3.one;
        }

        var startAlpha = visible ? 0f : 1f;
        var endAlpha = visible ? 1f : 0f;

        if (duration <= 0f)
        {
            Apply(islandRoot, renderers, endScale, endAlpha);
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var scale = Vector3.Lerp(startScale, endScale, t);
            var alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            Apply(islandRoot, renderers, scale, alpha);
            yield return null;
        }

        Apply(islandRoot, renderers, endScale, endAlpha);
    }

    static void Apply(Transform root, Renderer[] renderers, Vector3 scale, float alpha)
    {
        root.localScale = scale;
        if (renderers == null)
            return;

        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
                continue;

            var materials = renderer.materials;
            for (var m = 0; m < materials.Length; m++)
            {
                var mat = materials[m];
                if (mat == null || !mat.HasProperty("_Color"))
                    continue;

                var color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }
}
