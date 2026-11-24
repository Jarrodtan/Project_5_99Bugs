using UnityEngine;

public class WeatherVisualController : MonoBehaviour
{
    [Header("Particle Settings")]
    public bool enableWeatherParticles = true;
    private ParticleSystem snowSystem;
    private ParticleSystem rainSystem;

    private bool isSnowActive = false;
    private bool isRainActive = false;

    [Header("Lighting & Environment Shifts")]
    public Light directionalLight;
    public Color normalLightColor = Color.white;                  // Clear / Spring / Autumn
    public Color winterLightColor = new Color(0.85f, 0.92f, 1f);  // Cold blue-white
    public Color rainLightColor = new Color(0.75f, 0.8f, 0.85f);    // Overcast grey-blue
    public Color heatwaveLightColor = new Color(1f, 0.9f, 0.75f); // Warm golden sun

    SimulationSpeedController speedController;
    static Texture2D softDot;


    void Awake()
    {
        if (enableWeatherParticles)
        {
            if (snowSystem == null) CreateProceduralSnowSystem();
            if (rainSystem == null) CreateProceduralRainSystem();
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
        // A slight slant, as if the wind is blowing. All three axes must use the same curve mode.
        var vel = rainSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(2f, 4f);
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
