// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LegLayoutTests
{
    // A "table" animal facing +Z: body 1 wide (X), 2 long (Z), from y = 0.6 to 1.0, on four legs
    // 0.1 thick from y = 0 to 0.6 at the corners.
    static List<Vector3> TableAnimal(bool longAlongX = false)
    {
        var points = new List<Vector3>();
        void Box(Vector3 min, Vector3 max)
        {
            for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++)
            for (int k = 0; k <= 4; k++)
            {
                var p = new Vector3(Mathf.Lerp(min.x, max.x, i / 4f), Mathf.Lerp(min.y, max.y, j / 4f), Mathf.Lerp(min.z, max.z, k / 4f));
                points.Add(longAlongX ? new Vector3(p.z, p.y, p.x) : p);
            }
        }
        Box(new Vector3(-0.5f, 0.6f, -1f), new Vector3(0.5f, 1f, 1f));
        foreach (float x in new[] { -0.45f, 0.45f })
        foreach (float z in new[] { -0.95f, 0.95f })
            Box(new Vector3(x - 0.05f, 0f, z - 0.05f), new Vector3(x + 0.05f, 0.6f, z + 0.05f));
        return points;
    }

    static LegLayout Analyze(bool longAlongX = false) => LegLayout.Analyze(TableAnimal(longAlongX), 0.5f, 0.1f);

    [Test]
    public void FindsFourLegs_WithHipsAtTheCorners()
    {
        var layout = Analyze();
        Assert.IsTrue(layout.Valid);
        Assert.AreEqual(2, layout.LongAxis, "body is longest along Z");
        Assert.AreEqual(0.5f, layout.HipY, 1e-5f);
        Vector3 fl = layout.Hips[LegLayout.FrontLeft], br = layout.Hips[LegLayout.BackRight];
        Assert.AreEqual(-0.45f, fl.x, 0.01f);
        Assert.AreEqual(0.95f, fl.z, 0.01f);
        Assert.AreEqual(0.45f, br.x, 0.01f);
        Assert.AreEqual(-0.95f, br.z, 0.01f);
    }

    [Test]
    public void DetectsTheLongAxis_WhenTheModelFacesX()
    {
        var layout = Analyze(longAlongX: true);
        Assert.IsTrue(layout.Valid);
        Assert.AreEqual(0, layout.LongAxis);
        Assert.AreEqual(Vector3.right, layout.Forward);
    }

    [Test]
    public void BodyIsNotALeg_FeetAreFullyLeg_HipBlends()
    {
        var layout = Analyze();
        Assert.IsFalse(layout.Classify(new Vector3(0f, 0.8f, 0f), out _, out _), "body point");
        Assert.IsTrue(layout.Classify(new Vector3(-0.45f, 0f, 0.95f), out int leg, out float foot));
        Assert.AreEqual(LegLayout.FrontLeft, leg);
        Assert.AreEqual(1f, foot, 1e-5f);
        layout.Classify(new Vector3(-0.45f, 0.49f, 0.95f), out _, out float nearHip);
        Assert.That(nearHip, Is.GreaterThan(0f).And.LessThan(0.1f), "just below the hip barely moves, so the mesh doesn't tear");
    }

    [Test]
    public void PositiveAngle_SwingsTheFootForward_AndKeepsLegLength()
    {
        var layout = Analyze();
        var foot = new Vector3(-0.45f, 0f, 0.95f);
        var moved = layout.Rotate(foot, LegLayout.FrontLeft, 30f, 1f);
        Assert.Greater(moved.z, foot.z, "foot moves forward (+Z)");
        Assert.Greater(moved.y, foot.y, "and lifts as it swings");
        var hip = layout.Hips[LegLayout.FrontLeft];
        Assert.AreEqual((foot - hip).magnitude, (moved - hip).magnitude, 1e-4f, "rotation keeps the leg's length");
        Assert.AreEqual(foot, layout.Rotate(foot, LegLayout.FrontLeft, 30f, 0f), "weight 0 = no movement");
    }

    [Test]
    public void Trot_DiagonalLegsMoveTogether_PairsOpposite()
    {
        for (float phase = 0f; phase < 1f; phase += 0.1f)
        {
            float fl = LegLayout.LegAngle(LegLayout.FrontLeft, phase, 20f, 0f, 0f);
            float br = LegLayout.LegAngle(LegLayout.BackRight, phase, 20f, 0f, 0f);
            float fr = LegLayout.LegAngle(LegLayout.FrontRight, phase, 20f, 0f, 0f);
            Assert.AreEqual(fl, br, 1e-4f);
            Assert.AreEqual(-fl, fr, 1e-4f);
        }
    }

    [Test]
    public void FixedPoses_ApplyToFrontAndBackLegs()
    {
        // e.g. eating: front legs brace forward, back legs back; no swing.
        Assert.AreEqual(12f, LegLayout.LegAngle(LegLayout.FrontRight, 0.3f, 0f, 12f, -6f), 1e-5f);
        Assert.AreEqual(-6f, LegLayout.LegAngle(LegLayout.BackLeft, 0.3f, 0f, 12f, -6f), 1e-5f);
    }

    [Test]
    public void NotFourLegged_IsRejected()
    {
        var blob = new List<Vector3>();
        for (int i = 0; i < 50; i++) blob.Add(new Vector3(0.5f, i / 50f, 0.5f)); // one thin column
        Assert.IsFalse(LegLayout.Analyze(blob, 0.4f, 0.1f).Valid);
        Assert.IsFalse(LegLayout.Analyze(new List<Vector3>(), 0.4f, 0.1f).Valid);
    }

    [Test]
    public void ProfileDefaults_SwingLegsOnlyInGaits()
    {
        var p = ScriptableObject.CreateInstance<MotionProfile>();
        try
        {
            Assert.Greater(p.walk.legSwing, 0f);
            Assert.Greater(p.run.legSwing, p.walk.legSwing, "running swings further than walking");
            Assert.AreEqual(0f, p.idle.legSwing);
            Assert.AreEqual(0f, p.eat.legSwing);
            Assert.Greater(p.eat.frontLegAngle, 0f, "front legs brace to eat");
            Assert.Greater(p.deathLegSpread, 0f);
        }
        finally { Object.DestroyImmediate(p); }
    }
}
// [AI-GENERATED END]
