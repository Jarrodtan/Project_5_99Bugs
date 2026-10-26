// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using NUnit.Framework;
using UnityEngine;

// AnimalMotionBridge loads Resources/Motion/<species asset name>; a renamed species or a broken
// profile would silently fall back to the defaults, so check each species has a sensible one.
public class MotionProfileAssetTests
{
    [TestCase("Default")]
    [TestCase("BlueSheep")]
    [TestCase("SnowLeopard")]
    public void MotionProfile_LoadsWithUsableSettings(string name)
    {
        var p = Resources.Load<MotionProfile>("Motion/" + name);
        Assert.IsNotNull(p, $"missing Resources/Motion/{name}.asset");
        Assert.Greater(p.walk.stepsPerMetre, 0f, "walk must be a distance-driven gait");
        Assert.Greater(p.run.stepsPerMetre, 0f, "run must be a distance-driven gait");
        Assert.Greater(p.idle.cycleHz, 0f, "idle must loop");
        Assert.Greater(p.eat.cycleHz, 0f, "eat must loop");
        Assert.Greater(p.deathRoll, 45f, "death should tip the animal over");
    }

    [Test]
    public void EverySpecies_HasItsOwnMotionProfile()
    {
        foreach (var species in new[] { "BlueSheep", "SnowLeopard" })
            Assert.IsNotNull(Resources.Load<MotionProfile>("Motion/" + species), $"{species} would use the default motion");
    }
}
// [AI-GENERATED END]
