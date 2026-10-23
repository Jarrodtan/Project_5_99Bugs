using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class TerrainConfig : MonoBehaviour
{
    public int spawn_trees = 0;
    public int spawn_rivers = 0;
    public int spawn_hills = 0;
    public int spawn_mud = 0;

    [System.Serializable] public class TerrainResponse { public TerrainResults results; }
    [System.Serializable] public class TerrainResults { public TerrainBinding[] bindings; }
    [System.Serializable] public class TerrainBinding { public TerrainValue elevation; }
    [System.Serializable] public class TerrainValue { public string value; }

    void Start()
    {
        StartCoroutine(FetchRealWorldTerrain());
    }

    IEnumerator FetchRealWorldTerrain()
    {
        // Query Wikidata for the elevation of Mount Everest (wd:Q178)
        string query = "SELECT ?elevation WHERE { wd:Q513 wdt:P2044 ?elevation. } LIMIT 1";
        string url = "https://query.wikidata.org/sparql?query="
                     + UnityWebRequest.EscapeURL(query) + "&format=json";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("User-Agent", "EcoSimUnityProject/1.0 (your@email.com)");
            req.SetRequestHeader("Accept", "application/sparql-results+json");
            yield return req.SendWebRequest();

            bool success = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[TERRAIN API] Request error: {req.responseCode} {req.error}");
            }
            else
            {
                string json = req.downloadHandler.text;
                Debug.Log($"[TERRAIN API] Raw response: {json}");

                TerrainResponse data = null;
                try
                {
                    data = JsonUtility.FromJson<TerrainResponse>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TERRAIN API] JSON parse exception: {e.Message}");
                }

                if (data != null && data.results != null
                    && data.results.bindings != null && data.results.bindings.Length > 0
                    && data.results.bindings[0].elevation != null)
                {
                    string raw = data.results.bindings[0].elevation.value;

                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float everestElevation))
                    {
                        // Scale real-world elevation down to simulation limits
                        spawn_trees  = Mathf.Clamp(Mathf.RoundToInt(everestElevation / 250f), 10, 50);
                        spawn_rivers = Mathf.Clamp(Mathf.RoundToInt(everestElevation / 3000f), 1, 4);
                        spawn_hills  = Mathf.Clamp(Mathf.RoundToInt(everestElevation / 1500f), 2, 10);
                        spawn_mud    = Mathf.Clamp(Mathf.RoundToInt(everestElevation / 4000f), 1, 5);

                        Debug.Log($"[TERRAIN API] Live Everest Elevation ({everestElevation}m) -> Trees: {spawn_trees}, Rivers: {spawn_rivers}, Hills: {spawn_hills}, Mud: {spawn_mud}");
                        success = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[TERRAIN API] Could not parse elevation value: '{raw}'");
                    }
                }
                else
                {
                    Debug.LogWarning("[TERRAIN API] No usable bindings in response.");
                }
            }

            if (!success)
            {
                Debug.LogWarning("[TERRAIN API] Live fetch failed. Using fallback.");
                spawn_trees = 20; spawn_rivers = 2; spawn_hills = 5; spawn_mud = 2;
            }
        }

        AutoBakeFloor floorBaker = FindAnyObjectByType<AutoBakeFloor>();
        if (floorBaker != null)
            floorBaker.Bake(spawn_trees, spawn_rivers, spawn_hills, spawn_mud);
    }
}
