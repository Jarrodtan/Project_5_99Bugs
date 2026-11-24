using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class DataLogging : MonoBehaviour
{
    private struct Data
    {
        public int check;
        public int day;
        public int blue_sheep;
        public int snow_leopard;
        public int trees;
    }

    // Only used as a fallback when RecordTick isn't given the clock's day.
    public int ticksPerDay = 30;

    string runMode = "Free";

    private List<Data> temp = new List<Data>();
    private bool recording = false;
    private bool saved = false;

    public void Start()
    {
        Record();
    }

    public void Record()
    {
        temp.Clear();
        recording = true;
        saved = false;
        Debug.Log("[DATA LOG] Data logging started.");
    }

    // day is optional: pass the clock's current day if you can. If not, it is worked out from the tick.
    public void RecordTick(int curr, int sheep, int leopard, int tree_num, int day = -1)
    {
        if (!recording) return;
        if (temp.Count == 0)
            runMode = FieldJournalObservations.WasGuided ? "Guided" : "Free";
        if (day < 0) day = curr / Mathf.Max(1, ticksPerDay);
        temp.Add(new Data { check = curr, day = day, blue_sheep = sheep, snow_leopard = leopard, trees = tree_num });
    }

    void OnDestroy() { ExportCSV(); }

    void OnApplicationPause(bool paused)
    {
        if (paused) ExportCSV();
    }

    public void ExportCSV()
    {
        if (!recording || saved || temp.Count == 0) return;
        saved = true;
        recording = false;

        MyRuns run = BuildRecord(runMode);
        RunHist.Add(run);
        WriteCsvFiles(run);
    }

    void WriteCsvFiles(MyRuns r)
    {
        string folder = Application.isEditor
            ? Path.Combine(Application.dataPath, "EcoBalance/Data/DataLogging")
            : Path.Combine(Application.persistentDataPath, "DataLogging");
        Directory.CreateDirectory(folder);

        var csv = new StringBuilder();
        csv.AppendLine("RunId,Mode,Tick,Day,BlueSheep,SnowLeopard,Trees,TotalAnimals");
        for (int i = 0; i < r.t.Count; i++)
        {
            csv.AppendLine($"{r.id},{r.mode},{r.t[i]},{r.days[i]},{r.sheep[i]},{r.leopard[i]},{r.trees[i]},{r.sheep[i] + r.leopard[i]}");
        }
        string runFile = Path.Combine(folder, $"Run_{r.id}.csv");
        File.WriteAllText(runFile, csv.ToString());

        string summaryFile = Path.Combine(folder, "Runs_Summary.csv");
        var sum = new StringBuilder();
        if (!File.Exists(summaryFile))
            sum.AppendLine("RunId,Date,Mode,DaysSurvived,Outcome,PeakSheep,PeakLeopard,EndSheep,EndLeopard,EndTrees,CasesUnlocked,Stars");
        sum.AppendLine($"{r.id},{r.start},{r.mode},{r.days_survived},{r.outcome},{r.peaksheep},{r.peakleopard},{r.endsheep},{r.endleopard},{r.endtrees},{r.cases.Count},{r.stars}");
        File.AppendAllText(summaryFile, sum.ToString());

        Debug.Log($"[DATA LOG] Saved {r.ticks} ticks to: {runFile}");
    }

    MyRuns BuildRecord(string mode)
    {
        var r = new MyRuns
        {
            id = System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            start = System.DateTime.Now.ToString("s"),
            mode = mode,
            ticks = temp.Count
        };
        foreach (var d in temp)
        {
            r.t.Add(d.check); r.days.Add(d.day);
            r.sheep.Add(d.blue_sheep); r.leopard.Add(d.snow_leopard); r.trees.Add(d.trees);
            if (r.outcome == null && (d.blue_sheep == 0 || d.snow_leopard == 0))
            {
                r.days_survived = d.day;
                r.outcome = d.blue_sheep == 0 ? "SheepDied" : "LeopardDied";
            }
        }
        var last = temp[temp.Count - 1];
        if (r.outcome == null)
        {
            r.outcome = "BothSurvived";
            r.days_survived = last.day;
        }
        r.peaksheep = GetPeakBS(); r.peakleopard = GetPeakSL();
        r.endsheep = last.blue_sheep; r.endleopard = last.snow_leopard; r.endtrees = last.trees;
        r.stars = RunRewards.Stars(r);
        return r;
    }

    public int GetPeakBS()
    {
        int max = 0;
        foreach (var d in temp) if (d.blue_sheep > max) max = d.blue_sheep;
        return max;
    }

    public int GetPeakSL()
    {
        int max = 0;
        foreach (var d in temp) if (d.snow_leopard > max) max = d.snow_leopard;
        return max;
    }

    public int GetTotalTicks() { return temp.Count; }
}
