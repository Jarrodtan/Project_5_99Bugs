using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>One stream, lake outline or spring, as (u,v) points inside the terrain's box.</summary>
public class WaterFeature
{
    public string Name;
    public bool IsSpring;
    public bool IsArea;
    public readonly List<Vector2> UV = new List<Vector2>();
}

/// <summary>Fetches rivers, streams, lakes and springs from the OpenStreetMap Overpass API.</summary>
public class HimalayaWaterLoader : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;

    public IEnumerator Fetch(Action<List<WaterFeature>> done)
    {
        var b = config.Bounds;
        string file = $"overpass_{b.Key}.json";
        string json = null;

        if (config.useCache && GeoCache.TryReadText(file, out var cached))
            json = cached;
        else
        {
            string box = b.ToOverpass();
            string query = "[out:json][timeout:25];(" +
                           $"way[\"waterway\"~\"river|stream\"]({box});" +
                           $"way[\"natural\"=\"water\"]({box});" +
                           $"node[\"natural\"=\"spring\"]({box});" +
                           ");out geom;";
            string url = config.overpassUrl + "?data=" + UnityWebRequest.EscapeURL(query);

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Himalaya] Overpass request failed: {req.error}", this);
                    done(new List<WaterFeature>());
                    yield break;
                }
                json = req.downloadHandler.text;
            }
            if (config.useCache) GeoCache.WriteText(file, json);
        }

        done(Parse(json, b));
    }

    static List<WaterFeature> Parse(string json, GeoBounds b)
    {
        var result = new List<WaterFeature>();
        var data = JsonUtility.FromJson<OverpassResponse>(json);
        if (data?.elements == null) return result;

        foreach (var e in data.elements)
        {
            var f = new WaterFeature { Name = e.tags != null ? e.tags.name : null };
            if (e.type == "node")
            {
                f.IsSpring = true;
                f.UV.Add(b.ToUV(e.lat, e.lon));
            }
            else if (e.geometry != null && e.geometry.Length > 1)
            {
                foreach (var p in e.geometry) f.UV.Add(b.ToUV(p.lat, p.lon));
                f.IsArea = e.tags != null && e.tags.natural == "water";
            }
            else continue;
            result.Add(f);
        }
        Debug.Log($"[Himalaya] Water features: {result.Count}");
        return result;
    }

    // JsonUtility needs classes that match the JSON field names; unknown fields are ignored.
    [Serializable] public class OverpassResponse { public OverpassElement[] elements; }
    [Serializable] public class OverpassElement
    {
        public string type;
        public double lat, lon;                 // nodes (springs)
        public OverpassPoint[] geometry;        // ways
        public OverpassTags tags;
    }
    [Serializable] public class OverpassPoint { public double lat, lon; }
    [Serializable] public class OverpassTags { public string waterway; public string natural; public string name; }
}
