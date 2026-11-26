using System.Collections.Generic;
using NUnit.Framework;

// EditMode tests for Display 2 resource logic: live levels, bar fill, and critical threshold.
public class ResourceLevelReaderTests
{
    private static readonly ResourceGaugeLimits Default = new(100f, 0.2f);

    private FakeProvider provider;
    private Dictionary<string, ResourceGaugeLimits> limits;
    private ResourceLevelReader reader;

    [SetUp]
    public void SetUp()
    {
        provider = new FakeProvider();
        provider.Add("grass", "Grass", 65f);
        provider.Add("water", "Water", 40f);
        limits = new Dictionary<string, ResourceGaugeLimits>();
        reader = new ResourceLevelReader();
    }

    private void Refresh() =>
        reader.Refresh(provider, id => limits.TryGetValue(id, out var l) ? l : Default);

    [Test]
    public void ReportsLevelsAndBarFillInProviderOrder()
    {
        Refresh();

        Assert.AreEqual(2, reader.States.Count);
        var grass = reader.States[0];
        Assert.AreEqual("grass", grass.ResourceId);
        Assert.AreEqual("Grass", grass.DisplayName);
        Assert.AreEqual(65f, grass.Level);
        Assert.AreEqual(100f, grass.MaxLevel);
        Assert.AreEqual(0.65f, grass.Fraction, 1e-5f);
        Assert.AreEqual(ResourceLevelStatus.Normal, grass.Status);
    }

    [Test]
    public void LevelChanges_AreReflectedOnNextRefresh()
    {
        Refresh();
        provider.Levels["grass"] = 30f;
        Refresh();

        Assert.AreEqual(30f, reader.States[0].Level);
        Assert.AreEqual(0.3f, reader.States[0].Fraction, 1e-5f);
    }

    [TestCase(20f, ResourceLevelStatus.Critical)]
    [TestCase(19.9f, ResourceLevelStatus.Critical)]
    [TestCase(0f, ResourceLevelStatus.Critical)]
    [TestCase(20.1f, ResourceLevelStatus.Normal)]
    public void CriticalAtOrBelowThreshold(float level, ResourceLevelStatus expected)
    {
        provider.Levels["grass"] = level;
        Refresh();

        Assert.AreEqual(expected, reader.States[0].Status);
    }

    [Test]
    public void RecoveringAboveThreshold_ClearsCritical()
    {
        provider.Levels["grass"] = 5f;
        Refresh();
        Assert.AreEqual(ResourceLevelStatus.Critical, reader.States[0].Status);

        provider.Levels["grass"] = 50f;
        Refresh();
        Assert.AreEqual(ResourceLevelStatus.Normal, reader.States[0].Status);
    }

    [Test]
    public void PerResourceLimits_OverrideDefaults()
    {
        limits["water"] = new ResourceGaugeLimits(50f, 0.5f);
        provider.Levels["water"] = 20f;
        Refresh();

        var water = reader.States[1];
        Assert.AreEqual(0.4f, water.Fraction, 1e-5f);
        Assert.AreEqual(ResourceLevelStatus.Critical, water.Status, "20/50 is below the 50% override");
        Assert.AreEqual(ResourceLevelStatus.Normal, reader.States[0].Status, "grass still uses defaults");
    }

    [Test]
    public void AboveMax_ShowsRealLevelButFullBar()
    {
        provider.Levels["grass"] = 130f;
        Refresh();

        Assert.AreEqual(130f, reader.States[0].Level);
        Assert.AreEqual(1f, reader.States[0].Fraction);
    }

    [Test]
    public void NegativeLevel_ClampsToEmptyAndCritical()
    {
        provider.Levels["grass"] = -4f;
        Refresh();

        Assert.AreEqual(0f, reader.States[0].Level);
        Assert.AreEqual(0f, reader.States[0].Fraction);
        Assert.AreEqual(ResourceLevelStatus.Critical, reader.States[0].Status);
    }

    [Test]
    public void NonPositiveMax_DoesNotDivideByZero()
    {
        limits["grass"] = new ResourceGaugeLimits(0f, 0.2f);
        Refresh();

        Assert.AreEqual(1f, reader.States[0].Fraction);
        Assert.AreEqual(ResourceLevelStatus.Normal, reader.States[0].Status);
    }

    [Test]
    public void MissingOrNaNValue_IsUnavailableNotCritical()
    {
        provider.Levels.Remove("grass");
        provider.Levels["water"] = float.NaN;
        Refresh();

        Assert.AreEqual(ResourceLevelStatus.Unavailable, reader.States[0].Status);
        Assert.AreEqual(ResourceLevelStatus.Unavailable, reader.States[1].Status);
        Assert.AreEqual(0f, reader.States[0].Fraction);
    }

    [Test]
    public void EmptyProvider_ThenDataArrives_ShowsLiveValues()
    {
        // Mirrors the live adapter today: no resources until T02 is wired.
        var empty = new FakeProvider();
        reader.Refresh(empty, _ => Default);
        Assert.AreEqual(0, reader.States.Count);

        empty.Add("grass", "Grass", 12f);
        reader.Refresh(empty, _ => Default);
        Assert.AreEqual(1, reader.States.Count);
        Assert.AreEqual(12f, reader.States[0].Level);
        Assert.AreEqual(ResourceLevelStatus.Critical, reader.States[0].Status);
    }

    [Test]
    public void DuplicateAndEmptyIds_AreSkipped()
    {
        provider.Ids.Add("grass");
        provider.Ids.Add("");
        Refresh();

        Assert.AreEqual(2, reader.States.Count);
    }

    [Test]
    public void NullProvider_ClearsRows()
    {
        Refresh();
        reader.Refresh(null, _ => Default);
        Assert.AreEqual(0, reader.States.Count);
    }

    private class FakeProvider : ISimulationDataProvider
    {
        public readonly List<string> Ids = new();
        public readonly Dictionary<string, float> Levels = new();
        private readonly Dictionary<string, string> names = new();

        public void Add(string id, string name, float level)
        {
            Ids.Add(id);
            names[id] = name;
            Levels[id] = level;
        }

        public IReadOnlyList<string> GetSpeciesIds() => new List<string>();

        public IReadOnlyList<string> GetResourceIds() => Ids;

        public bool TryGetPopulationCount(string speciesId, out float count)
        {
            count = 0f;
            return false;
        }

        public bool TryGetResourceLevel(string resourceId, out float level) =>
            Levels.TryGetValue(resourceId, out level);

        public string GetDisplayName(string id) =>
            names.TryGetValue(id, out var name) ? name : id;
    }
}
