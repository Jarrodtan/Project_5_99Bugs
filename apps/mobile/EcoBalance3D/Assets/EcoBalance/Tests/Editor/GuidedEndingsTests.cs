// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Endings (doc: Ecosystem Outcomes & Endings, Ending Gallery), achievements (Badges and Achievement) and the
// graph helpers they use, without a scene.
public class GuidedEndingsTests
{
    int? savedGallery, savedBadges;

    [SetUp]
    public void Save()
    {
        savedGallery = PlayerPrefs.HasKey(GuidedChallenge.GalleryKey) ? PlayerPrefs.GetInt(GuidedChallenge.GalleryKey) : (int?)null;
        savedBadges = PlayerPrefs.HasKey(GuidedAchievements.Key) ? PlayerPrefs.GetInt(GuidedAchievements.Key) : (int?)null;
        PlayerPrefs.DeleteKey(GuidedChallenge.GalleryKey);
        PlayerPrefs.DeleteKey(GuidedAchievements.Key);
    }

    [TearDown]
    public void Restore()
    {
        if (savedGallery == null) PlayerPrefs.DeleteKey(GuidedChallenge.GalleryKey); else PlayerPrefs.SetInt(GuidedChallenge.GalleryKey, savedGallery.Value);
        if (savedBadges == null) PlayerPrefs.DeleteKey(GuidedAchievements.Key); else PlayerPrefs.SetInt(GuidedAchievements.Key, savedBadges.Value);
        PlayerPrefs.Save();
    }

    // ---------- Endings ----------

    [TestCase(0, 0, 90f, "END05")]    // collapse first, whatever the grass
    [TestCase(7, 0, 90f, "END03")]    // predator-free
    [TestCase(0, 2, 90f, "END04")]    // disappearing herd
    [TestCase(5, 1, 50f, "END01")]    // thriving: exactly on every threshold
    [TestCase(4, 1, 90f, "END02")]    // too few sheep
    [TestCase(9, 1, 49.9f, "END02")]  // too little grass
    public void Classify_FollowsTheDocPriority(int sheep, int leopards, float grass, string id)
    {
        Assert.AreEqual(id, GuidedEndings.All[GuidedEndings.Classify(sheep, leopards, grass)].id);
    }

    [Test]
    public void FiveEndings_WithTheDocText_InGalleryOrder()
    {
        Assert.AreEqual(5, GuidedEndings.Count);
        var ids = new List<string>();
        foreach (int i in GuidedEndings.GalleryOrder) ids.Add(GuidedEndings.All[i].id);
        CollectionAssert.AreEqual(new[] { "END01", "END02", "END03", "END04", "END05" }, ids);
        foreach (var e in GuidedEndings.All)
        {
            Assert.IsNotEmpty(e.description); Assert.IsNotEmpty(e.meaning); Assert.IsNotEmpty(e.takeaway); Assert.IsNotEmpty(e.card);
        }
        StringAssert.StartsWith("Blue Sheep and Snow Leopards are still living in the highlands", GuidedEndings.All[3].description);
        Assert.AreEqual("Plants support herbivores, and herbivores support predators. Protecting one part of the food web can affect the others.", GuidedEndings.All[3].takeaway);
    }

    [Test]
    public void Gallery_UnlocksOnce_AndCounts()
    {
        Assert.AreEqual("0 / 5 Endings Discovered", GuidedEndings.Progress());
        Assert.IsTrue(GuidedEndings.Unlock(3), "new ending");
        Assert.IsFalse(GuidedEndings.Unlock(3), "the same ending again: no duplicate");
        Assert.IsTrue(GuidedEndings.Unlock(0));
        Assert.AreEqual("2 / 5 Endings Discovered", GuidedEndings.Progress());
        Assert.IsTrue(GuidedEndings.IsUnlocked(0) && GuidedEndings.IsUnlocked(3) && !GuidedEndings.IsUnlocked(4));
    }

    [Test]
    public void FocalOutcome_SaysWhetherTheAnimalSurvived_WithoutBlame()
    {
        Assert.AreEqual("Miloy made it through all 20 days!", GuidedChallenge.FocalOutcome("Miloy", false, 20, FamilyDeathCause.Other, 20));
        StringAssert.Contains("caught by a Snow Leopard on Day 7", GuidedChallenge.FocalOutcome("Miloy", true, 7, FamilyDeathCause.Caught, 20));
        StringAssert.Contains("enough food", GuidedChallenge.FocalOutcome("Snowy", true, 9, FamilyDeathCause.Hunger, 20));
        StringAssert.Contains("enough water", GuidedChallenge.FocalOutcome("Snowy", true, 9, FamilyDeathCause.Thirst, 20));
        foreach (FamilyDeathCause c in System.Enum.GetValues(typeof(FamilyDeathCause)))
        {
            var line = GuidedChallenge.FocalOutcome("Snowy", true, 3, c, 20);
            StringAssert.Contains("ended early", line);
            StringAssert.DoesNotContain("you", line.ToLowerInvariant(), "never blames the player");
        }
    }

    // ---------- Achievements ----------

    [Test]
    public void ThreeMissions_OnDays1to3_WithTheDocBadges()
    {
        Assert.AreEqual(3, GuidedAchievements.Count);
        CollectionAssert.AreEqual(new[] { "Resource Explorer", "Survival Guardian", "Ecosystem Guardian" },
            System.Array.ConvertAll(GuidedAchievements.All, m => m.badge));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, System.Array.ConvertAll(GuidedAchievements.All, m => m.day));
        CollectionAssert.AreEqual(new[] { "Find What You Need", "Stay Safe, Stay Strong", "Keep the Balance" },
            System.Array.ConvertAll(GuidedAchievements.All, m => m.name));
    }

    [Test]
    public void Mission_CompletesWhenEveryStepIsDone_AndTheTrackerMovesOn()
    {
        var done = new HashSet<string>();
        var m1 = GuidedAchievements.Get(GuidedAchievements.Id.ResourceExplorer);
        Assert.AreEqual(m1, GuidedAchievements.Current(1, done));
        done.Add(GuidedAchievements.ResourceDecision);
        done.Add(GuidedAchievements.InspectStep);
        Assert.IsFalse(GuidedAchievements.IsComplete(m1, done), "Day 1 hasn't ended");
        done.Add(GuidedAchievements.DayDone(1));
        Assert.IsTrue(GuidedAchievements.IsComplete(m1, done));
        Assert.IsNull(GuidedAchievements.Current(1, done), "nothing else has started on Day 1");
        Assert.AreEqual(GuidedAchievements.Id.SurvivalGuardian, GuidedAchievements.Current(2, done).id);
        Assert.AreEqual(GuidedAchievements.Id.EcosystemGuardian, GuidedAchievements.Current(3, done, m => m.id == GuidedAchievements.Id.SurvivalGuardian).id,
            "a mission that can't be finished any more is skipped");
        Assert.IsFalse(GuidedAchievements.QuestionReady(done));
        done.Add(GuidedAchievements.DayDone(3));
        Assert.IsTrue(GuidedAchievements.QuestionReady(done), "the Day 3 question comes after Day 3");
    }

    [Test]
    public void Badges_UnlockOnce_AndAreKept()
    {
        Assert.AreEqual("0 / 3 Badges Earned", GuidedAchievements.Progress());
        Assert.IsTrue(GuidedAchievements.Unlock(GuidedAchievements.Id.SurvivalGuardian));
        Assert.IsFalse(GuidedAchievements.Unlock(GuidedAchievements.Id.SurvivalGuardian));
        Assert.IsTrue(GuidedAchievements.IsUnlocked(GuidedAchievements.Id.SurvivalGuardian));
        Assert.IsFalse(GuidedAchievements.IsUnlocked(GuidedAchievements.Id.ResourceExplorer));
        Assert.AreEqual("1 / 3 Badges Earned", GuidedAchievements.Progress());
    }

    // ---------- Graph helpers ----------

    [Test]
    public void ClipX_CutsTheLineExactlyAtTheEdge()
    {
        var pts = new List<Vector2> { new(0, 0), new(10, 10), new(20, 0) };
        var cut = PopulationGraphMath.ClipX(pts, 15f);
        Assert.AreEqual(3, cut.Count);
        Assert.AreEqual(new Vector2(15, 5), cut[2]);
        Assert.AreEqual(3, PopulationGraphMath.ClipX(pts, 99f).Count, "past the end: the whole line");
        Assert.AreEqual(0, PopulationGraphMath.ClipX(pts, -1f).Count);
    }

    [Test]
    public void MyRuns_BecomeAGraphHistory_WithDayLabels()
    {
        var run = new MyRuns();
        for (int i = 0; i < 5; i++) { run.t.Add(i * 30); run.days.Add(i + 1); run.sheep.Add(10 + i); run.leopard.Add(2); run.trees.Add(40 - i); }
        var (history, days) = FieldJournalScreen.RunHistory(run);
        Assert.AreEqual(5, history.Count);
        Assert.AreEqual(14f, history.Latest.Get("BlueSheep"));
        Assert.AreEqual(36f, history.Latest.Get("Trees"));
        Assert.AreEqual(5, days[history.Latest.Tick]);
    }
}
