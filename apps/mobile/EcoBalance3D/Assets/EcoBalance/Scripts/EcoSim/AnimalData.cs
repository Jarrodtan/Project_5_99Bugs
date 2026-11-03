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
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the live status now picks a share of the counter's maximum
        // (Shumin's research: up to 20 Blue Sheep and 5 Snow Leopards), so the defaults come out at the research's
        // 10 sheep (Least Concern) and 2 leopards (Vulnerable) instead of 100 and 10.
        float share = 0.5f;
        switch (data.category)
        {
            case "EX":
            case "EW": share = 0f; break;
            case "CR": share = 0.1f; break;
            case "EN": share = 0.25f; break;
            case "VU": share = 0.4f; break;
            case "NT": share = 0.45f; break;
            case "LC": share = 0.5f; break;
        }
        // The share is of the research herd size (20 sheep / 5 leopards), not of the counter's maximum, so
        // raising the Free maximum (100 each since 3 Nov) keeps the defaults at 10 sheep and 2 leopards.
        var counter = simu_name == "SnowLeopard" ? snowLeopardCounter : blueSheepCounter;
        int reference = simu_name == "SnowLeopard" ? 5 : 20;
        int start_num = Mathf.Max(share > 0f ? 1 : 0, Mathf.RoundToInt(reference * share));
        if (counter != null) start_num = Mathf.Min(start_num, counter.maxCount);

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
