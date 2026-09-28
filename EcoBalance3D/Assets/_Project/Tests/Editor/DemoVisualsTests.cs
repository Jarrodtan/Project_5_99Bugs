// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using NUnit.Framework;

/// <summary>Tests the pure rules that map simulation numbers to what the M1 demo shows.</summary>
public class DemoVisualsTests
{
    [Test]
    public void GrassField_FullResource_ShowsAllPatches()
    {
        Assert.AreEqual(30, GrassField.VisibleCount(100f, 100f, 30));
    }

    [Test]
    public void GrassField_HalfResource_ShowsHalfThePatches()
    {
        Assert.AreEqual(15, GrassField.VisibleCount(50f, 100f, 30));
    }

    [Test]
    public void GrassField_EmptyOrInvalid_ShowsNothingAndNeverOverflows()
    {
        Assert.AreEqual(0, GrassField.VisibleCount(0f, 100f, 30));
        Assert.AreEqual(0, GrassField.VisibleCount(50f, 0f, 30));
        Assert.AreEqual(30, GrassField.VisibleCount(500f, 100f, 30));
    }

    [Test]
    public void PopulationView_OneAgentPerGroup_AtLeastOneWhileAlive()
    {
        Assert.AreEqual(13, EcoPopulationView.TargetCount(50f, 4f, 20));
        Assert.AreEqual(1, EcoPopulationView.TargetCount(0.3f, 4f, 20));
        Assert.AreEqual(0, EcoPopulationView.TargetCount(0f, 4f, 20));
    }

    [Test]
    public void PopulationView_NeverExceedsMaxVisible()
    {
        Assert.AreEqual(20, EcoPopulationView.TargetCount(1000f, 4f, 20));
    }
}
// [AI-GENERATED END]
