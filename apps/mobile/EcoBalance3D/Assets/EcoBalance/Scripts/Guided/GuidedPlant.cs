// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player action: help the grass. In Guided, tapping an eaten-out or burnt grass patch replants it
/// (+60 of 100). The player has HelpsPerChapter helps each chapter, so they must choose where the herd
/// needs food most. A green sparkle shows where the grass was planted.
/// </summary>
public partial class GuidedChallenge
{
    const int HelpsPerChapter = 3;
    const float PlantAmount = 60f;        // points of 100 added to the patch
    const float LowGrass = 50f;           // only patches below this % can be helped
    const float PlantTapRadiusPx = 90f;   // at 1200 px screen height

    public static int HelpsLeft { get; private set; } = HelpsPerChapter;
    public static int GrassPlanted { get; private set; }
    int helpChapter;
    bool helpHintShown;

    void ResetPlanting()
    {
        HelpsLeft = HelpsPerChapter;
        GrassPlanted = 0;
        helpChapter = 1;
        helpHintShown = false;
    }

    /// <summary>Called from Update when no card is open.</summary>
    void UpdatePlanting()
    {
        if (helpChapter != CurrentChapter) { helpChapter = CurrentChapter; HelpsLeft = HelpsPerChapter; }

        // Demo / test key: P makes a grass patch brown now and points the camera at it (like B for fire).
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            var p = GrassFire.PickStartPatch();
            if (p != null)
            {
                p.Remove(p.Amount - p.MaxAmount * 0.15f);   // leave 15%: brown, ready to plant (not eaten by an animal)
                helpHintShown = true;
                Mascot.Alert("The grass is getting low! Tap a brown patch to plant new grass for the sheep.", p.transform);
            }
            return;
        }

        // First time the grass gets low, tell the player they can help.
        if (!helpHintShown && ResourcePatch.VegetationPercent() < 60f)
        {
            helpHintShown = true;
            Mascot.Alert("The grass is getting low! Tap a brown patch to plant new grass for the sheep.", null);
        }

        var cam = Camera.main;
        var pointer = Pointer.current;
        if (cam == null || pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (PointerOverUI.IsOver()) return;

        ResourcePatch hit = FindLowPatchNear(cam, pointer.position.ReadValue());
        if (hit == null) return;

        if (HelpsLeft <= 0)
        {
            Mascot.Alert("You have no more helps this season. Watch how the animals manage!", null);
            return;
        }
        HelpsLeft--;
        GrassPlanted++;
        hit.Restore(PlantAmount);
        PlantSparkle(hit.transform.position);
        Mascot.Alert(HelpsLeft > 0
            ? "New grass planted! The sheep will find food here."
            : "New grass planted! That was your last help this season.", null);
    }

    static ResourcePatch FindLowPatchNear(Camera cam, Vector2 tap)
    {
        float limit = PlantTapRadiusPx * Screen.height / 1200f;
        ResourcePatch best = null;
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.IsWater || GrassFire.IsBurning(p)) continue;
            if (p.Amount >= p.MaxAmount * LowGrass / 100f) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(tap, sp);
            if (d < limit) { limit = d; best = p; }
        }
        return best;
    }

    static Material sparkleMat;

    // Green sparkle burst made in code (same style as the fire's water splash), removed after 2 s.
    static void PlantSparkle(Vector3 at)
    {
        if (sparkleMat == null) sparkleMat = new Material(Shader.Find("Sprites/Default"));
        var go = new GameObject("PlantSparkle");
        go.transform.position = at + Vector3.up * 0.5f;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startColor = new Color(0.5f, 1f, 0.4f, 0.9f);
        main.gravityModifier = -0.1f;   // drifts up gently
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = sparkleMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        Object.Destroy(go, 2.5f);
    }

    string PlantRecap() => GrassPlanted == 0 ? "" : $"\nGrass you planted: {GrassPlanted} patch{(GrassPlanted == 1 ? "" : "es")}";
}
