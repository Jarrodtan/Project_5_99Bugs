// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections;
using System.Collections.Generic;
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
        // Water check, once a second: who is in the river or a stream, are they moving (crossing) or standing,
        // and are they on a ford (allowed) or in the blocked part of the main river (should never happen).
        int fordBit = HimalayaRiver.FordArea > 0 ? 1 << HimalayaRiver.FordArea : 0;
        var lastPos = new Dictionary<AnimalBrain, Vector3>();
        var stillInWater = new Dictionary<AnimalBrain, int>();
        int samples = 0, inWater = 0, crossing = 0, offFord = 0, longStill = 0, swimFleeHunt = 0, swimOther = 0;
        float nextSample = 0f;
        while (t < RunSeconds)
        {
            // Opened directly, the scene runs as Guided: its intro card and events pause the run. Nobody taps here.
            foreach (var c in speeds) if (c != null && !c.IsRunning) c.Play();
            if (t >= nextSample)
            {
                nextSample = t + 1f;
                foreach (var b in FindObjectsByType<AnimalBrain>())
                {
                    if (b == null || b.IsCaught) continue;
                    samples++;
                    Vector3 p = b.transform.position;
                    bool moving = lastPos.TryGetValue(b, out var lp) && Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(lp.x, 0, lp.z)) > 0.3f;
                    lastPos[b] = p;
                    if (!HimalayaRiver.InWater(p, AnimalMotionBridge.SwimWithin)) { stillInWater.Remove(b); continue; }
                    inWater++;
                    if (moving) { crossing++; stillInWater.Remove(b); }
                    else
                    {
                        stillInWater[b] = stillInWater.TryGetValue(b, out var s) ? s + 1 : 1;
                        if (stillInWater[b] == 4) { longStill++; Debug.LogWarning($"[Water] {b.name} standing in the water 4s+ at {p:F0}, action {b.CurrentAction}", b); }
                    }
                    bool mainRiver = HimalayaRiver.DistanceTo(p) < 4f;
                    if (mainRiver && fordBit != 0 && UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 1f, UnityEngine.AI.NavMesh.AllAreas) && (hit.mask & fordBit) == 0)
                    { offFord++; Debug.LogWarning($"[Water] {b.name} in the main river away from a ford at {p:F0}, action {b.CurrentAction}", b); }
                    if (b.CurrentAction == AnimalBrain.AnimalAction.Fleeing || b.CurrentAction == AnimalBrain.AnimalAction.Hunting) swimFleeHunt++; else swimOther++;
                }
            }
            t += Time.unscaledDeltaTime; frames++; yield return null;
        }
        Debug.Log($"[Probe] water: {inWater} of {samples} animal-seconds in water; moving across {crossing}; standing in water 4s+: {longStill}; " +
                  $"in main river off a ford: {offFord}; while fleeing/hunting {swimFleeHunt}, other {swimOther}");

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
