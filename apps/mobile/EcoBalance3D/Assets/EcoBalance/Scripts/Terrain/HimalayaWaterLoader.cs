using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>One stream, lake outline or spring, as (u,v) points inside the terrain's box.</summary>
public class WaterFeature
{
    public string Name;
    public bool IsSpring;
    public bool IsArea;
    public bool IsRiver;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): wider than a stream
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
            // Main/configured server first, then known backups. POST + User-Agent avoids
            // some public Overpass blocks; first success is cached for later runs.
            string encoded = UnityWebRequest.EscapeURL(query);
            var body = Encoding.UTF8.GetBytes("data=" + encoded);
            string lastError = null;
            var servers = new List<string>();
            foreach (var endpoint in Endpoints(config.overpassUrl))
                servers.Add(endpoint);
            if (config.overpassBackupUrls != null)
            {
                foreach (var backup in config.overpassBackupUrls)
                {
                    if (!string.IsNullOrWhiteSpace(backup))
                        servers.Add(backup.Trim().TrimEnd('/'));
                }
            }

            foreach (var endpoint in servers)
            {
                if (string.IsNullOrEmpty(endpoint)) continue;
                using (var req = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.timeout = 15;
                    req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                    req.SetRequestHeader("User-Agent", "EcoBalance3D/1.0 (educational Unity client)");
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
                    {
                        json = req.downloadHandler.text;
                        lastError = null;
                        break;
                    }

                    lastError = $"{endpoint}: {req.error}";
                    Debug.LogWarning($"[Himalaya] Overpass request failed: {lastError}", this);
                }
            }

            if (json == null)
            {
                Debug.LogWarning(
                    $"[Himalaya] Overpass unavailable ({lastError}). Placing water in low valleys instead.",
                    this);
                done(new List<WaterFeature>());
                yield break;
            }

            if (config.useCache) GeoCache.WriteText(file, json);
        }

        done(Parse(json, b));
    }

    static IEnumerable<string> Endpoints(string configured)
    {
        var seen = new HashSet<string>();
        foreach (var url in new[]
        {
            configured,
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.osm.ch/api/interpreter",
            "http://overpass-api.de/api/interpreter"
        })
        {
            if (string.IsNullOrWhiteSpace(url))
                continue;

            var trimmed = url.Trim().TrimEnd('/');
            if (seen.Add(trimmed))
                yield return trimmed;
        }
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
                f.IsRiver = e.tags != null && e.tags.waterway == "river";
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
