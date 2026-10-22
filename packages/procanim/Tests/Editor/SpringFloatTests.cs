// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using NUnit.Framework;
using UnityEngine;

public class SpringFloatTests
{
    static float Run(ref SpringFloat s, float target, float hz, float damping, float seconds, float fps, out float max)
    {
        max = s.Value;
        int frames = Mathf.RoundToInt(seconds * fps);
        for (int i = 0; i < frames; i++)
        {
            s.Step(target, hz, damping, 1f / fps);
            max = Mathf.Max(max, s.Value);
        }
        return s.Value;
    }

    [Test]
    public void SettlesOnTarget()
    {
        var s = new SpringFloat();
        Assert.AreEqual(1f, Run(ref s, 1f, 3f, 0.35f, 5f, 60f, out _), 1e-3f);
    }

    [Test]
    public void Underdamped_OvershootsThenSettles()
    {
        var s = new SpringFloat();
        Run(ref s, 1f, 3f, 0.3f, 5f, 60f, out float max);
        Assert.Greater(max, 1.05f, "a bouncy spring should overshoot");
    }

    [Test]
    public void CriticallyDamped_NeverOvershoots()
    {
        var s = new SpringFloat();
        Run(ref s, 1f, 3f, 1f, 5f, 60f, out float max);
        Assert.LessOrEqual(max, 1f + 1e-4f);
    }

    [Test]
    public void SameMotionAt30And120Fps()
    {
        var slow = new SpringFloat();
        var fast = new SpringFloat();
        float a = Run(ref slow, 1f, 3f, 0.35f, 0.5f, 30f, out _);
        float b = Run(ref fast, 1f, 3f, 0.35f, 0.5f, 120f, out _);
        Assert.AreEqual(b, a, 0.01f);
    }

    [Test]
    public void ZeroDeltaTime_ChangesNothing()
    {
        var s = new SpringFloat { Value = 0.3f, Velocity = 2f };
        s.Step(1f, 3f, 0.35f, 0f);
        Assert.AreEqual(0.3f, s.Value);
        Assert.AreEqual(2f, s.Velocity);
    }
}
// [AI-GENERATED END]
