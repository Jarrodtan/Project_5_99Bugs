using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live line graph of a <see cref="PopulationHistory"/>: one smooth line per species (or per Food &amp; Water value)
/// with a soft colour fill under it, dashed gridlines with whole-number labels, the day range along the bottom,
/// a bubble at the end of each line with today's value, and a row of pill-shaped legend chips that show / hide
/// each line when tapped. Redraws whenever the history gets a new day.
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
    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    /// <summary>Added to the axis numbers and the end-of-line bubbles, e.g. "%" for the Food &amp; Water view.</summary>
    public string valueSuffix = "";
    /// <summary>The axis reaches at least this (100 for percentages, so the scale doesn't jump while values are low).</summary>
    public float minAxisMax;
    /// <summary>Rounded sprite for the legend pills and value bubbles (plain rectangles without one).</summary>
    public Sprite pillSprite;
    // [AI-GENERATED END]

    /// <summary>Raised after each redraw (new day, line shown / hidden), e.g. to refresh a summary next to the graph.</summary>
    public event Action Redrawn;

    /// <summary>A numbered pin above the plot at one history sample, e.g. a note in the Observations list.</summary>
    public struct Marker
    {
        public int Index;     // history sample
        public string Label;  // short, e.g. "1"
        public Color Color;
    }

    /// <summary>Asked for the pins on every redraw, so they always match the history being drawn.</summary>
    public Func<IReadOnlyList<Marker>> MarkerSource;

    public PopulationHistory History { get; private set; }
    public IReadOnlyList<PopulationSeries> Series => series;

    readonly List<PopulationSeries> series = new();
    readonly List<(PopulationSeries s, Image bg, Image dot, TextMeshProUGUI label)> chips = new();
    readonly List<(Image bg, TextMeshProUGUI text)> bubbles = new();
    IReadOnlyDictionary<int, int> dayAtTick;
    PopulationGraphLines lines;
    RectTransform plot, legend;
    readonly List<TextMeshProUGUI> yLabels = new();
    readonly List<TextMeshProUGUI> markerLabels = new();
    TextMeshProUGUI[] xLabels;
    TextMeshProUGUI emptyText;
    bool built, dirty;
    float legendHeight = OneRowLegend;

    // Right margin leaves room for the value bubbles at the end of each line.
    const float LeftMargin = 90f, BottomMargin = 56f, TopMargin = 52f, RightMargin = 112f;
    const float OneRowLegend = 70f, ChipHeight = 54f, ChipGap = 12f;
    /// <summary>Longest line drawn: a long run is thinned to this many points (always keeping today).</summary>
    public const int MaxPoints = 240;

    float PlotBottom => BottomMargin + legendHeight;

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

        emptyText = Label("Empty", plot, labelSize * 1.1f, TextAlignmentOptions.Center);
        var ert = emptyText.rectTransform;
        ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one; ert.offsetMin = ert.offsetMax = Vector2.zero;
        ApplyMargins();
    }

    void ApplyMargins()
    {
        plot.offsetMin = new Vector2(LeftMargin, PlotBottom);
        plot.offsetMax = new Vector2(-RightMargin, -TopMargin);
        legend.offsetMin = new Vector2(LeftMargin * 0.5f, 6f);
        legend.offsetMax = new Vector2(-RightMargin * 0.5f, legendHeight);
    }

    // Pill chips: filled with the line's colour while it is shown, grey while hidden. Four or fewer sit in one
    // row; more wrap into two rows (the Food & Water view has six).
    void BuildLegend()
    {
        foreach (Transform c in legend) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
        chips.Clear();
        foreach (var s in series)
        {
            var chip = NewRect(s.Label, legend);
            var bg = chip.gameObject.AddComponent<Image>();
            if (pillSprite != null) { bg.sprite = pillSprite; bg.type = Image.Type.Sliced; }

            var dot = NewRect("Dot", chip).gameObject.AddComponent<Image>();
            if (pillSprite != null) { dot.sprite = pillSprite; dot.type = Image.Type.Sliced; }
            var drt = dot.rectTransform;
            drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0f, 0.5f);
            drt.anchoredPosition = new Vector2(16f, 0f);
            drt.sizeDelta = new Vector2(20f, 20f);

            var text = Label("Text", chip, labelSize * 0.82f, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMax = labelSize * 0.82f;
            text.fontSizeMin = 20f;   // still easy to read for young children
            text.text = s.Label;
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(42f, 2f); trt.offsetMax = new Vector2(-14f, -2f);

            var key = s.Key;
            chip.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                foreach (var x in series) if (x.Key == key) SetVisible(key, !x.Visible);
            });
            chips.Add((s, bg, dot, text));
        }
        LayOutLegend();
        RefreshChips();
    }

    void LayOutLegend()
    {
        int n = chips.Count;
        int columns = n <= 4 ? Mathf.Max(1, n) : Mathf.CeilToInt(n / 2f);
        int rows = n == 0 ? 1 : Mathf.CeilToInt(n / (float)columns);
        float wanted = rows == 1 ? OneRowLegend : rows * ChipHeight + (rows - 1) * ChipGap + 16f;
        if (!Mathf.Approximately(wanted, legendHeight)) { legendHeight = wanted; ApplyMargins(); }

        float width = ((RectTransform)transform).rect.width - LeftMargin * 0.5f - RightMargin * 0.5f;
        float cell = n == 0 ? 0f : Mathf.Min(300f, (width - (columns - 1) * ChipGap) / columns);
        for (int i = 0; i < n; i++)
        {
            int r = i / columns, c = i % columns;
            int inRow = Mathf.Min(columns, n - r * columns);
            float left = (width - (inRow * cell + (inRow - 1) * ChipGap)) * 0.5f;   // each row centred
            var rt = (RectTransform)chips[i].bg.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(cell, ChipHeight);
            rt.anchoredPosition = new Vector2(left + c * (cell + ChipGap), -r * (ChipHeight + ChipGap));
        }
    }

    void RefreshChips()
    {
        foreach (var (s, bg, dot, label) in chips)
        {
            bg.color = s.Visible ? s.Color : new Color(0.9f, 0.91f, 0.92f);
            dot.color = s.Visible ? Color.white : s.Color;
            label.color = s.Visible ? Color.white : new Color(textColor.r, textColor.g, textColor.b, 0.6f);
            label.fontStyle = s.Visible ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    // ---------- Drawing ----------

    void Redraw()
    {
        Build();
        LayOutLegend();
        int n = History?.Count ?? 0;
        emptyText.gameObject.SetActive(n == 0);
        emptyText.text = History == null ? emptyMessage : "Waiting for the first day...";

        float max = minAxisMax;
        for (int i = 0; i < n; i++)
            foreach (var s in series)
                if (s.Visible && History[i].Has(s.Key)) max = Mathf.Max(max, History[i].Get(s.Key));
        var (step, divisions) = PopulationGraphMath.Scale(max);
        float yMax = step * divisions;

        var drawn = new List<PopulationGraphLines.Line>();
        var ends = new List<(PopulationSeries s, float y, float value)>();
        if (n > 0)
        {
            var indices = PopulationGraphMath.Thin(n, MaxPoints);
            foreach (var s in series)
            {
                if (!s.Visible) continue;
                var pts = new List<Vector2>(indices.Count);
                float endValue = 0f;
                foreach (int i in indices)
                    if (History[i].Has(s.Key))   // a value recorded only from some day on starts there
                    {
                        endValue = History[i].Get(s.Key);
                        pts.Add(new Vector2(n == 1 ? 0f : i / (float)(n - 1), endValue / yMax));
                    }
                if (pts.Count == 0) continue;
                drawn.Add(new PopulationGraphLines.Line { color = s.Color, points = pts });
                ends.Add((s, pts[pts.Count - 1].y, endValue));   // the bubble shows where its line really ends
            }
        }
        var markers = n > 0 && MarkerSource != null ? MarkerSource() : null;
        var marks = new List<PopulationGraphLines.Mark>();
        if (markers != null)
            foreach (var m in markers)
                if (m.Index >= 0 && m.Index < n)
                    marks.Add(new PopulationGraphLines.Mark { x = n == 1 ? 0f : m.Index / (float)(n - 1), color = m.Color });
        lines.Set(drawn, divisions, marks);

        var rect = ((RectTransform)transform).rect;
        float plotHeight = rect.height - PlotBottom - TopMargin;
        float plotWidth = rect.width - LeftMargin - RightMargin;

        // Y labels on the gridlines.
        while (yLabels.Count < divisions + 1)
        {
            var t = Label($"Y{yLabels.Count}", transform, labelSize * 0.9f, TextAlignmentOptions.Right);
            t.rectTransform.sizeDelta = new Vector2(LeftMargin - 14f, 40f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            yLabels.Add(t);
        }
        for (int i = 0; i < yLabels.Count; i++)
        {
            var t = yLabels[i];
            bool on = n > 0 && i <= divisions;   // no numbers on an empty graph
            t.gameObject.SetActive(on);
            if (!on) continue;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(LeftMargin - 14f, PlotBottom + plotHeight * i / divisions);
            t.color = new Color(textColor.r, textColor.g, textColor.b, 0.75f);
            t.text = (step * i).ToString("0") + valueSuffix;
        }

        PlaceBubbles(ends, plotHeight);

        // Marker numbers just above the plot, over their line.
        int shown = 0;
        if (markers != null)
            foreach (var m in markers)
            {
                if (m.Index < 0 || m.Index >= n) continue;
                if (shown == markerLabels.Count)
                {
                    var t = Label($"Marker{shown}", transform, labelSize * 0.95f, TextAlignmentOptions.Center);
                    t.fontStyle = FontStyles.Bold;
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    t.rectTransform.sizeDelta = new Vector2(64f, TopMargin - 6f);
                    markerLabels.Add(t);
                }
                var label = markerLabels[shown++];
                label.gameObject.SetActive(true);
                label.text = m.Label;
                label.color = m.Color;
                var rt = label.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(LeftMargin + plotWidth * (n == 1 ? 0f : m.Index / (float)(n - 1)), PlotBottom + plotHeight + 2f);
            }
        for (int i = shown; i < markerLabels.Count; i++) markerLabels[i].gameObject.SetActive(false);

        // Day range along the bottom.
        for (int i = 0; i < 3; i++)
        {
            int idx = n == 0 ? -1 : i == 0 ? 0 : i == 2 ? n - 1 : (n - 1) / 2;
            bool show = idx >= 0 && (i != 1 || n >= 3);
            xLabels[i].gameObject.SetActive(show);
            if (!show) continue;
            var rt = xLabels[i].rectTransform;
            float x = LeftMargin + plotWidth * (i * 0.5f);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, PlotBottom - 6f);
            xLabels[i].text = TimeLabel(History[idx].Tick);
        }

        Redrawn?.Invoke();
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): today's value in a coloured bubble at the end of each line,
    // nudged up / down so bubbles never sit on top of each other.
    void PlaceBubbles(List<(PopulationSeries s, float y, float value)> ends, float plotHeight)
    {
        const float H = 40f, Gap = 4f;
        var order = new List<int>();
        for (int i = 0; i < ends.Count; i++) order.Add(i);
        order.Sort((a, b) => ends[a].y.CompareTo(ends[b].y));
        var ys = new float[ends.Count];
        float floor = PlotBottom + H * 0.5f - 6f, ceiling = PlotBottom + plotHeight + 6f;
        float last = float.MinValue;
        foreach (int i in order)
        {
            float y = Mathf.Max(PlotBottom + Mathf.Clamp01(ends[i].y) * plotHeight, last + H + Gap);
            ys[i] = Mathf.Max(floor, y);
            last = ys[i];
        }
        // Pushed past the top: slide the whole stack back down.
        float over = last - ceiling;
        if (over > 0f) for (int i = 0; i < ys.Length; i++) ys[i] = Mathf.Max(floor, ys[i] - over);

        float x = ((RectTransform)transform).rect.width - RightMargin + 10f;
        for (int i = 0; i < ends.Count; i++)
        {
            if (i == bubbles.Count)
            {
                var bg = NewRect($"Bubble{i}", transform).gameObject.AddComponent<Image>();
                bg.raycastTarget = false;
                if (pillSprite != null) { bg.sprite = pillSprite; bg.type = Image.Type.Sliced; }
                var t = Label("Value", bg.transform, labelSize * 0.8f, TextAlignmentOptions.Center);
                t.color = Color.white;
                t.fontStyle = FontStyles.Bold;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true; t.fontSizeMax = labelSize * 0.8f; t.fontSizeMin = 18f;
                var trt = t.rectTransform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(6f, 0f); trt.offsetMax = new Vector2(-6f, 0f);
                bubbles.Add((bg, t));
            }
            var (image, text) = bubbles[i];
            image.gameObject.SetActive(true);
            image.color = ends[i].s.Color;
            text.text = ends[i].value.ToString("0") + valueSuffix;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(RightMargin - 16f, H);
            rt.anchoredPosition = new Vector2(x, ys[i]);
        }
        for (int i = ends.Count; i < bubbles.Count; i++) bubbles[i].bg.gameObject.SetActive(false);
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

/// <summary>
/// Draws the gridlines and the lines of a <see cref="PopulationGraph"/> as one UI mesh: dashed gridlines, a soft
/// colour fill fading down under each line, smooth curves through the points and a ringed dot on today's value.
/// </summary>
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

    /// <summary>A vertical pin across the plot (x in 0..1 of the rect).</summary>
    public struct Mark
    {
        public float x;
        public Color color;
    }

    public Color gridColor = new(0f, 0f, 0f, 0.12f);
    public float thickness = 6f;
    public float gridThickness = 2f;
    public float dotRadius = 11f;
    /// <summary>Opacity of the fill right under a line (it fades to nothing at the bottom).</summary>
    public float fillAlpha = 0.16f;
    /// <summary>Lines with this many points or fewer are drawn as smooth curves.</summary>
    public int smoothUpTo = 120;

    readonly List<Line> lines = new();
    readonly List<Mark> marks = new();
    int divisions = 5;

    public void Set(List<Line> newLines, int gridDivisions, List<Mark> newMarks = null)
    {
        lines.Clear();
        if (newLines != null) lines.AddRange(newLines);
        marks.Clear();
        if (newMarks != null) marks.AddRange(newMarks);
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
            if (i == 0) { c.a = Mathf.Min(1f, c.a * 3f); Segment(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), gridThickness * 1.5f, c); }   // solid baseline
            else Dashed(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), gridThickness, c, 14f, 10f);
        }

        // Pins under the lines, faint so the lines stay easy to read.
        foreach (var m in marks)
        {
            float x = r.xMin + Mathf.Clamp01(m.x) * r.width;
            var c = m.color;
            c.a *= 0.45f;
            Dashed(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), gridThickness * 1.5f, c, 10f, 8f);
        }

        // Fills first, so every line draws over every fill.
        var curves = new List<List<Vector2>>(lines.Count);
        foreach (var line in lines)
        {
            var pts = new List<Vector2>();
            if (line.points != null)
                foreach (var p in line.points) pts.Add(new Vector2(r.xMin + p.x * r.width, r.yMin + Mathf.Clamp01(p.y) * r.height));
            if (pts.Count > 2 && pts.Count <= smoothUpTo) pts = PopulationGraphMath.Smooth(pts, 4, r.yMin, r.yMax);
            curves.Add(pts);
            if (pts.Count < 2) continue;
            var top = line.color; top.a = fillAlpha;
            var bottom = line.color; bottom.a = 0f;
            for (int i = 1; i < pts.Count; i++) Fill(vh, pts[i - 1], pts[i], r.yMin, r.height, top, bottom);
        }

        for (int l = 0; l < lines.Count; l++)
        {
            var pts = curves[l];
            if (pts.Count == 0) continue;
            var color = lines[l].color;
            for (int i = 1; i < pts.Count; i++)
            {
                Segment(vh, pts[i - 1], pts[i], thickness, color);
                if (i < pts.Count - 1) Disc(vh, pts[i], thickness * 0.5f, color, 8);   // round joints
            }
            // Today: a white ring round a coloured dot.
            var end = pts[pts.Count - 1];
            Disc(vh, end, dotRadius + 4f, Color.white, 20);
            Disc(vh, end, dotRadius, color, 20);
        }
    }

    // A column from the line down to the bottom of the plot, colour fading from top to bottom.
    static void Fill(VertexHelper vh, Vector2 a, Vector2 b, float baseY, float height, Color top, Color bottom)
    {
        Color At(float y) => Color.Lerp(bottom, top, height > 0f ? Mathf.Clamp01((y - baseY) / height) * 1.4f : 0f);
        int i = vh.currentVertCount;
        vh.AddVert(a, At(a.y), Vector2.zero);
        vh.AddVert(b, At(b.y), Vector2.zero);
        vh.AddVert(new Vector2(b.x, baseY), bottom, Vector2.zero);
        vh.AddVert(new Vector2(a.x, baseY), bottom, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    static void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color, float dash, float gap)
    {
        float length = Vector2.Distance(a, b);
        if (length < 1e-3f) return;
        var dir = (b - a) / length;
        for (float d = 0f; d < length; d += dash + gap)
            Segment(vh, a + dir * d, a + dir * Mathf.Min(length, d + dash), width, color);
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
