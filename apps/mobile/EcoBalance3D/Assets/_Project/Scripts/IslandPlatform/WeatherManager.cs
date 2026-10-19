using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Net;
using TMPro;

[System.Serializable]
public class WeatherResponse
{
    public MainWeather main;
}

[System.Serializable]
public class MainWeather
{
    public float temp;
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
                if (data != null && data.main != null)
                {
                    float curr_temp = data.main.temp;
                    if (txt != null)
                    {
                        txt.text = $"Himalayas Live Temp: {curr_temp}°C";
                    }
                    Debug.Log($"Live Himalayan Temperature: {curr_temp}°C");
                }
                else
                {
                    Debug.LogError("Failed to parse weather data. API Response: " + req.downloadHandler.text);
                }
                // --- For generation of terrain ; Do it here onwards ---
                
            }
        }
    }

}
