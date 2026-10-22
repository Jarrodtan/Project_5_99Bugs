// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

public class MotionStateMachineTests
{
    MotionProfile profile;

    [SetUp] public void SetUp() => profile = ScriptableObject.CreateInstance<MotionProfile>();
    [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(profile);

    static void Tick(MotionStateMachine m, MotionProfile p, float speed, float seconds, float fps = 60f)
    {
        for (int i = 0; i < Mathf.RoundToInt(seconds * fps); i++) m.Evaluate(p, speed, 1f / fps);
    }

    [Test]
    public void CrossFade_TakesBlendSeconds()
    {
        var m = new MotionStateMachine();
        m.SetState(MotionState.Walk);
        Assert.AreEqual(0f, m.Blend);
        Tick(m, profile, 1f, profile.blendSeconds / 2f);
        Assert.AreEqual(0.5f, m.Blend, 0.05f);
        Tick(m, profile, 1f, profile.blendSeconds);
        Assert.AreEqual(1f, m.Blend);
    }

    [Test]
    public void SettingTheSameState_DoesNotRestartIt()
    {
        var m = new MotionStateMachine();
        m.SetState(MotionState.Run);
        Tick(m, profile, 3f, 1f);
        m.SetState(MotionState.Run);
        Assert.AreEqual(1f, m.Blend);
        Assert.Greater(m.TimeInState, 0.9f);
    }

    [Test]
    public void Gait_StepsFasterWhenMovingFaster()
    {
        Assert.AreEqual(2f * profile.walk.CycleRate(1f), profile.walk.CycleRate(2f), 1e-5f);
        Assert.AreEqual(0f, profile.walk.CycleRate(0f), "a walk with no movement doesn't step");
        Assert.AreEqual(profile.idle.cycleHz, profile.idle.CycleRate(5f), "idle loops in time, not distance");
    }

    // Without a cap a fast (or 8x-speed) animal stepped many times a second and looked like it was pounding.
    [Test]
    public void Gait_StepRateIsCapped()
    {
        foreach (var gait in new[] { profile.walk, profile.run, profile.flee, profile.stalk })
            Assert.LessOrEqual(gait.CycleRate(1000f), gait.maxCycleHz);
    }

    [Test]
    public void FootfallBob_IsSmooth_GroundAtFootfallsAndTopMidStride()
    {
        var walk = profile.walk;
        Assert.AreEqual(walk.height, walk.Evaluate(0f).Height, 1e-5f, "on the ground at a footfall");
        Assert.AreEqual(walk.height + walk.bobHeight, walk.Evaluate(0.25f).Height, 1e-5f, "highest mid-stride");
        // No sharp corner at the footfall: the bob barely changes just either side of it.
        float justBefore = walk.Evaluate(-0.01f).Height - walk.height;
        float justAfter = walk.Evaluate(0.01f).Height - walk.height;
        Assert.Less(justBefore, walk.bobHeight * 0.01f);
        Assert.Less(justAfter, walk.bobHeight * 0.01f);
    }

    [Test]
    public void Walk_StandingStill_DoesNotBob()
    {
        var m = new MotionStateMachine();
        m.SetState(MotionState.Walk);
        Tick(m, profile, 0f, 1f);
        float first = m.Evaluate(profile, 0f, 1f / 60f).Height;
        for (int i = 0; i < 60; i++)
            Assert.AreEqual(first, m.Evaluate(profile, 0f, 1f / 60f).Height, 1e-6f);
    }

    // TI-10 evidence: every state looks different (here: a quarter of the way into its cycle,
    // and death once it has tipped over).
    [Test]
    public void EveryState_HasADistinctPose()
    {
        var poses = new Dictionary<MotionState, MotionPose>();
        foreach (MotionState s in Enum.GetValues(typeof(MotionState)))
            poses[s] = s == MotionState.Die
                ? MotionStateMachine.DeathPose(profile, profile.deathSeconds)
                : profile.Get(s).Evaluate(0.25f);

        var states = new List<MotionState>(poses.Keys);
        for (int i = 0; i < states.Count; i++)
        for (int j = i + 1; j < states.Count; j++)
        {
            MotionPose a = poses[states[i]], b = poses[states[j]];
            float diff = Mathf.Abs(a.Height - b.Height) * 100f + Mathf.Abs(a.Pitch - b.Pitch)
                       + Mathf.Abs(a.Roll - b.Roll) + Mathf.Abs(a.Scale - b.Scale) * 100f;
            Assert.Greater(diff, 0.5f, $"{states[i]} and {states[j]} look the same");
        }
    }

    [Test]
    public void Death_TipsOverThenSinksAndStays()
    {
        var tipped = MotionStateMachine.DeathPose(profile, profile.deathSeconds);
        Assert.AreEqual(profile.deathRoll, tipped.Roll, 1e-3f);
        Assert.AreEqual(0f, tipped.Height, 1e-5f, "sinking starts after tipping over");

        float end = profile.deathSeconds + profile.deathSinkSeconds;
        Assert.AreEqual(-profile.deathSink, MotionStateMachine.DeathPose(profile, end).Height, 1e-5f);
        Assert.AreEqual(-profile.deathSink, MotionStateMachine.DeathPose(profile, end + 10f).Height, 1e-5f);
    }

    [Test]
    public void TurnLean_LeansIntoTheTurn_ClampedAndOnlyWhenMoving()
    {
        Assert.Greater(MotionStateMachine.TurnLeanTarget(profile, 90f, 1f), 0f, "turning right leans right");
        Assert.Less(MotionStateMachine.TurnLeanTarget(profile, -90f, 1f), 0f);
        Assert.AreEqual(profile.maxTurnLean, MotionStateMachine.TurnLeanTarget(profile, 10000f, 1f));
        Assert.AreEqual(0f, MotionStateMachine.TurnLeanTarget(profile, 90f, 0f));
    }

    [Test]
    public void Braking_DipsTheNose_Clamped()
    {
        Assert.Greater(MotionStateMachine.AccelPitchTarget(profile, -2f), 0f);
        Assert.Less(MotionStateMachine.AccelPitchTarget(profile, 2f), 0f);
        Assert.AreEqual(profile.maxAccelPitch, MotionStateMachine.AccelPitchTarget(profile, -1000f));
    }

    [Test]
    public void Spawn_PopsInWithOvershoot_ThenFullSize()
    {
        Assert.AreEqual(0f, MotionStateMachine.SpawnScale(profile, 0f), 1e-5f);
        float max = 0f;
        for (float t = 0f; t < profile.spawnSeconds; t += 0.005f) max = Mathf.Max(max, MotionStateMachine.SpawnScale(profile, t));
        Assert.Greater(max, 1f);
        Assert.AreEqual(1f, MotionStateMachine.SpawnScale(profile, profile.spawnSeconds));
    }

    [Test]
    public void SameInputs_SamePoses()
    {
        var a = new MotionStateMachine();
        var b = new MotionStateMachine();
        foreach (var m in new[] { a, b })
        {
            m.SetState(MotionState.Run);
            Tick(m, profile, 3f, 0.7f);
            m.SetState(MotionState.Eat);
        }
        for (int i = 0; i < 30; i++)
        {
            var pa = a.Evaluate(profile, 0.2f, 1f / 60f);
            var pb = b.Evaluate(profile, 0.2f, 1f / 60f);
            Assert.AreEqual(pa.Height, pb.Height);
            Assert.AreEqual(pa.Pitch, pb.Pitch);
        }
    }

    [Test]
    public void EnsurePivot_MovesChildrenUnderIt_KeepingWorldPositions()
    {
        var root = new GameObject("animal");
        try
        {
            root.transform.localScale = Vector3.one * 0.2f;
            var body = new GameObject("body").transform;
            body.SetParent(root.transform, false);
            body.localPosition = new Vector3(1f, 2f, 3f);
            Vector3 world = body.position;

            var pivot = ProcAnimator.EnsurePivot(root.transform);
            Assert.AreSame(pivot, body.parent);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.That(Vector3.Distance(world, body.position), Is.LessThan(1e-4f));
            Assert.AreSame(pivot, ProcAnimator.EnsurePivot(root.transform), "second call reuses the pivot");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // One frame of 500 animated animals (state machine + overlays), the most the tablet should
    // ever show. Generous budget: fails only on a real regression.
    [Test]
    public void Perf_500Animals_OneFrame()
    {
        var machines = new MotionStateMachine[500];
        for (int i = 0; i < machines.Length; i++)
        {
            machines[i] = new MotionStateMachine();
            machines[i].SetState((MotionState)(i % 9));
        }
        void Frame()
        {
            foreach (var m in machines)
            {
                var pose = m.Evaluate(profile, 1.5f, 1f / 60f);
                pose.Roll += MotionStateMachine.TurnLeanTarget(profile, 45f, 1.5f);
                pose.Pitch += MotionStateMachine.AccelPitchTarget(profile, -1f);
            }
        }
        Frame(); // warm-up (JIT)
        const int repeats = 100;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < repeats; i++) Frame();
        double ms = sw.Elapsed.TotalMilliseconds / repeats;
        UnityEngine.Debug.Log($"[Perf] procanim_500_animals_frame: {ms:0.000} ms (budget 2 ms)");
        Assert.Less(ms, 2.0, $"500 animals took {ms:0.00} ms per frame");
    }
}
// [AI-GENERATED END]
