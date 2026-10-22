// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// How one motion state looks. A looping cycle drives a bob, a pitch rock, a roll sway and a
/// scale pulse around a fixed base offset. Gaits (walk, run...) advance the cycle by distance
/// travelled (<see cref="stepsPerMetre"/>), so faster animals step faster; other states loop in
/// time (<see cref="cycleHz"/>).
/// </summary>
[System.Serializable]
public class StateMotion
{
    [Tooltip("Base lift in metres (negative = crouch).")] public float height;
    [Tooltip("Base pitch in degrees (+ = nose down).")] public float pitch;
    [Tooltip("Base roll in degrees.")] public float roll;

    [Tooltip("Bob amplitude in metres.")] public float bobHeight;
    [Tooltip("Pitch rock amplitude in degrees.")] public float pitchWave;
    [Tooltip("Roll sway amplitude in degrees.")] public float rollWave;
    [Tooltip("Scale pulse amplitude (0.02 = 2%), e.g. breathing.")] public float scalePulse;

    [Tooltip("Cycles per metre travelled (gaits). 0 = loop in time using Cycle Hz.")]
    [Min(0f)] public float stepsPerMetre;
    [Tooltip("Cycles per second when not distance-driven.")]
    [Min(0f)] public float cycleHz;
    [Tooltip("Gaits never cycle faster than this, however fast the animal moves (stops it looking like pounding).")]
    [Min(0f)] public float maxCycleHz = 2f;
    [Tooltip("Bob rises twice per cycle (one per footfall) instead of once.")]
    public bool bounce;

    /// <summary>The pose at <paramref name="phase"/> (in cycles; only the fraction matters).</summary>
    public MotionPose Evaluate(float phase)
    {
        float angle = 2f * Mathf.PI * phase;
        float s = Mathf.Sin(angle);
        // Footfall bob: two smooth humps per cycle, 0 at each footfall, 1 mid-stride (no sharp corners).
        float bob = bounce ? 0.5f - 0.5f * Mathf.Cos(2f * angle) : s;
        return new MotionPose
        {
            Height = height + bobHeight * bob,
            Pitch = pitch + pitchWave * s,
            Roll = roll + rollWave * Mathf.Cos(angle),
            Scale = 1f + scalePulse * s,
        };
    }

    /// <summary>Cycles per second at <paramref name="speed"/> m/s.</summary>
    public float CycleRate(float speed)
    {
        if (stepsPerMetre <= 0f) return cycleHz;
        float rate = stepsPerMetre * Mathf.Max(0f, speed);
        return maxCycleHz > 0f ? Mathf.Min(rate, maxCycleHz) : rate;
    }
}

/// <summary>
/// All motion settings for one kind of animal, as an asset, so designers tune motion without code
/// (Create > ProcAnim > Motion Profile). Defaults suit a small four-legged grazer.
/// </summary>
[CreateAssetMenu(menuName = "ProcAnim/Motion Profile", fileName = "Motion_")]
public class MotionProfile : ScriptableObject
{
    [Header("States")]
    public StateMotion idle = new StateMotion { cycleHz = 0.35f, pitchWave = 1f, scalePulse = 0.015f };
    public StateMotion walk = new StateMotion { stepsPerMetre = 0.6f, maxCycleHz = 1.6f, bobHeight = 0.008f, pitchWave = 1f, rollWave = 1.5f, bounce = true };
    public StateMotion run = new StateMotion { pitch = 3f, stepsPerMetre = 0.4f, maxCycleHz = 2.2f, bobHeight = 0.02f, pitchWave = 3f, rollWave = 1f, bounce = true };
    public StateMotion flee = new StateMotion { pitch = 6f, stepsPerMetre = 0.45f, maxCycleHz = 2.5f, bobHeight = 0.025f, pitchWave = 4f, rollWave = 1f, bounce = true };
    public StateMotion stalk = new StateMotion { height = -0.04f, pitch = 3f, stepsPerMetre = 0.8f, maxCycleHz = 1.2f, bobHeight = 0.004f, pitchWave = 0.5f, rollWave = 1f, bounce = true };
    public StateMotion eat = new StateMotion { pitch = 18f, cycleHz = 1f, pitchWave = 4f };
    public StateMotion drink = new StateMotion { height = -0.03f, pitch = 28f, cycleHz = 0.8f, pitchWave = 3f };
    public StateMotion alert = new StateMotion { height = 0.04f, pitch = -8f, cycleHz = 2.5f, rollWave = 1f };

    [Header("Blending")]
    [Tooltip("Seconds to cross-fade from one state to the next.")]
    [Min(0f)] public float blendSeconds = 0.2f;

    [Header("Turn lean and start/stop settle (springs)")]
    [Tooltip("Degrees of lean per degree/second of turning.")] public float turnLeanPerDegPerSec = 0.08f;
    [Min(0f)] public float maxTurnLean = 12f;
    [Tooltip("Degrees of pitch per m/s² of braking (+) or speeding up (-).")] public float pitchPerAccel = 2f;
    [Min(0f)] public float maxAccelPitch = 8f;
    [Tooltip("How fast the lean/settle springs react.")] [Min(0f)] public float springHz = 3f;
    [Tooltip("Below 1 the springs overshoot and wobble back.")] [Min(0f)] public float springDamping = 0.35f;

    [Header("Death")]
    [Min(0.01f)] public float deathSeconds = 0.7f;
    [Tooltip("Degrees it tips over onto its side.")] public float deathRoll = 80f;
    [Tooltip("Metres it sinks into the ground after tipping over.")] public float deathSink = 0.25f;
    [Min(0.01f)] public float deathSinkSeconds = 1.5f;

    [Header("Spawn")]
    [Tooltip("Seconds of the pop-in when the model appears (0 = off).")]
    [Min(0f)] public float spawnSeconds = 0.35f;

    /// <summary>The looping motion for <paramref name="state"/>. Die has no loop (see death settings).</summary>
    public StateMotion Get(MotionState state) => state switch
    {
        MotionState.Walk => walk,
        MotionState.Run => run,
        MotionState.Flee => flee,
        MotionState.Stalk => stalk,
        MotionState.Eat => eat,
        MotionState.Drink => drink,
        MotionState.Alert => alert,
        _ => idle,
    };
}
// [AI-GENERATED END]
