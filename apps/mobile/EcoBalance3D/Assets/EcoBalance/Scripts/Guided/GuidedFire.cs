// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SCRUM-390 Player action: grass fire. Once in each chapter except Winter, on its third day, a fire starts on a
/// grass patch. The mascot warns the player (with Watch). Tapping a fire puts it out. A fire left alone burns
/// its patch's grass and spreads to nearby grass, so the sheep lose food. The chapter card shows the result.
/// </summary>
public partial class GuidedChallenge
{
    const int FireDayInChapter = 3;   // days 3, 8 and 18 (Summer, Autumn, Spring)
    int fireChapter;

    /// <summary>Fires put out by the player / grass patches burned, this story.</summary>
    public static int FiresPutOut { get; private set; }
    public static int PatchesBurned { get; private set; }

    internal static void NotePutOut() => FiresPutOut++;
    internal static void NoteBurned() => PatchesBurned++;

    void ResetFires()
    {
        FiresPutOut = 0;
        PatchesBurned = 0;
        fireChapter = 0;
        GrassFire.ClearAll();
    }

    /// <summary>Called from Update when no card is open.</summary>
    void UpdateFire(int day)
    {
        // Demo / test key: F starts a fire now (for the M3 presentation; SCRUM-378 dev hotkeys).
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            var p = GrassFire.PickStartPatch();
            if (p != null) Mascot.Alert("Oh no! A fire in the grass! Tap the flames to put it out before it spreads!", GrassFire.Ignite(p).transform);
            return;
        }
        if (fireChapter == CurrentChapter || CurrentChapter == 3) return;   // one per chapter, none in Winter (snow)
        int dayInChapter = day - (CurrentChapter - 1) * DaysPerChapter;
        if (dayInChapter < FireDayInChapter) return;
        fireChapter = CurrentChapter;

        var patch = GrassFire.PickStartPatch();
        if (patch == null) return;
        var fire = GrassFire.Ignite(patch);
        Mascot.Alert("Oh no! A fire in the grass! Tap the flames to put it out before it spreads!", fire.transform);
    }

    string FireRecap() =>
        FiresPutOut + PatchesBurned == 0 ? "" :
        $"\nFires put out: {FiresPutOut}   Grass burned: {PatchesBurned} patch{(PatchesBurned == 1 ? "" : "es")}";
}

/// <summary>One burning grass patch: particle flames and smoke, scares animals away, eats the grass, spreads, tap to put out.</summary>
public class GrassFire : MonoBehaviour
{
    static readonly List<GrassFire> active = new List<GrassFire>();
    public static IReadOnlyList<GrassFire> Active => active;

    const int MaxFires = 5;
    const float SpreadRadius = 30f;
    const float BurnDays = 0.5f;       // a patch burns out in half a game day
    const float SpreadEveryDays = 0.25f;
    const float TapRadiusPx = 110f;    // at 1200 px screen height; generous for small fingers

    ResourcePatch patch;
    float spreadTimer;
    bool gone;
    Light glow;
    float scareTimer;
    static Material fireMat, smokeMat;
    static Texture2D softDot;
    static int lastTapFrame = -1;

    public static ResourcePatch PickStartPatch()
    {
        var options = new List<ResourcePatch>();
        foreach (var p in ResourcePatch.All)
            if (p != null && !p.IsWater && p.IsEdible && !IsBurning(p)) options.Add(p);
        return options.Count == 0 ? null : options[Random.Range(0, options.Count)];
    }

    static bool IsBurning(ResourcePatch p)
    {
        foreach (var f in active) if (f != null && f.patch == p) return true;
        return false;
    }

    public static GrassFire Ignite(ResourcePatch p)
    {
        var go = new GameObject("GrassFire");
        go.transform.position = p.transform.position;
        var f = go.AddComponent<GrassFire>();
        f.patch = p;
        f.BuildFlames();
        active.Add(f);
        return f;
    }

    public static void ClearAll()
    {
        foreach (var f in active) if (f != null) Destroy(f.gameObject);
        active.Clear();
    }

    // Particle fire + smoke with a soft round texture made in code (no asset needed), plus a flickering orange light.
    void BuildFlames()
    {
        if (softDot == null) softDot = MakeSoftDot(64);
        if (fireMat == null) fireMat = MakeMat(true);
        if (smokeMat == null) smokeMat = MakeMat(false);

        Particles("Flames", fireMat, 70f, 0.5f, 1.0f, 1.0f, 2.2f, 1.5f, 3.5f, 2.5f,
                  new Color(1f, 0.85f, 0.3f, 1f), new Color(0.9f, 0.15f, 0.02f, 0f), 0.3f);
        Particles("Smoke", smokeMat, 18f, 2.5f, 4f, 2.5f, 5f, 1.5f, 3f, 2f,
                  new Color(0.25f, 0.23f, 0.22f, 0.55f), new Color(0.5f, 0.5f, 0.5f, 0f), 1.5f);

        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.up * 2f;
        glow = lightGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.5f, 0.15f);
        glow.range = 14f;
    }

    void Particles(string name, Material mat, float rate, float lifeMin, float lifeMax, float sizeMin, float sizeMax,
                   float speedMin, float speedMax, float radius, Color start, Color end, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * y;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // emit upwards
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(start.a, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    static Material MakeMat(bool additive)
    {
        // Sprites/Default works in URP and in builds (Li Juan's snow and rain use it); Legacy particle shaders turn pink in URP.
        var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = softDot };
        m.renderQueue = additive ? 3100 : 3000;   // flames drawn over the smoke
        return m;
    }

    static Texture2D MakeSoftDot(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    // Animals near the fire run from it (like a herd alarm call), so the player sees why fire matters.
    void ScareAnimals()
    {
        foreach (var a in AnimalBrain.All)
            if (a != null && !a.IsCaught && Vector3.Distance(a.transform.position, transform.position) < 18f)
                a.HearAlarm(transform.position);
    }

    void Update()
    {
        if (patch == null) { Remove(); return; }

        var cam = Camera.main;
        if (glow != null) glow.intensity = 2.5f + 0.8f * Mathf.PerlinNoise(Time.unscaledTime * 6f, transform.position.x);

        HandleTap(cam);
        if (gone) return;

        // Burn: eats the whole patch in BurnDays game days (scaled time, so it stops while paused).
        float day = Mathf.Max(1f, ClockService.DaySeconds);
        patch.Consume(100f * Time.deltaTime / (BurnDays * day));
        if (patch.Amount <= 0.5f)
        {
            GuidedChallenge.NoteBurned();
            Remove();
            return;
        }

        scareTimer -= Time.deltaTime;
        if (scareTimer <= 0f) { scareTimer = 0.5f; ScareAnimals(); }

        spreadTimer += Time.deltaTime;
        if (spreadTimer >= SpreadEveryDays * day)
        {
            spreadTimer = 0f;
            TrySpread();
        }
    }

    void TrySpread()
    {
        if (active.Count >= MaxFires) return;
        ResourcePatch best = null;
        float bestD = SpreadRadius;
        foreach (var p in ResourcePatch.All)
        {
            if (p == null || p.IsWater || !p.IsEdible || IsBurning(p)) continue;
            float d = Vector3.Distance(p.transform.position, transform.position);
            if (d < bestD) { bestD = d; best = p; }
        }
        if (best != null) Ignite(best);
    }

    // One check per frame for all fires: put out the fire nearest the tap, if it is close enough on screen.
    static void HandleTap(Camera cam)
    {
        if (lastTapFrame == Time.frameCount) return;
        lastTapFrame = Time.frameCount;
        var pointer = Pointer.current;
        if (cam == null || pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (PointerOverUI.IsOver()) return;

        Vector2 tap = pointer.position.ReadValue();
        float limit = TapRadiusPx * Screen.height / 1200f;
        GrassFire hit = null;
        foreach (var f in active)
        {
            if (f == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(f.transform.position + Vector3.up);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(tap, sp);
            if (d < limit) { limit = d; hit = f; }
        }
        if (hit == null) return;

        GuidedChallenge.NotePutOut();
        hit.Remove();
        if (active.Count == 0)
            Mascot.Alert("You put out the fire! Well done, the grass can grow back now.", null);
    }

    void Remove()
    {
        gone = true;
        active.Remove(this);
        if (this != null) Destroy(gameObject);
    }
}
