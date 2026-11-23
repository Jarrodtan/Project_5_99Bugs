using UnityEngine;

public class WeatherVisualController : MonoBehaviour
{
    [Header("Snow Settings (Procedural)")]
    public bool enableSnowParticles = true;
    private ParticleSystem snowSystem;
    private bool shouldBePlaying = false;

    [Header("Lighting & Environment")]
    public Light directionalLight;
    public Color winterLightColor = new Color(0.85f, 0.92f, 1f);
    public Color normalLightColor = Color.white;

    void Awake()
    {
        if (enableSnowParticles && snowSystem == null)
        {
            CreateProceduralSnowSystem();
        }
    }

    void CreateProceduralSnowSystem()
    {
        GameObject snowObj = new GameObject("ProceduralSnowSystem");
        snowObj.transform.SetParent(transform);
        snowObj.transform.localPosition = new Vector3(0f, 30f, 0f);

        snowSystem = snowObj.AddComponent<ParticleSystem>();

        // Fix the purple/magenta error with a white material
        var renderer = snowObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = Color.white;
        }

        var main = snowSystem.main;
        main.startLifetime = 6f;
        main.startSpeed = 0.2f;        // Low initial speed so they don't shoot sideways
        main.gravityModifier = 0.3f;   // Pulls particles gently downward like real snow
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startColor = Color.white;
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = false;

        var emission = snowSystem.emission;
        emission.rateOverTime = 150f;

        var shape = snowSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(100f, 1f, 100f);

        snowSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetWeather(string season, string condition)
    {
        shouldBePlaying = (season != null && season.ToLower().Contains("winter")) ||
                          (condition != null && condition.ToLower().Contains("snow"));

        // Toggle directional lighting color
        if (directionalLight != null)
        {
            directionalLight.color = shouldBePlaying ? winterLightColor : normalLightColor;
        }
    }

    void Update()
    {
        if (snowSystem == null) return;

        // Check if the simulation is paused (using SimulationSpeedController if available)
        SimulationSpeedController speedController = FindAnyObjectByType<SimulationSpeedController>();
        bool isPaused = speedController != null && !speedController.IsRunning;

        // If it should snow AND the game isn't paused, play. Otherwise, pause/stop.
        if (shouldBePlaying && !isPaused)
        {
            if (!snowSystem.isPlaying) snowSystem.Play();
        }
        else
        {
            if (snowSystem.isPlaying) snowSystem.Pause();
        }
    }
}
