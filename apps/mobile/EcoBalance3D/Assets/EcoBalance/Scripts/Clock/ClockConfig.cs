using UnityEngine;

/// <summary>
/// Sole authored hub for the day-night clock. Time, lighting, and HUD copy live here —
/// never hardcode those knobs in <see cref="ClockService"/>, <see cref="ClockHudView"/>, or
/// <see cref="DayNightLighting"/>. Personal prototype — not Feature Plan / T04 work until approved.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Clock Config", fileName = "ClockConfig")]
public class ClockConfig : ScriptableObject
{
    [Header("Time")]
    [Tooltip("Real seconds for one in-game day at 1× sim speed.")]
    [Min(0.01f)]
    public float dayLengthSeconds = 240f;

    [Tooltip("Time of day when the clock starts or resets, 0–1 (0.25 = 06:00).")]
    [Range(0f, 1f)]
    public float startTimeOfDay = 0.25f;

    [Tooltip("Day number after Reset.")]
    [Min(1)]
    public int startingDay = 1;

    [Tooltip("Hour (0–24) when daytime begins for IsDaytime.")]
    [Range(0f, 24f)]
    public float sunriseHour = 6f;

    [Tooltip("Hour (0–24) when daytime ends for IsDaytime.")]
    [Range(0f, 24f)]
    public float sunsetHour = 18f;

    [Header("Lighting")]
    [Tooltip("Directional light / skybox disk colour (warm by day, cool silver-blue by night).")]
    public Gradient sunColorOverDay = new Gradient();

    [Tooltip("Directional light intensity across the day (0–1 time of day).")]
    public AnimationCurve sunIntensityOverDay = AnimationCurve.Linear(0f, 0.15f, 1f, 0.15f);

    [Tooltip("RenderSettings.ambientLight colour across the day.")]
    public Gradient ambientColorOverDay = new Gradient();

    [Tooltip("Intensity multiplier while IsDaytime is false (moon phase of the skybox disk).")]
    [Min(0f)]
    public float moonIntensity = 0.65f;

    [Tooltip("Fixed yaw (degrees) of the directional light while pitch follows time of day.")]
    public float lightYawDegrees = -30f;

    [Header("HUD")]
    [Tooltip("Format for the day pill. {0} is the day number.")]
    public string dayLabelFormat = "Day {0}";

    [Tooltip("Format for the clock pill. {0} is hours, {1} is minutes (0–59).")]
    public string timeLabelFormat = "{0:00}:{1:00}";

    [Tooltip("Time of day (0–1) at HUD dial left / light pitch zero (default 0.25 = 06:00).")]
    [Range(0f, 1f)]
    public float dialZeroTimeOfDay = 0.25f;

    [Tooltip("Radius in pixels of the HUD sun/moon dial.")]
    [Min(1f)]
    public float dialRadiusPixels = 48f;

    void Reset()
    {
        EnsureDefaults();
    }

    void OnValidate()
    {
        if (dayLengthSeconds < 0.01f)
            dayLengthSeconds = 0.01f;

        if (startingDay < 1)
            startingDay = 1;

        EnsureDefaults();
    }

    /// <summary>Fills empty gradients/curves so a freshly created asset works in Play Mode.</summary>
    public void EnsureDefaults()
    {
        var needsColour = sunColorOverDay == null || sunColorOverDay.colorKeys == null || sunColorOverDay.colorKeys.Length == 0;
        var needsIntensity = sunIntensityOverDay == null || sunIntensityOverDay.length == 0;
        var needsAmbient = ambientColorOverDay == null || ambientColorOverDay.colorKeys == null || ambientColorOverDay.colorKeys.Length == 0;
        if (needsColour || needsIntensity || needsAmbient)
            ApplyDefaultCurves();
    }

    void ApplyDefaultCurves()
    {
        sunColorOverDay = new Gradient();
        sunColorOverDay.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.8f, 0.95f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.3f), 0.25f),
                new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.5f),
                new GradientColorKey(new Color(1f, 0.45f, 0.25f), 0.75f),
                new GradientColorKey(new Color(0.7f, 0.78f, 0.95f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        sunIntensityOverDay = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.25f, 0.7f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(0.75f, 0.55f),
            new Keyframe(1f, 0.45f));

        ambientColorOverDay = new Gradient();
        ambientColorOverDay.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.08f, 0.1f, 0.2f), 0f),
                new GradientColorKey(new Color(0.45f, 0.4f, 0.35f), 0.25f),
                new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.3f, 0.25f), 0.75f),
                new GradientColorKey(new Color(0.06f, 0.08f, 0.16f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
    }
}
