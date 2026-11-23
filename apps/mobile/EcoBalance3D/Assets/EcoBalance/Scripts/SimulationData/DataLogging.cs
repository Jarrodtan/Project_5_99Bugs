using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class DataLogging : MonoBehaviour
{
    private struct Data
    {
        public int check;
        public int blue_sheep;
        public int snow_leopard;
        public int trees;
    }

    private List<Data> temp = new List<Data>();
    private bool recording = false;

    public void Start()
    {
        Record();
    }

    public void Record ()
    {
        temp.Clear();
        recording = true;
        Debug.Log("[DATA LOG] Data logging started.");
    }

    public void RecordTick(int curr, int sheep, int leopard, int tree_num)
    {
        if (!recording) return;
        temp.Add(new Data {check = curr, blue_sheep = sheep, snow_leopard = leopard, trees = tree_num});
    }

    // The run ends when the scene unloads (Back, Reset, stopping play, closing the app): save then,
    // so no End button is needed. Quiet if nothing was recorded.
    void OnDestroy()
    {
        if (recording && temp.Count > 0)
            ExportCSV();
    }

    public void ExportCSV()
    {
        if (!recording)
        {
            Debug.LogWarning("[DATA LOG] Cannot export: Recording was never started.");
            return;
        }
        if (temp.Count == 0)
        {
            Debug.LogWarning("[DATA LOG] Cannot export: 0 ticks were recorded. Are you calling RecordTick()?");
            return;
        }
        recording = false;

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Tick,Blue Sheep,Snow Leopard,Tree");

        foreach (var data in temp) {
            csv.AppendLine($"{data.check},{data.blue_sheep},{data.snow_leopard},{data.trees}");
        }

        // The Assets folder is read-only on a built app, so the tablet saves to its app data folder.
        string folder = Application.isEditor
            ? Path.Combine(Application.dataPath, "EcoBalance/Data/DataLogging")
            : Path.Combine(Application.persistentDataPath, "DataLogging");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string filepath = Path.Combine(folder, "Simulation_Run_Export.csv");
        File.WriteAllText(filepath, csv.ToString());

        Debug.Log($"[DATA LOG] Exported {temp.Count} ticks to: {filepath}");

    }

    public int GetPeakBS()
    {
        int max = 0;
        foreach (var data in temp)
        {
            if (data.blue_sheep > max) max = data.blue_sheep;
        }
        return max;
    }

    public int GetPeakSL()
    {
        int max = 0;
        foreach (var data in temp) {
            if (data.snow_leopard > max) max = data.snow_leopard;
        }
        return max;
    }

    public int GetTotalTicks()
    {
        return temp.Count;
    }
}
