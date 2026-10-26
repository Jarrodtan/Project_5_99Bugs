using UnityEngine;

/// <summary>
/// Tints the surrounding ocean material and scene fog from <see cref="IClockProvider"/> /
/// <see cref="ClockConfig"/>. Lives on the ocean plane — not part of the island prefab —
/// so it survives island swaps. Personal prototype — not Feature Plan / T04 until approved.
/// </summary>
public class OceanAtmosphere : MonoBehaviour
{
    [Tooltip("Clock hub for day/night colour curves.")]
    [SerializeField]
    ClockConfig config;

    [Tooltip("Must implement IClockProvider (usually ClockService).")]
    [SerializeField]
    MonoBehaviour providerBehaviour;

    [Tooltip("Ocean mesh renderer. When empty, uses a renderer on this object.")]
    [SerializeField]
    Renderer oceanRenderer;

    [Tooltip("Daytime fog density (exponential squared).")]
    [SerializeField]
    [Min(0f)]
    float dayFogDensity = 0.012f;

    [Tooltip("Nighttime fog density (exponential squared).")]
    [SerializeField]
    [Min(0f)]
    float nightFogDensity = 0.02f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");

    IClockProvider _provider;
    MaterialPropertyBlock _block;
    Color _shallow;
    Color _deep;

    void Awake()
    {
        _provider = providerBehaviour as IClockProvider;
        if (oceanRenderer == null)
            oceanRenderer = GetComponent<Renderer>();

        _block = new MaterialPropertyBlock();
        CacheMaterialColours();
        ApplyFogSettings();
    }

    void LateUpdate()
    {
        if (config == null || _provider == null)
            return;

        var time = Mathf.Clamp01(_provider.TimeOfDay01);
        var ambient = config.ambientColorOverDay.Evaluate(time);
        var dayFactor = _provider.IsDaytime ? 1f : 0.35f;

        var shallow = Color.Lerp(_shallow, ambient, 0.45f);
        shallow.a = _shallow.a;
        var deep = Color.Lerp(_deep, ambient * 0.55f, 0.5f);
        deep.a = _deep.a;

        if (oceanRenderer != null)
        {
            oceanRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, shallow);
            _block.SetColor(DeepColorId, deep);
            oceanRenderer.SetPropertyBlock(_block);
        }

        var fog = Color.Lerp(ambient, shallow, 0.55f);
        fog *= dayFactor;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, _provider.IsDaytime ? 1f : 0f);
    }

    void CacheMaterialColours()
    {
        _shallow = new Color(0.12f, 0.42f, 0.58f, 0.88f);
        _deep = new Color(0.02f, 0.12f, 0.28f, 0.95f);
        if (oceanRenderer == null)
            return;

        var shared = oceanRenderer.sharedMaterial;
        if (shared == null)
            return;

        if (shared.HasProperty(BaseColorId))
            _shallow = shared.GetColor(BaseColorId);
        if (shared.HasProperty(DeepColorId))
            _deep = shared.GetColor(DeepColorId);
    }

    void ApplyFogSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = dayFogDensity;
    }
}
