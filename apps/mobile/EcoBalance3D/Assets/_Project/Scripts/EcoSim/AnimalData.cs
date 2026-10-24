using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SpeciesStatusData
{
    public string scientific_name;
    public string category;
    public string population_trend;
}

[System.Serializable]
public class WikiResponse
{
    public WikiResults results;
}

[System.Serializable]
public class WikiResults
{
    public WikiBinding[] bindings;
}

[System.Serializable]
public class WikiBinding
{
    public WikiStatusLabel statusLabel;
}

[System.Serializable]
public class WikiStatusLabel
{
    public string value;
}

public class AnimalData : MonoBehaviour
{
    [System.NonSerialized] public Dictionary<string, int> calc_pop = new Dictionary<string, int>();

    public SpeciesCounter snowLeopardCounter;
    public SpeciesCounter blueSheepCounter;

    void Start()
    {
        StartCoroutine(FetchAllData());
    }

    IEnumerator FetchAllData()
    {
        yield return StartCoroutine(FetchSpeciesData("Panthera uncia", "SnowLeopard"));
        yield return StartCoroutine(FetchSpeciesData("Pseudois nayaur", "BlueSheep"));
        yield return StartCoroutine(FetchSpeciesData("Pinus wallichiana", "Tree"));

        EcosystemSimulator simulator = FindAnyObjectByType<EcosystemSimulator>();
        if (simulator != null)
        {
            simulator.ResetSimulation();
        }
    }

    IEnumerator FetchSpeciesData(string anim_name, string simu_name)
    {
        string query = $"SELECT ?statusLabel WHERE {{ ?taxon wdt:P225 \"{anim_name}\". ?taxon wdt:P141 ?status. SERVICE wikibase:label {{ bd:serviceParam wikibase:language \"en\". }} }}";
        string url = "https://query.wikidata.org/sparql?query=" + UnityWebRequest.EscapeURL(query) + "&format=json";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("User-Agent", "EcoSimUnityProject/1.0");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                WikiResponse wikiData = JsonUtility.FromJson<WikiResponse>(req.downloadHandler.text);

                if (wikiData?.results?.bindings != null && wikiData.results.bindings.Length > 0)
                {
                    string rawStatus = wikiData.results.bindings[0].statusLabel.value.ToLower();
                    string category = "LC";

                    if (rawStatus.Contains("critically endangered")) category = "CR";
                    else if (rawStatus.Contains("endangered")) category = "EN";
                    else if (rawStatus.Contains("vulnerable")) category = "VU";
                    else if (rawStatus.Contains("near threatened")) category = "NT";

                    SpeciesStatusData liveData = new SpeciesStatusData
                    {
                        scientific_name = anim_name,
                        category = category,
                        population_trend = "Stable"
                    };

                    Debug.Log($"[WIKIDATA API] Live fetch success: {anim_name} is {category}");
                    CalculateGamePopulation(simu_name, liveData);
                    yield break;
                }
            }

            Debug.LogWarning($"[WIKIDATA API] Fetch failed for {anim_name} ({req.error}). Using fallback.");

            SpeciesStatusData fallback = new SpeciesStatusData
            {
                scientific_name = anim_name,
                population_trend = "Stable",
                category = (simu_name == "SnowLeopard") ? "VU" : "LC"
            };

            CalculateGamePopulation(simu_name, fallback);
        }
    }

    void CalculateGamePopulation(string simu_name, SpeciesStatusData data)
    {
        int start_num = 100;

        switch (data.category)
        {
            case "EX":
            case "EW": start_num = 0; break;
            case "CR": start_num = 2; break;
            case "EN": start_num = 5; break;
            case "VU": start_num = 10; break;
            case "NT": start_num = 40; break;
            case "LC": start_num = 100; break;
        }

        calc_pop[simu_name] = start_num;
        Debug.Log($"[CONSERVATION DATA] {simu_name} ({data.category}, {data.population_trend}) -> Game Spawn: {start_num}");

        if (simu_name == "SnowLeopard" && snowLeopardCounter != null)
        {
            snowLeopardCounter.SetCount(start_num);
            // Bulletproof dictionary mapping (covers both spelling variations)
            EcosystemSimulator.StartingPopulationOverrides["SnowLeopard"] = start_num;
            EcosystemSimulator.StartingPopulationOverrides["Snow Leopard"] = start_num;
        }
        else if (simu_name == "BlueSheep" && blueSheepCounter != null)
        {
            blueSheepCounter.SetCount(start_num);
            EcosystemSimulator.StartingPopulationOverrides["BlueSheep"] = start_num;
            EcosystemSimulator.StartingPopulationOverrides["Blue Sheep"] = start_num;
        }
    }
}
