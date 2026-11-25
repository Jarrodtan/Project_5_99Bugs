using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// [AI-GENERATED] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
// Live weather from Open-Meteo (https://open-meteo.com): free, no API key, data CC BY 4.0.
// (Was OpenWeatherMap, which needed a key that could not be kept secret in a public repo.)
[System.Serializable] public class OpenMeteoResponse
{
    public OpenMeteoCurrent current;
}

[System.Serializable] public class OpenMeteoCurrent
{
    public float temperature_2m;
    public int weather_code;   // WMO weather code
}

public class WeatherManager : MonoBehaviour
{
    private string lat = "27.9881";
    private string lon = "86.9250";

    public TextMeshProUGUI txt;

    /// <summary>Latest live condition ("Clear", "Rain", "Snow", ...) and temperature, or null before the first answer.</summary>
    public static string LiveCondition;   // [AI-GENERATED] Claude Code (Claude Opus 5.5)
    public static float LiveTemperature;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // The lat/lon above is Everest's summit (8,849 m). Air warms about 6.5 °C per 1,000 m lower down,
    // so the valleys where the herds live (~4,500 m) are much warmer than the summit reading.
    const float SummitMetres = 8849f, ValleyMetres = 4500f, LapseRatePerMetre = 0.0065f;
    public static float ValleyTemperature => LiveTemperature + (SummitMetres - ValleyMetres) * LapseRatePerMetre;
    public static bool HasLiveData => LiveCondition != null;

    // The text was placed from the screen centre and got cut off at the bottom; pin it to the bottom-left corner.
    void PinText()
    {
        if (txt == null) return;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(40f, 150f);   // above the speed buttons' row, so they never overlap
        rt.sizeDelta = new Vector2(700f, 130f);
        rt.localScale = Vector3.one;
        txt.enableAutoSizing = true;
        txt.fontSizeMax = 40f;
        txt.fontSizeMin = 22f;
        txt.alignment = TextAlignmentOptions.BottomLeft;
    }
    // [AI-GENERATED END]

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): hidden behind the Free setup menu; the run shows it.
    void Update()
    {
        if (txt == null) return;
        bool show = !ConfigurationScreen.Open;
        if (txt.enabled != show) txt.enabled = show;
    }

    void Start()
    {
        PinText();
        StartCoroutine(FetchHimalayanWeather());
    }

    IEnumerator FetchHimalayanWeather()
    {
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("API connection failed: " + req.error);
                if (txt != null) txt.text = "Live Everest weather: offline (no internet)";   // [AI-GENERATED] Claude Code (Claude Opus 5.5): say so instead of a stale placeholder
            }
            else
            {
                OpenMeteoResponse data = JsonUtility.FromJson<OpenMeteoResponse>(req.downloadHandler.text);
                if (data != null && data.current != null)
                {
                    float curr_temp = data.current.temperature_2m;
                    string condi = Condition(data.current.weather_code);
                    LiveCondition = condi;     // [AI-GENERATED] Claude Code (Claude Opus 5.5): SeasonWeather starts from the real weather
                    LiveTemperature = curr_temp;
                    if (txt != null)
                    {
                        txt.text = $"Live Everest summit: {curr_temp:0}°C, {condi}\nValleys (about 4,500 m): {ValleyTemperature:0}°C";
                    }
                    Debug.Log($"Live Himalayan Temp: {curr_temp}°C, Condition: {condi}");

                    // --- For generation of terrain ; Do it here onwards ---
                    // Example (from AI):
                    // if (condition == "Snow") { TeammateScript.SpawnSnowParticles(); }
                    // else if (condition == "Thunderstorm") { TeammateScript.StartStorm(); }

                }
                else
                {
                    Debug.LogError("Failed to parse weather data. API Response: " + req.downloadHandler.text);
                }                
            }
        }
    }

    // WMO weather code -> the same words OpenWeatherMap used ("Clear", "Clouds", "Rain", "Snow"...),
    // so SeasonWeather.FromLive keeps working unchanged.
    static string Condition(int code)
    {
        if (code == 0) return "Clear";
        if (code <= 3) return "Clouds";
        if (code == 45 || code == 48) return "Fog";
        if (code >= 51 && code <= 57) return "Drizzle";
        if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return "Rain";
        if ((code >= 71 && code <= 77) || code == 85 || code == 86) return "Snow";
        if (code >= 95) return "Thunderstorm";
        return "Clouds";
    }
}
