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
            renderer.material = new Material(Shader.Find("Sprites/Default"));
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
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);
        }

        var main = rainSystem.main;
        main.startLifetime = 3f;
        main.startSpeed = 18f;
        main.gravityModifier = 1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.5f); // Fixed type here
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = false;

        var emission = rainSystem.emission;
        emission.rateOverTime = 1000f;

        var shape = rainSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(400f, 1f, 400f);

        rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
}
