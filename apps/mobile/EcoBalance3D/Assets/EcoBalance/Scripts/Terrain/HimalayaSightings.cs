using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SpeciesSightingSource
{
    public SpeciesDefinition species;
    [Tooltip("e.g. Panthera uncia (snow leopard), Pseudois nayaur (blue sheep / bharal)")]
    public string scientificName;
}

/// <summary>
/// Downloads real sighting coordinates from GBIF for each species. Use TryGetSpawnPoint to start animals
/// where the real ones have been seen. A small box may return few or no records; nothing breaks if so.
/// </summary>
public class HimalayaSightings : MonoBehaviour
{
    [SerializeField] HimalayaConfig config;
    [SerializeField] List<SpeciesSightingSource> sources = new List<SpeciesSightingSource>();
    [Range(1, 300)] [SerializeField] int limit = 300;

    readonly Dictionary<SpeciesDefinition, List<Vector2>> uvBySpecies = new Dictionary<SpeciesDefinition, List<Vector2>>();

    public IEnumerator FetchAll()
    {
        uvBySpecies.Clear();
        foreach (var s in sources)
            if (s != null && s.species != null && !string.IsNullOrWhiteSpace(s.scientificName))
                yield return Fetch(s);
    }

    IEnumerator Fetch(SpeciesSightingSource source)
    {
        var b = config.Bounds;
        string file = $"gbif_{source.scientificName.Replace(' ', '_')}_{b.Key}.json";
        string json = null;

        if (config.useCache && GeoCache.TryReadText(file, out var cached))
            json = cached;
        else
        {
            string url = string.Format(CultureInfo.InvariantCulture,
                "https://api.gbif.org/v1/occurrence/search?scientificName={0}&hasCoordinate=true" +
                "&decimalLatitude={1},{2}&decimalLongitude={3},{4}&limit={5}",
                UnityWebRequest.EscapeURL(source.scientificName), b.South, b.North, b.West, b.East, limit);

            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 15;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): a slow server must not stop the world from finishing
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Himalaya] GBIF request failed for {source.scientificName}: {req.error}", this);
                    yield break;
                }
                json = req.downloadHandler.text;
            }
            if (config.useCache) GeoCache.WriteText(file, json);
        }

        var data = JsonUtility.FromJson<GbifResponse>(json);
        var list = new List<Vector2>();
        if (data?.results != null)
            foreach (var r in data.results)
                list.Add(b.ToUV(r.decimalLatitude, r.decimalLongitude));
        uvBySpecies[source.species] = list;
        Debug.Log($"[Himalaya] {source.scientificName}: {list.Count} sightings");
    }

    /// <summary>A random real sighting for this species as a world position, or false if there are none.</summary>
    public bool TryGetSpawnPoint(SpeciesDefinition species, HimalayaTerrainBuilder tb, out Vector3 position)
    {
        position = default;
        if (species == null || !uvBySpecies.TryGetValue(species, out var list) || list.Count == 0) return false;
        var uv = list[UnityEngine.Random.Range(0, list.Count)];
        position = tb.WorldPosition(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
        return true;
    }

    [Serializable] public class GbifResponse { public GbifRecord[] results; }
    [Serializable] public class GbifRecord { public double decimalLatitude; public double decimalLongitude; }
}
