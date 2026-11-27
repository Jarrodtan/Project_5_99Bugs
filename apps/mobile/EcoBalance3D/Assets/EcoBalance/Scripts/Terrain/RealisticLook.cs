// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// M3 feedback "make it look realistic, less cartoon": filmic colour grading on the simulation scene.
/// ACES tonemapping, muted earthy colours, a little more contrast and slightly warm light, and deeper soft
/// sun shadows. Works on a copy of the Global Volume's profile, so the asset on disk is never changed.
/// Turn it off with Enabled = false to compare.
/// </summary>
public static class RealisticLook
{
    public static bool Enabled = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnLoaded(Scene scene, LoadSceneMode _)
    {
        if (!Enabled || scene.name != ScreenFlow.Simulation) return;

        var volume = Object.FindAnyObjectByType<Volume>();
        if (volume != null && volume.isGlobal)
        {
            var p = volume.profile;   // instance copy

            if (!p.TryGet(out Tonemapping tone)) tone = p.Add<Tonemapping>();
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);            // filmic: no flat, saturated "cartoon" colours

            if (!p.TryGet(out ColorAdjustments col)) col = p.Add<ColorAdjustments>();
            col.active = true;
            col.saturation.Override(-18f);                       // earthy, alpine palette
            col.contrast.Override(14f);
            col.postExposure.Override(0.15f);                    // ACES darkens a little

            if (!p.TryGet(out WhiteBalance wb)) wb = p.Add<WhiteBalance>();
            wb.active = true;
            wb.temperature.Override(6f);                         // slightly warm sunlight

            if (p.TryGet(out Vignette vig)) { vig.active = true; vig.intensity.Override(0.22f); }
        }

        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional) continue;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.85f;                            // deeper shadows give depth to the slopes
        }
    }
}
