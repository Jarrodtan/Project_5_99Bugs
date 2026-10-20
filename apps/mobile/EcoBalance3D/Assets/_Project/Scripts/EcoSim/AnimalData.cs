using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class Response
{
    public string name;
    public Results[] result;
}

[System.Serializable]
public class Results
{
    public string scientific_name;
    public string category;
    public string population_trend;
}

public class AnimalData : MonoBehaviour
{
    public string api_key = "rlts_EOiBtfcYU3U1A92F8iNFAChLp7SmHU0bj2E4";
    [System.NonSerialized] public Dictionary<string, int> calc_pop = new Dictionary<string, int>();
    public SpeciesCounter snowLeopardCounter;
    public SpeciesCounter blueSheepCounter;

    void Start()
    {
        StartCoroutine(FetchSpeciesData("Panthera uncia", "SnowLeopard"));
        StartCoroutine(FetchSpeciesData("Pseudois nayaur", "BlueSheep"));
        StartCoroutine(FetchSpeciesData("Pinus wallichiana", "Tree"));
    }

    IEnumerator FetchSpeciesData(string anim_name, string simu_name)
    {
        string formattedName = anim_name.Replace(" ", "%20");
        string url = $"https://api.iucnredlist.org/api/v4/taxa/scientific_name?scientific_name={formattedName}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + api_key);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json_txt = req.downloadHandler.text;
                Response data = JsonUtility.FromJson<Response>(json_txt);

                if (data != null && data.result != null && data.result.Length > 0)
                {
                    CalculateGamePopulation(simu_name, data.result[0]);
                    yield break; 
                }
            }

            Debug.LogWarning($"[IUCN API] Live fetch failed for {anim_name} ({req.error}). Using fallback data.");

            Results mockData = new Results();
            mockData.scientific_name = anim_name;
            mockData.population_trend = "Stable";
            if (simu_name == "SnowLeopard") mockData.category = "VU";
            else if (simu_name == "BlueSheep") mockData.category = "LC";
            else mockData.category = "LC";

            CalculateGamePopulation(simu_name, mockData);
        }

    }


    void CalculateGamePopulation(string simu_name, Results data)
    {
        int start_num = 100;

        switch (data.category)
        {
            case "EX": // Extinct
            case "EW": // Extinct in the Wild
                start_num = 0;
                break;
            case "CR": // Critically Endangered
                start_num = 2;
                break;
            case "EN": // Endangered
                start_num = 5;
                break;
            case "VU": // Vulnerable (Snow Leopard)
                start_num = 10;
                break;
            case "NT": // Near Threatened
                start_num = 40;
                break;
            case "LC": // Least Concern (Blue Sheep / Pine)
                start_num = 100;
                break;
        }

        calc_pop[simu_name] = start_num;
        Debug.Log($"[IUCN API] {simu_name} ({data.category}, {data.population_trend}) -> Game Spawn: {start_num}");

        if (simu_name == "SnowLeopard" && snowLeopardCounter != null)
        {
            snowLeopardCounter.SetCount(start_num);
            // NEW: Push directly to the Simulator overrides instantly
            EcosystemSimulator.StartingPopulationOverrides["Snow Leopard"] = start_num;
        }
        else if (simu_name == "BlueSheep" && blueSheepCounter != null)
        {
            blueSheepCounter.SetCount(start_num);
            // NEW: Push directly to the Simulator overrides instantly (Mind the space!)
            EcosystemSimulator.StartingPopulationOverrides["Blue Sheep"] = start_num;
        }

        // NEW: Force the top-left panel to refresh right now in the Config scene
        EcosystemSimulator simulator = FindAnyObjectByType<EcosystemSimulator>();
        if (simulator != null)
        {
            simulator.ResetSimulation();
        }

    }

}
