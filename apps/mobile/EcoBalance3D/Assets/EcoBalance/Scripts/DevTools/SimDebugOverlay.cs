// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Developer overlay (Editor and development builds only): shows the simulated day, live populations
/// and the Free Exploration investigation cases, with a banner when one unlocks.
/// Installs itself whenever a scene with an EcosystemSimulator loads, adding a PopulationHistoryRecorder
/// and CaseDetector at runtime if the scene doesn't have them, so no scene file is edited.
/// Cases come from Resources/Cases. Toggle with the backquote key (`) or the small button.
/// Not the player-facing UI: Dillon's popup and Field Journal replace it.
/// </summary>
public class SimDebugOverlay : MonoBehaviour
{
    EcosystemSimulator sim;
    PopulationHistoryRecorder recorder;
    CaseDetector detector;
    // Hidden by default so demos and recordings show the real UI; ` opens it.
    bool visible;
    string banner;
    float bannerUntil;
    GUIStyle box, label, title, bannerStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall();
    }

    static void OnSceneLoaded(Scene _, LoadSceneMode __) => TryInstall();

    static void TryInstall()
    {
        if (!Debug.isDebugBuild) return;
        if (FindAnyObjectByType<SimDebugOverlay>() != null) return;
        var sim = FindAnyObjectByType<EcosystemSimulator>();
        if (sim == null) return;
        var go = new GameObject("[SimDebugOverlay]");
        go.AddComponent<SimDebugOverlay>().Attach(sim);
        go.AddComponent<SpatialGridView>().sim = sim;
    }

    void Attach(EcosystemSimulator target)
    {
        sim = target;
        recorder = sim.GetComponent<PopulationHistoryRecorder>() ?? sim.gameObject.AddComponent<PopulationHistoryRecorder>();
        detector = sim.GetComponent<CaseDetector>();
        if (detector == null)
        {
            detector = sim.gameObject.AddComponent<CaseDetector>();
            detector.cases = Resources.LoadAll<InvestigationCase>("Cases").OrderBy(c => c.name).ToList();
        }
        detector.CaseUnlocked += c => { banner = $"New case discovered: {c.title}"; bannerUntil = Time.unscaledTime + 4f; };
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame) visible = !visible;
        if (sim == null) Destroy(gameObject); // its scene was unloaded
    }

    void Styles()
    {
        if (box != null) return;
        box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
        label = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, richText = true };
        title = new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = 15 };
        bannerStyle = new GUIStyle(GUI.skin.box) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }

    void OnGUI()
    {
        if (sim == null) return;
        Styles();

        if (banner != null && Time.unscaledTime < bannerUntil)
            GUI.Box(new Rect(Screen.width / 2f - 260, 20, 520, 48), banner, bannerStyle);

        float w = 320;
        if (!visible) return;
        if (GUI.Button(new Rect(Screen.width - w - 10, 10, 90, 24), "Hide debug")) visible = false;

        var lines = new List<(string text, GUIStyle style)>();
        lines.Add(($"Day {sim.TickCount}  ·  mode {sim.mode}", title));
        foreach (var kv in sim.populations)
            if (kv.Key != null) lines.Add(($"{kv.Key.name}: {kv.Value:0}", label));
        if (sim.resources != null)
            foreach (var r in sim.resources)
                if (r != null) lines.Add(($"{r.name}: {r.currentAmount:0} / {r.maxAmount:0}", label));
        lines.Add(($"History: {recorder.History.Count} days recorded", label));
        lines.Add(("Investigation cases", title));
        if (detector.cases.Count == 0) lines.Add(("(none in Resources/Cases)", label));
        foreach (var c in detector.cases)
        {
            bool open = detector.IsUnlocked(c);
            lines.Add((open ? $"<color=#7CFC9A>✔ {c.title}</color>" : $"🔒 {c.title}\n<i>{c.hint}</i>", label));
        }

        float h = 46;
        foreach (var l in lines) h += l.style.CalcHeight(new GUIContent(l.text), w - 20) + 2;
        var area = new Rect(Screen.width - w - 10, 40, w, h);
        GUI.Box(area, GUIContent.none, box);
        float y = area.y + 8;
        foreach (var l in lines)
        {
            float lh = l.style.CalcHeight(new GUIContent(l.text), w - 20);
            GUI.Label(new Rect(area.x + 10, y, w - 20, lh), l.text, l.style);
            y += lh + 2;
        }
        if (GUI.Button(new Rect(area.x + 10, area.yMax - 32, 140, 24), "Reset journal"))
            detector.Journal.ResetAll();
    }
}
// [AI-GENERATED END]
