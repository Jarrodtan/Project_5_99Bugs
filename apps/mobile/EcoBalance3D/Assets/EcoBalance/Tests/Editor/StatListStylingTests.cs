using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// EditMode tests for the shared labeled-stat list style used by the Population and Inspect tabs.
public class StatListStylingTests
{
    private StatListStyle style;

    [SetUp]
    public void SetUp()
    {
        style = ScriptableObject.CreateInstance<StatListStyle>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(style);
    }

    [Test]
    public void PopulationCountAndInspectedStat_FormatTheSameWay()
    {
        var population = StatListStyling.FormatLabelValue(style, "Blue Sheep", 42);
        var stat = new StatEntry("Health", "80");
        var inspected = StatListStyling.FormatLabelValue(style, stat.Label, stat.Value);

        Assert.AreEqual("Blue Sheep: 42", population);
        Assert.AreEqual("Health: 80", inspected);
    }

    [Test]
    public void CustomFormat_AppliesToEveryList()
    {
        style.labelValueFormat = "{0} — {1}";

        Assert.AreEqual("Grass — 55", StatListStyling.FormatLabelValue(style, "Grass", 55));
    }

    [Test]
    public void PanelHeight_MatchesPaddedTitlePlusSpacedRows()
    {
        style.panelPadding = 10;
        style.titleFontSize = 20f;
        style.rowSpacing = 4f;
        style.rowHeight = 30f;

        // 10 pad + (20 + 4) title + 4 gap + 3 * 30 rows + 2 * 4 gaps + 10 pad
        Assert.AreEqual(146f, StatListStyling.PanelHeight(style, 3), 0.001f);
        Assert.AreEqual(10f + 24f + 10f, StatListStyling.PanelHeight(style, 0), 0.001f);
    }

    [Test]
    public void ApplyRow_GivesEveryRowTheSameHeightAndBackground()
    {
        var a = new GameObject("RowA", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        var b = new GameObject("RowB", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        try
        {
            StatListStyling.ApplyRow(style, a.GetComponent<LayoutElement>(), a.GetComponent<Image>(), null);
            StatListStyling.ApplyRow(style, b.GetComponent<LayoutElement>(), b.GetComponent<Image>(), null);

            Assert.AreEqual(style.rowHeight, a.GetComponent<LayoutElement>().preferredHeight);
            Assert.AreEqual(a.GetComponent<LayoutElement>().preferredHeight, b.GetComponent<LayoutElement>().preferredHeight);
            Assert.AreEqual(style.rowBackgroundColor, b.GetComponent<Image>().color);
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
