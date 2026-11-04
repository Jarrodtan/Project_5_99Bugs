// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Headless freeze check, only when Unity is started with -freezeProbe (see Editor/FreezeProbeRunner):
/// starts the Himalaya world like Free mode with 100 of each species, lets it run, then logs how many
/// animals moved and quits. AgentUnstick writes a [Frozen] line for each animal that stood still.
/// Does nothing in a normal run.
/// </summary>
public class FreezeProbe : MonoBehaviour
{
    public const string Arg = "-freezeProbe";
    const int Count = 50;
    const float RunSeconds = 45f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!System.Environment.GetCommandLineArgs().Contains(Arg) || FindAnyObjectByType<FreezeProbe>() != null) return;
        var go = new GameObject("FreezeProbe");
        DontDestroyOnLoad(go);
        go.AddComponent<FreezeProbe>();
    }

    IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != "HimalayaSimulation")
        {
            SceneManager.LoadScene("HimalayaSimulation");
            yield return null;
        }
        EcosystemSimulator sim = null;
        while ((sim = FindAnyObjectByType<EcosystemSimulator>()) == null) yield return null;
        yield return new WaitForSecondsRealtime(8f);   // world, river and NavMesh build

        EcosystemSimulator.StartingPopulationOverrides.Clear();
        foreach (var sp in sim.species) if (sp != null) EcosystemSimulator.StartingPopulationOverrides[sp.name] = Count;
        sim.ResetSimulation();
        Time.timeScale = 1f;
        foreach (var c in FindObjectsByType<SimulationSpeedController>()) c.Play();   // what Free's Start button does
        var args = System.Environment.GetCommandLineArgs();
        int bi = System.Array.IndexOf(args, "-pathBudget");
        if (bi >= 0 && bi + 1 < args.Length && int.TryParse(args[bi + 1], out int budget)) UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = budget;
        Debug.Log($"[Probe] started: {Count} of each of {sim.species.Count} species, path budget {UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame}");

        yield return new WaitForSecondsRealtime(2f);
        var brains = FindObjectsByType<AnimalBrain>();
        var start = brains.ToDictionary(b => b, b => b.transform.position);
        Debug.Log($"[Probe] budget now {UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame}; {brains.Length} animal models in the world, fps {1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0}");

        float t = 0f; int frames = 0;
        var speeds = FindObjectsByType<SimulationSpeedController>();
        while (t < RunSeconds)
        {
            // Opened directly, the scene runs as Guided: its intro card and events pause the run. Nobody taps here.
            foreach (var c in speeds) if (c != null && !c.IsRunning) c.Play();
            t += Time.unscaledDeltaTime; frames++; yield return null;
        }

        int moved = 0, still = 0;
        foreach (var kv in start)
        {
            if (kv.Key == null) continue;
            Vector3 d = kv.Key.transform.position - kv.Value; d.y = 0f;
            if (d.magnitude > 2f) moved++; else still++;
        }
        Debug.Log($"[Probe] done after {RunSeconds:0}s, {frames / RunSeconds:0} fps: moved {moved}, still {still}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(still == 0 ? 0 : 2);
#else
        Application.Quit(still == 0 ? 0 : 2);
#endif
    }
}
// [AI-GENERATED END]
