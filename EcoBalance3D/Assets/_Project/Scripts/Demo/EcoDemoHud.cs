// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary M1 demo HUD (IMGUI, so it needs no Canvas setup and works on Android):
/// live counts, Play/Pause/Reset, speed buttons and a population-over-time graph.
/// To be replaced by the team's UI (T04 displays, F02/F06 controls).
/// </summary>
public class EcoDemoHud : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public ScriptableObject key;
        public string label;
        public Color color = Color.white;
    }

    public EcoDemoController controller;
    public List<Entry> entries = new List<Entry>();
    public string title = "EcoBalance - M1 Demo";

    static readonly int[] Speeds = { 1, 2, 4, 8 };
    const float RefHeight = 720f;

    GUIStyle label, header, button;

    void OnGUI()
    {
        if (controller == null) return;
        EnsureStyles();

        float scale = Screen.height / RefHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        DrawPanel();
        DrawGraph(new Rect(10f, RefHeight - 190f, width - 20f, 180f));
    }

    void DrawPanel()
    {
        float h = 110f + entries.Count * 28f;
        GUI.Box(new Rect(10f, 10f, 300f, h), GUIContent.none);
        GUI.Label(new Rect(22f, 16f, 280f, 28f), title, header);

        float y = 48f;
        GUI.Label(new Rect(22f, y, 280f, 24f), $"Tick {controller.TickCount}   Speed x{controller.Speed}" + (controller.Paused ? "   (paused)" : ""), label);
        y += 28f;

        foreach (var e in entries)
        {
            var old = GUI.color;
            GUI.color = e.color;
            GUI.DrawTexture(new Rect(22f, y + 5f, 14f, 14f), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(44f, y, 260f, 24f), $"{e.label}: {Mathf.RoundToInt(Latest(e.key))}", label);
            y += 28f;
        }

        y += 4f;
        if (GUI.Button(new Rect(22f, y, 86f, 34f), controller.Paused ? "Play" : "Pause", button))
            controller.SetPaused(!controller.Paused);
        if (GUI.Button(new Rect(114f, y, 86f, 34f), "Reset", button))
            controller.ResetSimulation();
        if (GUI.Button(new Rect(206f, y, 92f, 34f), "Step", button))
            controller.Step();

        // Speed buttons sit to the right of the panel.
        for (int i = 0; i < Speeds.Length; i++)
        {
            bool selected = controller.Speed == Speeds[i];
            var old = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUI.Button(new Rect(320f + i * 62f, 10f, 56f, 34f), $"x{Speeds[i]}", button))
                controller.SetSpeed(Speeds[i]);
            GUI.backgroundColor = old;
        }
    }

    void DrawGraph(Rect r)
    {
        GUI.Box(r, GUIContent.none);
        GUI.Label(new Rect(r.x + 10f, r.y + 4f, 400f, 22f), "Populations over time", label);

        float max = 1f;
        foreach (var e in entries)
            if (e.key != null && controller.History.TryGetValue(e.key, out var s))
                foreach (float v in s) max = Mathf.Max(max, v);

        var plot = new Rect(r.x + 10f, r.y + 30f, r.width - 20f, r.height - 40f);
        var oldColor = GUI.color;
        foreach (var e in entries)
        {
            if (e.key == null || !controller.History.TryGetValue(e.key, out var s) || s.Count < 2) continue;
            GUI.color = e.color;
            float dx = plot.width / (controller.historyLength - 1);
            for (int i = 1; i < s.Count; i++)
            {
                // Short dotted segment between consecutive samples.
                for (int k = 0; k < 4; k++)
                {
                    float t = k / 4f;
                    float v = Mathf.Lerp(s[i - 1], s[i], t);
                    float x = plot.x + (i - 1 + t) * dx;
                    float y = plot.yMax - v / max * plot.height;
                    GUI.DrawTexture(new Rect(x, y - 1.5f, 3f, 3f), Texture2D.whiteTexture);
                }
            }
        }
        GUI.color = oldColor;
    }

    float Latest(ScriptableObject key)
    {
        return key != null && controller.History.TryGetValue(key, out var s) && s.Count > 0 ? s[s.Count - 1] : 0f;
    }

    void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        label.normal.textColor = Color.white;
        header = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };
        button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
    }
}
// [AI-GENERATED END]
