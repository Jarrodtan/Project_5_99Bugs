using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-center day-night dial and labels. Reads <see cref="IClockProvider"/> only —
/// never advances time. Personal prototype — not Feature Plan / T04 work until approved.
/// </summary>
public class ClockHudView : MonoBehaviour
{
    [Tooltip("Clock hub for label formats and dial radius.")]
    [SerializeField]
    ClockConfig config;

    [Tooltip("Must implement IClockProvider (usually ClockService).")]
    [SerializeField]
    MonoBehaviour providerBehaviour;

    [Tooltip("Optional. Sun icon on the dial.")]
    [SerializeField]
    RectTransform sunIcon;

    [Tooltip("Optional. Moon icon on the dial (opposite the sun).")]
    [SerializeField]
    RectTransform moonIcon;

    [Tooltip("Day number label.")]
    [SerializeField]
    Text dayLabel;

    [Tooltip("Time of day label.")]
    [SerializeField]
    Text timeLabel;

    IClockProvider _provider;

    void Awake()
    {
        _provider = providerBehaviour as IClockProvider;
        if (config != null)
            config.EnsureDefaults();
        DisableRaycasts();
    }

    void LateUpdate()
    {
        if (config == null || _provider == null)
            return;

        var time = Mathf.Clamp01(_provider.TimeOfDay01);
        if (dayLabel != null)
            dayLabel.text = string.Format(config.dayLabelFormat, _provider.Day);

        if (timeLabel != null)
        {
            var totalMinutes = Mathf.FloorToInt(time * 24f * 60f);
            var hours = (totalMinutes / 60) % 24;
            var minutes = totalMinutes % 60;
            timeLabel.text = string.Format(config.timeLabelFormat, hours, minutes);
        }

        PlaceDialIcons(time);
    }

    void PlaceDialIcons(float timeOfDay)
    {
        // −90° so dialZero (sunrise) sits on the left, moon opposite on the right.
        var angle = (timeOfDay - config.dialZeroTimeOfDay) * 360f - 90f;
        var radius = config.dialRadiusPixels;
        if (sunIcon != null)
            sunIcon.anchoredPosition = PointOnDial(angle, radius);

        if (moonIcon != null)
            moonIcon.anchoredPosition = PointOnDial(angle + 180f, radius);
    }

    static Vector2 PointOnDial(float degrees, float radius)
    {
        var radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
    }

    void DisableRaycasts()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        for (var i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
