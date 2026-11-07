using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable] public class WeatherResponse
{
    public MainWeather main;
    public List<WeatherCondition> weather;
}

[System.Serializable] public class MainWeather
{
    public float temp;
}

[System.Serializable] public class WeatherCondition
{
    public string main;
    public string description;
}

public class WeatherManager : MonoBehaviour
{
    public string api_key = "b0c376cfe1ec96717743554c9f4d5356";
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
        string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={api_key}&units=metric";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("API connection failed: " + req.error);
            }
            else
            {
                WeatherResponse data = JsonUtility.FromJson<WeatherResponse>(req.downloadHandler.text);
                if (data != null && data.main != null && data.weather != null && data.weather.Count > 0)
                {
                    float curr_temp = data.main.temp;
                    string condi = data.weather[0].main;
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

}
