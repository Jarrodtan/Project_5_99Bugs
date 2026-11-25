using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class MyRuns
{
    public string id;
    public string start;
    public string mode;
    public int ticks;
    public int days_survived;
    public string outcome;
    public int peaksheep, peakleopard, endsheep, endleopard, endtrees;
    public List<string> cases = new();   // was "casses"
    public int stars;
    public List<int> t = new(), sheep = new(), leopard = new(), trees = new();
    public List<int> days = new();
}

[System.Serializable]
public class RunData
{
    public List<MyRuns> runs = new();
}

public static class RunHist
{
    public const int keep = 10;
    static string filepath => Path.Combine(Application.persistentDataPath, "runs.json");

    public static RunData Load()
    {
        try
        {
            return File.Exists(filepath)
                ? JsonUtility.FromJson<RunData>(File.ReadAllText(filepath)) ?? new RunData()
                : new RunData();
        }
        catch { return new RunData(); }   // corrupt file: start fresh
    }

    public static void Add(MyRuns r)
    {
        var s = Load();
        s.runs.Add(r);
        while (s.runs.Count > keep) s.runs.RemoveAt(0);
        File.WriteAllText(filepath, JsonUtility.ToJson(s));
    }

    public static MyRuns previous(string currID = null)
    {
        var runs = Load().runs;
        for (int i = runs.Count - 1; i >= 0; i--)
        {
            if (runs[i].id != currID) return runs[i];
        }
        return null;   // moved outside the loop
    }
}
