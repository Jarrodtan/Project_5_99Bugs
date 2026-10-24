// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// ProcAnim's replacement for an Animator Controller: tracks the current and previous
/// <see cref="MotionState"/>, advances each state's cycle, and cross-fades between them.
/// Plain C# (no MonoBehaviour), so it can be tested and benchmarked without a scene.
/// </summary>
public class MotionStateMachine
{
    public MotionState Current { get; private set; } = MotionState.Idle;
    public MotionState Previous { get; private set; } = MotionState.Idle;

    /// <summary>0 when a state was just entered, 1 once the cross-fade has finished.</summary>
    public float Blend { get; private set; } = 1f;

    /// <summary>Seconds since <see cref="Current"/> was entered.</summary>
    public float TimeInState { get; private set; }

    float phase, previousPhase, previousTime;

    public void SetState(MotionState state)
    {
        if (state == Current) return;
        Previous = Current;
        previousPhase = phase;
        previousTime = TimeInState;
        Current = state;
        phase = 0f;
        TimeInState = 0f;
        Blend = 0f;
    }

    /// <summary>Advances by <paramref name="deltaTime"/> while moving at <paramref name="speed"/> m/s and returns the blended pose.</summary>
    public MotionPose Evaluate(MotionProfile profile, float speed, float deltaTime)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        TimeInState += deltaTime;
        previousTime += deltaTime;
        Blend = profile.blendSeconds <= 0f ? 1f : Mathf.Min(1f, Blend + deltaTime / profile.blendSeconds);

        phase = Advance(profile, Current, phase, speed, deltaTime);
        var pose = PoseOf(profile, Current, phase, TimeInState);
        if (Blend >= 1f) return pose;

        // The old state keeps moving while it fades out, so the hand-over never freezes.
        previousPhase = Advance(profile, Previous, previousPhase, speed, deltaTime);
        var from = PoseOf(profile, Previous, previousPhase, previousTime);
        return MotionPose.Lerp(from, pose, Mathf.SmoothStep(0f, 1f, Blend));
    }

    static float Advance(MotionProfile profile, MotionState state, float phase, float speed, float deltaTime)
        => Mathf.Repeat(phase + profile.Get(state).CycleRate(speed) * deltaTime, 1f);

    static MotionPose PoseOf(MotionProfile profile, MotionState state, float phase, float time)
        => state == MotionState.Die ? DeathPose(profile, time) : profile.Get(state).Evaluate(phase);

    /// <summary>
    /// Child-friendly death: lies down gently onto its side (ease-out), stays still for a moment so
    /// it's noticed, then shrinks away (ease-in) instead of leaving a body. Optional sink.
    /// </summary>
    public static MotionPose DeathPose(MotionProfile profile, float time)
    {
        float lie = DeathLieProgress(profile, time);
        float sink = Mathf.Clamp01((time - profile.deathSeconds) / profile.deathSinkSeconds);
        float shrink = profile.deathShrinkSeconds <= 0f ? 0f
            : Mathf.Clamp01((time - DeathShrinkStart(profile)) / profile.deathShrinkSeconds);
        return new MotionPose
        {
            Height = -profile.deathSink * sink,
            Roll = profile.deathRoll * lie,
            Scale = 1f - shrink * shrink,
        };
    }

    /// <summary>0 → 1 while lying down (ease-out).</summary>
    public static float DeathLieProgress(MotionProfile profile, float time)
    {
        float t = Mathf.Clamp01(time / profile.deathSeconds);
        return 1f - (1f - t) * (1f - t);
    }

    /// <summary>Seconds after dying when it starts shrinking away (and the death effect appears).</summary>
    public static float DeathShrinkStart(MotionProfile profile) => profile.deathSeconds + profile.deathPauseSeconds;

    // ---- Overlays: layered on top of any state by ProcAnimator ----

    /// <summary>
    /// Lean into a turn (roll, degrees; + = right) for a turn rate in degrees/second (+ = turning right).
    /// No lean when standing.
    /// </summary>
    public static float TurnLeanTarget(MotionProfile profile, float yawRateDegPerSec, float speed)
    {
        if (speed < 0.05f) return 0f;
        return Mathf.Clamp(yawRateDegPerSec * profile.turnLeanPerDegPerSec, -profile.maxTurnLean, profile.maxTurnLean);
    }

    /// <summary>Nose dips when braking and lifts when speeding up (pitch, degrees) for an acceleration in m/s².</summary>
    public static float AccelPitchTarget(MotionProfile profile, float acceleration)
        => Mathf.Clamp(-acceleration * profile.pitchPerAccel, -profile.maxAccelPitch, profile.maxAccelPitch);

    /// <summary>Scale during the spawn pop: grows from 0 with a small overshoot (ease-out-back), then stays at 1.</summary>
    public static float SpawnScale(MotionProfile profile, float timeSinceSpawn)
    {
        if (profile.spawnSeconds <= 0f || timeSinceSpawn >= profile.spawnSeconds) return 1f;
        float t = Mathf.Clamp01(timeSinceSpawn / profile.spawnSeconds) - 1f;
        const float overshoot = 1.70158f;
        return 1f + t * t * ((overshoot + 1f) * t + overshoot);
    }
}
// [AI-GENERATED END]
