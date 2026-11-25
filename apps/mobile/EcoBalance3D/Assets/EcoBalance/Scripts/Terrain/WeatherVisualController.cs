using UnityEngine;

public class WeatherVisualController : MonoBehaviour
{
    [Header("Particle Settings")]
    public bool enableWeatherParticles = true;
    private ParticleSystem snowSystem;
    private ParticleSystem rainSystem;

    private bool isSnowActive = false;
    private bool isRainActive = false;
    // [AI-GENERATED] Claude Code (Claude Opus 5.5): M3 - every weather has its own particles.
    private ParticleSystem leafSystem, pollenSystem;
    private bool isLeavesActive, isSunnyActive;
    private ClockService clock;

    [Header("Lighting & Environment Shifts")]
    public Light directionalLight;
    public Color normalLightColor = Color.white;                  // Clear / Spring / Autumn
    public Color winterLightColor = new Color(0.85f, 0.92f, 1f);  // Cold blue-white
    public Color rainLightColor = new Color(0.75f, 0.8f, 0.85f);    // Overcast grey-blue
    public Color heatwaveLightColor = new Color(1f, 0.9f, 0.75f); // Warm golden sun

    SimulationSpeedController speedController;
    static Texture2D softDot;

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): M3 realism - each weather changes the air and the land, not only particles.
    //   Clear:    light haze far away (mountain air)
    //   Rain:     grey fog closes in, sun dimmed by cloud
    //   Snow:     white-out fog, sun softened
    //   Heatwave: warm dusty haze, bright sun, grass dries from olive to straw
    // Everything blends over BlendSeconds so a weather change is a visible shift, not a cut.
    const float BlendSeconds = 4f;
    struct Look { public Color fog; public float fogStart, fogEnd, sun, dry; }
    static readonly Look ClearLook = new Look { fog = new Color(0.72f, 0.80f, 0.88f), fogStart = 250f, fogEnd = 1400f, sun = 1f,    dry = 0f };
    static readonly Look RainLook  = new Look { fog = new Color(0.52f, 0.56f, 0.60f), fogStart = 30f,  fogEnd = 320f,  sun = 0.55f, dry = 0f };
    static readonly Look SnowLook  = new Look { fog = new Color(0.88f, 0.91f, 0.95f), fogStart = 20f,  fogEnd = 260f,  sun = 0.75f, dry = 0f };
    static readonly Look HeatLook  = new Look { fog = new Color(0.90f, 0.80f, 0.62f), fogStart = 120f, fogEnd = 750f,  sun = 1.2f,  dry = 1f };
    Look target = ClearLook, now = ClearLook;
    bool lookStarted;

    /// <summary>Multiplier for the sun's brightness from the weather (read by DayNightLighting).</summary>
    public static float SunScale { get; private set; } = 1f;

    static readonly Color GrassBase = new Color(0.18f, 0.25f, 0.11f), GrassTip = new Color(0.55f, 0.56f, 0.30f);
    static readonly Color StrawBase = new Color(0.36f, 0.30f, 0.15f), StrawTip = new Color(0.80f, 0.70f, 0.42f);

    void OnDestroy() => SunScale = 1f;   // other scenes use DayNightLighting too: leave the sun normal

    void BlendLook()
    {
        if (!lookStarted) { lookStarted = true; now = target; }
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f / BlendSeconds);
        now.fog = Color.Lerp(now.fog, target.fog, k);
        now.fogStart = Mathf.Lerp(now.fogStart, target.fogStart, k);
        now.fogEnd = Mathf.Lerp(now.fogEnd, target.fogEnd, k);
        now.sun = Mathf.Lerp(now.sun, target.sun, k);
        now.dry = Mathf.Lerp(now.dry, target.dry, k);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        float night = clock != null && !clock.IsDaytime ? 0.35f : 1f;   // fog goes dark at night, not glowing
        RenderSettings.fogColor = now.fog * night;
        RenderSettings.fogStartDistance = now.fogStart;
        RenderSettings.fogEndDistance = now.fogEnd;
        SunScale = now.sun;

        var grass = HimalayaResourcePlacer.GrassMaterial;
        if (grass != null)
        {
            grass.SetColor("_BaseColor", Color.Lerp(GrassBase, StrawBase, now.dry));
            grass.SetColor("_TipColor", Color.Lerp(GrassTip, StrawTip, now.dry));
        }
    }


    void Awake()
    {
        if (enableWeatherParticles)
        {
            if (snowSystem == null) CreateProceduralSnowSystem();
            if (rainSystem == null) CreateProceduralRainSystem();
            if (leafSystem == null) CreateLeafSystem();
            if (pollenSystem == null) CreatePollenSystem();
        }

        if (directionalLight == null)
        {
            directionalLight = FindAnyObjectByType<Light>();
        }
    }

    void CreateProceduralSnowSystem()
    {
        GameObject snowObj = new GameObject("ProceduralSnowSystem");
        snowObj.transform.SetParent(transform);
        snowObj.transform.localPosition = new Vector3(0f, 40f, 0f);

        snowSystem = snowObj.AddComponent<ParticleSystem>();

        var renderer = snowObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftDot() };
            renderer.material.color = Color.white;
        }

        var main = snowSystem.main;
        main.startLifetime = 10f;
        main.startSpeed = 1.5f;
        main.gravityModifier = 0.4f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); // Fixed type here
        main.maxParticles = 3000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = false;

        var emission = snowSystem.emission;
        emission.rateOverTime = 600f;

        var shape = snowSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(400f, 1f, 400f);

        snowSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void CreateProceduralRainSystem()
    {
        GameObject rainObj = new GameObject("ProceduralRainSystem");
        rainObj.transform.SetParent(transform);
        rainObj.transform.localPosition = new Vector3(0f, 40f, 0f);

        rainSystem = rainObj.AddComponent<ParticleSystem>();

        var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = RainStreak() };
            renderer.material.color = new Color(0.75f, 0.85f, 1f, 0.55f);

            // Stretch each drop along the way it is falling, so it reads as a streak.
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.5f;     // streak length from the drop's size
            renderer.velocityScale = 0.04f;  // faster drops get longer
            renderer.cameraVelocityScale = 0f;
        }

        var main = rainSystem.main;
        main.startLifetime = 2.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(26f, 34f);   // fast, with some variety
        main.gravityModifier = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f); // thin
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.8f, 0.95f, 0.35f), new Color(0.85f, 0.92f, 1f, 0.7f)); // some drops fainter than others
        main.maxParticles = 5000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = false;

        var emission = rainSystem.emission;
        emission.rateOverTime = 1200f;

        var shape = rainSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(400f, 1f, 400f);
        shape.rotation = new Vector3(90f, 0f, 0f);   // emit downward, not up

        // A slight slant, as if the wind is blowing.
        var vel = rainSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(2f, 4f);
        // All three axes must use the same curve mode (x is "between two constants"), or Unity logs
        // "Particle Velocity curves must all be in the same mode" every frame and the game stutters.
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static Texture2D rainStreak;

    // A soft vertical line: bright in the middle, faded at the sides and ends.
    static Texture2D RainStreak()
    {
        if (rainStreak != null) return rainStreak;
        const int w = 16, h = 64;
        rainStreak = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f;   // 0 at the centre, 1 at the sides
                float dy = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f;   // 0 at the middle, 1 at the ends
                float a = Mathf.Clamp01(1f - dx) * Mathf.Clamp01((1f - dy) / 0.4f);
                rainStreak.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        rainStreak.Apply();
        return rainStreak;
    }

    public void SetWeather(string season, string condition)
    {
        string s = season != null ? season.ToLowerInvariant() : "";
        string c = condition != null ? condition.ToLowerInvariant() : "";

        bool isWinter = s.Contains("winter");
        bool hasCondition = c.Length > 0;

        // Snow only when the condition says so. A winter day with no condition set still snows;
        // a winter day that is "clear" does not.
        isSnowActive = c.Contains("snow") || (isWinter && !hasCondition);
        isRainActive = c.Contains("rain");
        bool isHeatwave = c.Contains("drought");
        // A cold drought (below 5 °C) is dry and clear, not a heatwave: no straw grass or dust haze.
        bool hot = isHeatwave && SeasonWeather.Temperature >= 5f;
        target = isSnowActive ? SnowLook : isRainActive ? RainLook : hot ? HeatLook : ClearLook;
        // Autumn without rain/snow: leaves blowing in the wind. Clear or dry Spring/Summer: floating pollen in the sun.
        isLeavesActive = s.Contains("autumn") && !isSnowActive && !isRainActive;
        isSunnyActive = !isWinter && !s.Contains("autumn") && !isSnowActive && !isRainActive;

        Debug.Log($"[WEATHER] season='{season}' condition='{condition}' -> snow={isSnowActive} rain={isRainActive}");

        if (directionalLight != null)
        {
            if (isSnowActive) directionalLight.color = winterLightColor;
            else if (isRainActive) directionalLight.color = rainLightColor;
            else if (isHeatwave) directionalLight.color = heatwaveLightColor;
            else if (isWinter) directionalLight.color = winterLightColor;   // cold light, no snow
            else directionalLight.color = normalLightColor;
        }
    }

    void Update()
    {
        if (speedController == null) speedController = FindAnyObjectByType<SimulationSpeedController>();
        bool isPaused = speedController != null && !speedController.IsRunning;

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the map is now about 5x bigger than the 400-wide weather boxes,
        // so the weather follows the camera instead of staying over the map centre.
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 c = cam.transform.position;
            Follow(snowSystem, c, 40f); Follow(rainSystem, c, 40f);
            Follow(leafSystem, c, 15f); Follow(pollenSystem, c, 2f);
        }
        if (clock == null) clock = FindAnyObjectByType<ClockService>();
        BlendLook();
        Toggle(leafSystem, isLeavesActive && (clock == null || clock.IsDaytime) && !isPaused);   // unlit too: would glow at night
        // Pollen only while the sun is up: the particles are unlit, so at night they showed as yellow dots.
        if (clock == null) clock = FindAnyObjectByType<ClockService>();
        bool daytime = clock == null || clock.IsDaytime;
        Toggle(pollenSystem, isSunnyActive && daytime && !isPaused);

        // Control Snow
        if (snowSystem != null)
        {
            if (isSnowActive && !isPaused)
            {
                if (!snowSystem.isPlaying) snowSystem.Play();
            }
            else
            {
                // Stop emitting AND clear existing flakes when weather changes or game pauses
                if (snowSystem.isPlaying || snowSystem.particleCount > 0)
                {
                    snowSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        // Control Rain
        if (rainSystem != null)
        {
            if (isRainActive && !isPaused)
            {
                if (!rainSystem.isPlaying) rainSystem.Play();
            }
            else
            {
                // Stop emitting AND clear existing drops when weather changes or game pauses
                if (rainSystem.isPlaying || rainSystem.particleCount > 0)
                {
                    rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    static void Follow(ParticleSystem ps, Vector3 cam, float height)
    {
        if (ps != null) ps.transform.position = new Vector3(cam.x, cam.y + height, cam.z);
    }

    static void Toggle(ParticleSystem ps, bool on)
    {
        if (ps == null) return;
        if (on) { if (!ps.isPlaying) ps.Play(); }
        else if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // let them drift away
    }

    // Autumn: orange, red and brown leaves tumbling sideways in the wind.
    void CreateLeafSystem()
    {
        var go = new GameObject("ProceduralLeafSystem");
        go.transform.SetParent(transform);
        leafSystem = go.AddComponent<ParticleSystem>();
        leafSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = LeafTexture() };

        var main = leafSystem.main;
        main.startLifetime = 9f;
        main.startSpeed = 0f;
        main.gravityModifier = 0.03f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.45f, 0.12f), new Color(0.55f, 0.28f, 0.1f));
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = leafSystem.emission; em.rateOverTime = 35f;
        var sh = leafSystem.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(140f, 20f, 140f);
        var vel = leafSystem.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(3f, 6f);   // the wind
        vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
        vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
        var rot = leafSystem.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
        var noise = leafSystem.noise; noise.enabled = true; noise.strength = 1.2f; noise.frequency = 0.4f;
        var col = leafSystem.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // Sunny Spring / Summer: a few warm specks of pollen floating near the camera, catching the light.
    void CreatePollenSystem()
    {
        var go = new GameObject("ProceduralPollenSystem");
        go.transform.SetParent(transform);
        pollenSystem = go.AddComponent<ParticleSystem>();
        pollenSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftDot() };

        var main = pollenSystem.main;
        main.startLifetime = 6f;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f, 0.9f), new Color(1f, 0.85f, 0.5f, 0.6f));
        main.maxParticles = 250;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = pollenSystem.emission; em.rateOverTime = 30f;
        var sh = pollenSystem.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 25f;
        var noise = pollenSystem.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.3f;
        var col = pollenSystem.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    static Texture2D leafTex;
    // A small pointed leaf shape (white, tinted per particle).
    static Texture2D LeafTexture()
    {
        if (leafTex != null) return leafTex;
        const int n = 32;
        leafTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float width = 0.55f * (1f - v * v);            // pointed at both ends
                float a = Mathf.Clamp01((width - Mathf.Abs(u)) * 8f);
                if (Mathf.Abs(u) < 0.05f && Mathf.Abs(v) < 0.8f) a *= 0.7f;   // midrib
                leafTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        leafTex.Apply();
        return leafTex;
    }

    static Texture2D SoftDot()
    {
        if (softDot != null) return softDot;
        const int size = 64;
        softDot = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01((1f - d) / 0.35f);   // solid in the middle, fades over the outer edge
                softDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        softDot.Apply();
        return softDot;
    }
}
