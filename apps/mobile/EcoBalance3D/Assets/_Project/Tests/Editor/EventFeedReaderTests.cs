using System.Collections.Generic;
using NUnit.Framework;

// EditMode tests for Display 3 feed logic: newest-first order, visible cap, and new-entry tracking.
public class EventFeedReaderTests
{
    private FakeEventProvider provider;
    private EventFeedReader reader;

    [SetUp]
    public void SetUp()
    {
        provider = new FakeEventProvider();
        reader = new EventFeedReader();
    }

    [Test]
    public void Entries_AreNewestFirst()
    {
        provider.Add("grazing", "Blue Sheep ate vegetation", 1f);
        provider.Add("predation", "Snow Leopard hunted a Blue Sheep", 2f);
        provider.Add("drinking", "Blue Sheep drank water", 3f);

        reader.Refresh(provider, 10, 0f);

        Assert.AreEqual(3, reader.Entries.Count);
        Assert.AreEqual("Blue Sheep drank water", reader.Entries[0].Text);
        Assert.AreEqual("Snow Leopard hunted a Blue Sheep", reader.Entries[1].Text);
        Assert.AreEqual("Blue Sheep ate vegetation", reader.Entries[2].Text);
    }

    [Test]
    public void Entries_AreCappedToMaxVisible_KeepingNewest()
    {
        for (var i = 0; i < 12; i++)
            provider.Add("grazing", $"event {i}", i);

        reader.Refresh(provider, 5, 0f);

        Assert.AreEqual(5, reader.Entries.Count);
        Assert.AreEqual("event 11", reader.Entries[0].Text);
        Assert.AreEqual("event 7", reader.Entries[4].Text);
    }

    [Test]
    public void EqualTimestamps_LaterListIndexIsNewer()
    {
        provider.Add("grazing", "first", 5f);
        provider.Add("grazing", "second", 5f);

        reader.Refresh(provider, 10, 0f);

        Assert.AreEqual("second", reader.Entries[0].Text);
        Assert.AreEqual("first", reader.Entries[1].Text);
    }

    [Test]
    public void FirstRefresh_DoesNotHighlightExistingHistory()
    {
        provider.Add("grazing", "old", 1f);

        var newCount = reader.Refresh(provider, 10, 100f);

        Assert.AreEqual(0, newCount);
        Assert.IsTrue(float.IsNegativeInfinity(reader.Entries[0].SeenAt));
    }

    [Test]
    public void NewEvents_AreStampedWithRefreshTime_AndOlderEntriesKeepTheirStamp()
    {
        reader.Refresh(provider, 10, 0f);
        provider.Add("grazing", "a", 1f);
        Assert.AreEqual(1, reader.Refresh(provider, 10, 10f));

        provider.Add("predation", "b", 2f);
        provider.Add("drinking", "c", 3f);
        Assert.AreEqual(2, reader.Refresh(provider, 10, 20f));

        Assert.AreEqual("c", reader.Entries[0].Text);
        Assert.AreEqual(20f, reader.Entries[0].SeenAt);
        Assert.AreEqual(20f, reader.Entries[1].SeenAt);
        Assert.AreEqual("a", reader.Entries[2].Text);
        Assert.AreEqual(10f, reader.Entries[2].SeenAt, "shifted entry keeps its original arrival time");
    }

    [Test]
    public void UnchangedHistory_ReportsNoNewEntries()
    {
        provider.Add("grazing", "a", 1f);
        reader.Refresh(provider, 10, 0f);

        Assert.AreEqual(0, reader.Refresh(provider, 10, 5f));
    }

    [Test]
    public void EmptyDescription_FallsBackToEventType()
    {
        provider.Add("predation", "", 1f);

        reader.Refresh(provider, 10, 0f);

        Assert.AreEqual("predation", reader.Entries[0].Text);
    }

    [Test]
    public void NullProviderOrZeroCap_ShowsNothing()
    {
        provider.Add("grazing", "a", 1f);

        reader.Refresh(null, 10, 0f);
        Assert.AreEqual(0, reader.Entries.Count);

        reader.Refresh(provider, 0, 0f);
        Assert.AreEqual(0, reader.Entries.Count);
    }

    [Test]
    public void Reset_SuppressesHighlightOnNextRefresh()
    {
        reader.Refresh(provider, 10, 0f);
        provider.Add("grazing", "a", 1f);
        reader.Reset();

        Assert.AreEqual(0, reader.Refresh(provider, 10, 5f));
        Assert.IsTrue(float.IsNegativeInfinity(reader.Entries[0].SeenAt));
    }

    private class FakeEventProvider : ISimulationEventProvider
    {
        private readonly List<SimulationEvent> events = new List<SimulationEvent>();

        public void Add(string typeId, string description, float timestamp)
        {
            events.Add(new SimulationEvent { EventTypeId = typeId, Description = description, Timestamp = timestamp });
        }

        public IReadOnlyList<SimulationEvent> GetRecentEvents() => events;
    }
}
