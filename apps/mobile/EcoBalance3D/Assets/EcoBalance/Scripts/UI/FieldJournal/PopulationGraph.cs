using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live line graph of a <see cref="PopulationHistory"/>: one line per species (and, when switched on,
/// per resource), gridlines with whole-number labels, the day range along the bottom and a row of legend
/// chips that show / hide each line when tapped. Redraws whenever the history gets a new day.
/// Builds its own children, so it can be dropped on any RectTransform (Field Journal, HUD...):
/// <code>PopulationGraph.Create(parent, font).Bind(recorder.History, series, days.DayAtTick);</code>
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PopulationGraph : MonoBehaviour
{
    public TMP_FontAsset font;
    public float labelSize = 26f;
    public Color textColor = new(0.2f, 0.24f, 0.28f);
    public Color gridColor = new(0.12f, 0.16f, 0.2f, 0.12f);
    public string emptyMessage = "Nothing observed yet.\nPlay Guided or Free Exploration, then come back to see how the animals changed.";

    /// <summary>Raised after each redraw (new day, line shown / hidden), e.g. to refresh a summary next to the graph.</summary>
    public event Action Redrawn;

    public PopulationHistory History { get; private set; }
    public IReadOnlyList<PopulationSeries> Series => series;

    readonly List<PopulationSeries> series = new();
    readonly List<(PopulationSeries s, Image swatch, TextMeshProUGUI label)> chips = new();
    IReadOnlyDictionary<int, int> dayAtTick;
    PopulationGraphLines lines;
    RectTransform plot, legend;
    readonly List<TextMeshProUGUI> yLabels = new();
    TextMeshProUGUI[] xLabels;
    TextMeshProUGUI emptyText;
    bool built, dirty;

    const float LeftMargin = 90f, BottomMargin = 56f, LegendHeight = 70f, TopMargin = 24f, RightMargin = 24f;

    public static PopulationGraph Create(Transform parent, TMP_FontAsset font = null, string name = "PopulationGraph")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var graph = go.AddComponent<PopulationGraph>();
        graph.font = font;
        return graph;
    }

    /// <summary>Shows <paramref name="history"/> (null shows the empty message). <paramref name="dayAtTick"/> labels time in HUD days.</summary>
    public PopulationGraph Bind(PopulationHistory history, IEnumerable<PopulationSeries> lineSet, IReadOnlyDictionary<int, int> dayAtTick = null)
    {
        Build();
        Unsubscribe();
        History = history;
        this.dayAtTick = dayAtTick;
        if (History != null)
        {
            History.SampleAdded += OnSample;
            History.Cleared += OnCleared;
        }
        series.Clear();
        if (lineSet != null) series.AddRange(lineSet);
        BuildLegend();
        dirty = true;
        return this;
    }

    public void SetVisible(string key, bool visible)
    {
        foreach (var s in series) if (s.Key == key) s.Visible = visible;
        RefreshChips();
        dirty = true;
    }

    void OnSample(PopulationHistory.Sample _) => dirty = true;
    void OnCleared() => dirty = true;

    void Unsubscribe()
    {
        if (History == null) return;
        History.SampleAdded -= OnSample;
        History.Cleared -= OnCleared;
    }

    void OnDestroy() => Unsubscribe();
    void OnEnable() => dirty = true;

    // At 8x the simulator can add several days a frame: redraw once, after them.
    void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        Redraw();
    }

    // ---------- Layout ----------

    void Build()
    {
        if (built) return;
        built = true;
        var root = (RectTransform)transform;

        plot = NewRect("Plot", root);
        plot.anchorMin = Vector2.zero; plot.anchorMax = Vector2.one;
        plot.offsetMin = new Vector2(LeftMargin, BottomMargin + LegendHeight);
        plot.offsetMax = new Vector2(-RightMargin, -TopMargin);
        lines = plot.gameObject.AddComponent<PopulationGraphLines>();
        lines.raycastTarget = false;
        lines.gridColor = gridColor;

        xLabels = new TextMeshProUGUI[3];
        var xAlign = new[] { TextAlignmentOptions.Left, TextAlignmentOptions.Center, TextAlignmentOptions.Right };
        for (int i = 0; i < 3; i++)
        {
            var t = Label($"X{i}", root, labelSize * 0.9f, xAlign[i]);
            var rt = t.rectTransform;
            float ax = i * 0.5f;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, 0f);
            rt.pivot = new Vector2(ax, 1f);
            rt.sizeDelta = new Vector2(320f, BottomMargin - 8f);
            xLabels[i] = t;
        }

        legend = NewRect("Legend", root);
        legend.anchorMin = new Vector2(0f, 0f); legend.anchorMax = new Vector2(1f, 0f); legend.pivot = new Vector2(0.5f, 0f);
        legend.offsetMin = new Vector2(LeftMargin, 0f); legend.offsetMax = new Vector2(-RightMargin, LegendHeight);
        var row = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 18f; row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;

        emptyText = Label("Empty", plot, labelSize * 1.1f, TextAlignmentOptions.Center);
        var ert = emptyText.rectTransform;
        ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one; ert.offsetMin = ert.offsetMax = Vector2.zero;
    }

    void BuildLegend()
    {
        foreach (Transform c in legend) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
        chips.Clear();
        foreach (var s in series)
        {
            var chip = NewRect(s.Label, legend);
            var bg = chip.gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);   // tappable, invisible
            var h = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.padding = new RectOffset(6, 10, 8, 8); h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            var sw = NewRect("Swatch", chip).gameObject.AddComponent<Image>();
            var le = sw.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 34f; le.preferredHeight = 12f;

            var text = Label("Text", chip, labelSize, TextAlignmentOptions.Left);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = s.Label;

            var key = s.Key;
            chip.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                foreach (var x in series) if (x.Key == key) SetVisible(key, !x.Visible);
            });
            chips.Add((s, sw, text));
        }
        RefreshChips();
    }

    void RefreshChips()
    {
        foreach (var (s, swatch, label) in chips)
        {
            var c = s.Color; c.a = s.Visible ? 1f : 0.25f;
            swatch.color = c;
            var t = textColor; t.a = s.Visible ? 1f : 0.4f;
            label.color = t;
            label.fontStyle = s.Visible ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    // ---------- Drawing ----------

    void Redraw()
    {
        Build();
        int n = History?.Count ?? 0;
        emptyText.gameObject.SetActive(n == 0);
        emptyText.text = History == null ? emptyMessage : "Waiting for the first day...";

        float max = 0f;
        for (int i = 0; i < n; i++)
            foreach (var s in series)
                if (s.Visible) max = Mathf.Max(max, History[i].Get(s.Key));
        var (step, divisions) = PopulationGraphMath.Scale(max);
        float yMax = step * divisions;

        var drawn = new List<PopulationGraphLines.Line>();
        if (n > 0)
            foreach (var s in series)
            {
                if (!s.Visible) continue;
                var pts = new List<Vector2>(n);
                for (int i = 0; i < n; i++)
                    pts.Add(new Vector2(n == 1 ? 0f : i / (float)(n - 1), History[i].Get(s.Key) / yMax));
                drawn.Add(new PopulationGraphLines.Line { color = s.Color, points = pts });
            }
        lines.Set(drawn, divisions);

        // Y labels on the gridlines.
        while (yLabels.Count < divisions + 1)
        {
            var t = Label($"Y{yLabels.Count}", transform, labelSize * 0.9f, TextAlignmentOptions.Right);
            t.rectTransform.sizeDelta = new Vector2(LeftMargin - 14f, 40f);
            yLabels.Add(t);
        }
        float plotHeight = ((RectTransform)transform).rect.height - BottomMargin - LegendHeight - TopMargin;
        for (int i = 0; i < yLabels.Count; i++)
        {
            var t = yLabels[i];
            bool on = n > 0 && i <= divisions;   // no numbers on an empty graph
            t.gameObject.SetActive(on);
            if (!on) continue;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(LeftMargin - 14f, BottomMargin + LegendHeight + plotHeight * i / divisions);
            t.text = (step * i).ToString("0");
        }

        // Day range along the bottom.
        for (int i = 0; i < 3; i++)
        {
            int idx = n == 0 ? -1 : i == 0 ? 0 : i == 2 ? n - 1 : (n - 1) / 2;
            bool show = idx >= 0 && (i != 1 || n >= 3);
            xLabels[i].gameObject.SetActive(show);
            if (!show) continue;
            var rt = xLabels[i].rectTransform;
            float x = LeftMargin + (((RectTransform)transform).rect.width - LeftMargin - RightMargin) * (i * 0.5f);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, BottomMargin + LegendHeight - 6f);
            xLabels[i].text = TimeLabel(History[idx].Tick);
        }

        Redrawn?.Invoke();
    }

    /// <summary>"Day 3" when the HUD day of that tick is known, otherwise "Step 120".</summary>
    public string TimeLabel(int tick) => dayAtTick != null && dayAtTick.TryGetValue(tick, out int d) ? $"Day {d}" : $"Step {tick}";

    void OnRectTransformDimensionsChange() => dirty = true;

    // ---------- Helpers ----------

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    TextMeshProUGUI Label(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = textColor;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }
}

/// <summary>Draws the gridlines and the polylines of a <see cref="PopulationGraph"/> as one UI mesh.</summary>
// uGUI 2 no longer adds a CanvasRenderer for custom Graphics; without one nothing is drawn.
[RequireComponent(typeof(CanvasRenderer))]
public class PopulationGraphLines : MaskableGraphic
{
    public struct Line
    {
        public Color color;
        /// <summary>Points in 0..1 of the rect (x = time, y = value / axis max).</summary>
        public List<Vector2> points;
    }

    public Color gridColor = new(0f, 0f, 0f, 0.12f);
    public float thickness = 5f;
    public float gridThickness = 2f;
    public float dotRadius = 9f;

    readonly List<Line> lines = new();
    int divisions = 5;

    public void Set(List<Line> newLines, int gridDivisions)
    {
        lines.Clear();
        if (newLines != null) lines.AddRange(newLines);
        divisions = Mathf.Max(1, gridDivisions);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        for (int i = 0; i <= divisions; i++)
        {
            float y = r.yMin + r.height * i / divisions;
            var c = gridColor;
            if (i == 0) c.a = Mathf.Min(1f, c.a * 3f);   // darker baseline
            Segment(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), i == 0 ? gridThickness * 1.5f : gridThickness, c);
        }

        foreach (var line in lines)
        {
            if (line.points == null || line.points.Count == 0) continue;
            Vector2 P(int i) => new(r.xMin + line.points[i].x * r.width, r.yMin + Mathf.Clamp01(line.points[i].y) * r.height);
            for (int i = 1; i < line.points.Count; i++)
            {
                Segment(vh, P(i - 1), P(i), thickness, line.color);
                if (i < line.points.Count - 1) Disc(vh, P(i), thickness * 0.5f, line.color, 6);   // fills the joint
            }
            Disc(vh, P(line.points.Count - 1), dotRadius, line.color, 16);   // today
        }
    }

    static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        var d = b - a;
        if (d.sqrMagnitude < 1e-6f) return;
        var n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
        int i = vh.currentVertCount;
        vh.AddVert(a + n, color, Vector2.zero);
        vh.AddVert(b + n, color, Vector2.zero);
        vh.AddVert(b - n, color, Vector2.zero);
        vh.AddVert(a - n, color, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    static void Disc(VertexHelper vh, Vector2 c, float radius, Color color, int sides)
    {
        int center = vh.currentVertCount;
        vh.AddVert(c, color, Vector2.zero);
        for (int k = 0; k <= sides; k++)
        {
            float a = k * Mathf.PI * 2f / sides;
            vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            if (k > 0) vh.AddTriangle(center, center + k, center + k + 1);
        }
    }
}
