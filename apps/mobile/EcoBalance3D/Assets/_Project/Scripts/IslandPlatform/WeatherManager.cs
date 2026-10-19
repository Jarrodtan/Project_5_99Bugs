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

    void Start()
    {
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
                    if (txt != null)
                    {
                        txt.text = $"Himalayas Live Temp: {curr_temp}°C\nCondition: {condi}";
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
