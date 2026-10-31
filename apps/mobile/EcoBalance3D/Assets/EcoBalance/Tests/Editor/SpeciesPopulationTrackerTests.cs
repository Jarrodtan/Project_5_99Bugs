using System.Collections.Generic;
using NUnit.Framework;

// EditMode tests for Display 1 counter logic: live counts, change direction, and extinction.
public class SpeciesPopulationTrackerTests
{
    private FakeProvider provider;
    private SpeciesPopulationTracker tracker;

    [SetUp]
    public void SetUp()
    {
        provider = new FakeProvider();
        provider.Add("blue_sheep", "Blue Sheep", 42f);
        provider.Add("snow_leopard", "Snow Leopard", 6f);
        tracker = new SpeciesPopulationTracker();
    }

    [Test]
    public void FirstRefresh_ReportsCountsWithNoTrendOrFlash()
    {
        Assert.IsTrue(tracker.Refresh(provider), "first refresh builds the row layout");

        Assert.AreEqual(2, tracker.States.Count);
        var sheep = tracker.States[0];
        Assert.AreEqual("blue_sheep", sheep.SpeciesId);
        Assert.AreEqual("Blue Sheep", sheep.DisplayName);
        Assert.AreEqual(42, sheep.Count);
        Assert.AreEqual(SpeciesCounterStatus.Alive, sheep.Status);
        Assert.AreEqual(PopulationTrend.None, sheep.Trend);
        Assert.IsFalse(sheep.Changed);
    }

    [Test]
    public void CountIncrease_TrendsUpAndFlashesOnce()
    {
        tracker.Refresh(provider);
        provider.Counts["blue_sheep"] = 45f;

        Assert.IsFalse(tracker.Refresh(provider), "same ids must not rebuild rows");
        Assert.AreEqual(PopulationTrend.Up, tracker.States[0].Trend);
        Assert.IsTrue(tracker.States[0].Changed);

        tracker.Refresh(provider);
        Assert.AreEqual(PopulationTrend.Up, tracker.States[0].Trend, "trend persists until the next change");
        Assert.IsFalse(tracker.States[0].Changed, "flash fires only on the refresh that changed");
    }

    [Test]
    public void CountDecrease_TrendsDown()
    {
        tracker.Refresh(provider);
        provider.Counts["snow_leopard"] = 4f;
        tracker.Refresh(provider);

        Assert.AreEqual(PopulationTrend.Down, tracker.States[1].Trend);
        Assert.IsTrue(tracker.States[1].Changed);
        Assert.AreEqual(PopulationTrend.None, tracker.States[0].Trend, "unchanged species stay neutral");
    }

    [Test]
    public void SubUnitFluctuation_DoesNotCountAsChange()
    {
        provider.Counts["blue_sheep"] = 42.2f;
        tracker.Refresh(provider);
        provider.Counts["blue_sheep"] = 41.8f;
        tracker.Refresh(provider);

        Assert.AreEqual(42, tracker.States[0].Count);
        Assert.IsFalse(tracker.States[0].Changed);
    }

    [Test]
    public void ZeroCount_IsExtinctAndTrendsDown()
    {
        tracker.Refresh(provider);
        provider.Counts["snow_leopard"] = 0f;
        tracker.Refresh(provider);

        var leopard = tracker.States[1];
        Assert.AreEqual(0, leopard.Count);
        Assert.AreEqual(SpeciesCounterStatus.Extinct, leopard.Status);
        Assert.AreEqual(PopulationTrend.Down, leopard.Trend);
        Assert.IsTrue(leopard.Changed);
    }

    [TestCase(0.4f)]
    [TestCase(-3f)]
    public void BelowHalfOrNegative_RoundsToExtinct(float raw)
    {
        provider.Counts["blue_sheep"] = raw;
        tracker.Refresh(provider);

        Assert.AreEqual(0, tracker.States[0].Count);
        Assert.AreEqual(SpeciesCounterStatus.Extinct, tracker.States[0].Status);
    }

    [Test]
    public void Recovery_FromExtinct_IsAliveAndTrendsUp()
    {
        provider.Counts["snow_leopard"] = 0f;
        tracker.Refresh(provider);
        provider.Counts["snow_leopard"] = 2f;
        tracker.Refresh(provider);

        Assert.AreEqual(SpeciesCounterStatus.Alive, tracker.States[1].Status);
        Assert.AreEqual(PopulationTrend.Up, tracker.States[1].Trend);
    }

    [Test]
    public void MissingValue_IsUnavailableNotExtinct_AndDoesNotFlashWhenItReturns()
    {
        tracker.Refresh(provider);
        provider.Counts.Remove("blue_sheep");
        tracker.Refresh(provider);

        Assert.AreEqual(SpeciesCounterStatus.Unavailable, tracker.States[0].Status);
        Assert.AreEqual(PopulationTrend.None, tracker.States[0].Trend);

        provider.Counts["blue_sheep"] = 10f;
        tracker.Refresh(provider);
        Assert.AreEqual(SpeciesCounterStatus.Alive, tracker.States[0].Status);
        Assert.IsFalse(tracker.States[0].Changed);
    }

    [Test]
    public void SpeciesListChange_RebuildsAndKeepsHistoryForRemainingIds()
    {
        tracker.Refresh(provider);
        provider.Add("marmot", "Marmot", 15f);
        provider.Counts["blue_sheep"] = 50f;

        Assert.IsTrue(tracker.Refresh(provider));
        Assert.AreEqual(3, tracker.States.Count);
        Assert.AreEqual(PopulationTrend.Up, tracker.States[0].Trend);
        Assert.AreEqual(PopulationTrend.None, tracker.States[2].Trend);
    }

    [Test]
    public void DuplicateIds_AreShownOnceAndDoNotRebuildEveryRefresh()
    {
        provider.Ids.Add("blue_sheep");
        tracker.Refresh(provider);

        Assert.AreEqual(2, tracker.States.Count);
        Assert.IsFalse(tracker.Refresh(provider));
    }

    [Test]
    public void EmptyProvider_ThenDataArrives_ShowsLiveValuesThatKeepUpdating()
    {
        // Mirrors the live adapter today: no species until T02 is wired.
        var empty = new FakeProvider();
        tracker.Refresh(empty);
        Assert.AreEqual(0, tracker.States.Count);

        empty.Add("blue_sheep", "Blue Sheep", 42f);
        Assert.IsTrue(tracker.Refresh(empty), "rows must be built once data appears");
        Assert.AreEqual(42, tracker.States[0].Count);
        Assert.IsFalse(tracker.States[0].Changed, "first value is not a change");

        empty.Counts["blue_sheep"] = 38f;
        tracker.Refresh(empty);
        Assert.AreEqual(38, tracker.States[0].Count);
        Assert.AreEqual(PopulationTrend.Down, tracker.States[0].Trend);
    }

    [Test]
    public void NullProvider_ClearsRows()
    {
        tracker.Refresh(provider);
        Assert.IsTrue(tracker.Refresh(null));
        Assert.AreEqual(0, tracker.States.Count);
    }

    private class FakeProvider : ISimulationDataProvider
    {
        public readonly List<string> Ids = new();
        public readonly Dictionary<string, float> Counts = new();
        private readonly Dictionary<string, string> names = new();

        public void Add(string id, string name, float count)
        {
            Ids.Add(id);
            names[id] = name;
            Counts[id] = count;
        }

        public IReadOnlyList<string> GetSpeciesIds() => Ids;

        public IReadOnlyList<string> GetResourceIds() => new List<string>();

        public bool TryGetPopulationCount(string speciesId, out float count) =>
            Counts.TryGetValue(speciesId, out count);

        public bool TryGetResourceLevel(string resourceId, out float level)
        {
            level = 0f;
            return false;
        }

        public string GetDisplayName(string id) =>
            names.TryGetValue(id, out var name) ? name : id;
    }
}
