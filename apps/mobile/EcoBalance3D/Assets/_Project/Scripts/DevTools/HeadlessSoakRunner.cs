// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Headless soak test (rubric: stability/soak tests; run on the team VM by tools/vm/soak.sh).
/// Does nothing unless the player is started with <c>-soakDays N</c>, e.g.
/// <c>./EcoBalanceHeadless.x86_64 -batchmode -nographics -soakDays 2000 -soakReport report.json</c>.
/// It loads the simulation scene, runs it fast for N simulated days with no graphics, counts errors and
/// exceptions, tracks memory and populations, writes a JSON report and quits with an exit code:
/// 0 = passed, 1 = errors were logged, 2 = no simulator found, 3 = timed out.
/// </summary>
public class HeadlessSoakRunner : MonoBehaviour
{
    [Serializable]
    class Report
    {
        public string result;
        public int exitCode;
        public int targetDays;
        public int daysReached;
        public float realSeconds;
        public float daysPerSecond;
        public int errorCount;
        public List<string> firstErrors = new();
        public long startMemoryMB;
        public long peakMemoryMB;
        public long endMemoryMB;
        public List<string> finalPopulations = new();
        public List<string> extinct = new();
        public string unityVersion;
        public string buildGuid;
    }

    int targetDays;
    string reportPath;
    string sceneName;
    float timeScale;
    float maxSeconds;
    float started;
    EcosystemSimulator sim;
    readonly Report report = new();
    bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        int days = (int)ArgFloat("-soakDays", 0);
        if (days <= 0) return;
        var go = new GameObject("[HeadlessSoak]");
        DontDestroyOnLoad(go);
        var r = go.AddComponent<HeadlessSoakRunner>();
        r.targetDays = days;
        r.reportPath = Arg("-soakReport") ?? "soak-report.json";
        r.sceneName = Arg("-soakScene") ?? "SampleScene";
        r.timeScale = ArgFloat("-soakTimeScale", 10f);
        r.maxSeconds = ArgFloat("-soakMaxSeconds", 3600f);
    }

    void Start()
    {
        Application.logMessageReceived += OnLog;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Time.timeScale = timeScale;
        started = Time.realtimeSinceStartup;
        report.targetDays = targetDays;
        report.unityVersion = Application.unityVersion;
        report.buildGuid = Application.buildGUID;
        report.startMemoryMB = MemoryMB();
        report.peakMemoryMB = report.startMemoryMB;
        Debug.Log($"[Soak] {targetDays} days of '{sceneName}' at timeScale {timeScale}, report -> {reportPath}");
        SceneManager.LoadScene(sceneName);
    }

    void Update()
    {
        if (finished) return;
        float elapsed = Time.realtimeSinceStartup - started;
        report.peakMemoryMB = Math.Max(report.peakMemoryMB, MemoryMB());

        if (sim == null)
        {
            sim = FindAnyObjectByType<EcosystemSimulator>();
            if (sim == null && elapsed > 60f) Finish(2, "no EcosystemSimulator found in the scene");
            return;
        }
        if (sim.TickCount >= targetDays) { Finish(report.errorCount > 0 ? 1 : 0, null); return; }
        if (elapsed > maxSeconds) Finish(3, $"timed out after {maxSeconds:0} s at day {sim.TickCount}");
    }

    void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        report.errorCount++;
        if (report.firstErrors.Count < 20) report.firstErrors.Add($"{type}: {message}");
    }

    void Finish(int code, string reason)
    {
        finished = true;
        float elapsed = Time.realtimeSinceStartup - started;
        report.exitCode = code;
        report.result = code == 0 ? "passed" : reason ?? $"{report.errorCount} errors logged";
        report.realSeconds = elapsed;
        report.daysReached = sim != null ? sim.TickCount : 0;
        report.daysPerSecond = elapsed > 0 ? report.daysReached / elapsed : 0;
        report.endMemoryMB = MemoryMB();
        if (sim != null)
            foreach (var kv in sim.populations)
                if (kv.Key != null)
                {
                    report.finalPopulations.Add($"{kv.Key.name}={kv.Value:0}");
                    if (kv.Value < 1f) report.extinct.Add(kv.Key.name);
                }
        try
        {
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Soak] could not write report: {e.Message}");
        }
        Debug.Log($"[Soak] {report.result}: day {report.daysReached}/{targetDays} in {elapsed:0} s, " +
                  $"{report.errorCount} errors, peak {report.peakMemoryMB} MB");
        Application.logMessageReceived -= OnLog;
        Application.Quit(code);
    }

    static long MemoryMB() => GC.GetTotalMemory(false) / (1024 * 1024)
        + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);

    static string Arg(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }

    static float ArgFloat(string flag, float fallback) =>
        float.TryParse(Arg(flag), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
// [AI-GENERATED END]
