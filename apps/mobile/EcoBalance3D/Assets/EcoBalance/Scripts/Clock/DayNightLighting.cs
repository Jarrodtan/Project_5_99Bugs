using UnityEngine;

/// <summary>
/// Drives the directional light (and Unity Procedural Skybox sun disk) from <see cref="IClockProvider"/>.
/// Night tints the same disk cool so it reads as the moon. No custom sun/moon meshes.
/// Personal prototype — not Feature Plan / T04 until approved.
/// </summary>
public class DayNightLighting : MonoBehaviour
{
    [Tooltip("Clock hub for colours and intensity.")]
    [SerializeField]
    ClockConfig config;

    [Tooltip("Must implement IClockProvider (usually ClockService).")]
    [SerializeField]
    MonoBehaviour providerBehaviour;

    [Tooltip("Scene directional light to rotate and tint (drives the skybox sun disk).")]
    [SerializeField]
    Light directionalLight;

    IClockProvider _provider;

    void Awake()
    {
        _provider = providerBehaviour as IClockProvider;
        if (config != null)
            config.EnsureDefaults();
    }

    void LateUpdate()
    {
        if (config == null || _provider == null)
            return;

        var time = Mathf.Clamp01(_provider.TimeOfDay01);
        var sunAngle = (time - config.dialZeroTimeOfDay) * 360f;

        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, config.lightYawDegrees, 0f);
            var colour = config.sunColorOverDay.Evaluate(time);
            var intensity = config.sunIntensityOverDay.Evaluate(time);
            if (!_provider.IsDaytime)
                intensity *= config.moonIntensity;

            directionalLight.color = colour;
            directionalLight.intensity = intensity;
        }

        RenderSettings.ambientLight = config.ambientColorOverDay.Evaluate(time);
    }
}
