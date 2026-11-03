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
    [Tooltip("How far each leg swings forward/back from the hip, in degrees (0 = legs still). Needs a readable mesh.")]
    public float legSwing;
    [Tooltip("Fixed front-leg angle in this state, degrees (+ = forward), e.g. bracing to eat.")]
    public float frontLegAngle;
    [Tooltip("Fixed back-leg angle in this state, degrees (+ = forward).")]
    public float backLegAngle;
    [Tooltip("Head bend in degrees (+ = head down, e.g. grazing; - = head up).")]
    public float headPitch;
    [Tooltip("Head nod amplitude in degrees, on the state's cycle.")]
    public float headPitchWave;
    [Tooltip("Head looks left/right this far (degrees) on the state's cycle, e.g. looking around while idle.")]
    public float headYawWave;
    [Tooltip("Tail wag amplitude in degrees (0 = still).")]
    public float tailWag;

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
    public StateMotion idle = new StateMotion { cycleHz = 0.35f, pitchWave = 1f, scalePulse = 0.015f, headPitchWave = 3f, headYawWave = 15f, tailWag = 8f };
    public StateMotion walk = new StateMotion { stepsPerMetre = 0.6f, maxCycleHz = 1.6f, bobHeight = 0.008f, pitchWave = 1f, rollWave = 1.5f, bounce = true, legSwing = 22f, headPitchWave = 3f, tailWag = 6f };
    public StateMotion run = new StateMotion { pitch = 3f, stepsPerMetre = 0.4f, maxCycleHz = 2.2f, bobHeight = 0.02f, pitchWave = 3f, rollWave = 1f, bounce = true, legSwing = 35f, headPitch = -5f, tailWag = 3f };
    public StateMotion flee = new StateMotion { pitch = 6f, stepsPerMetre = 0.45f, maxCycleHz = 2.5f, bobHeight = 0.025f, pitchWave = 4f, rollWave = 1f, bounce = true, legSwing = 40f, headPitch = -10f };
    public StateMotion stalk = new StateMotion { height = -0.04f, pitch = 3f, stepsPerMetre = 0.8f, maxCycleHz = 1.2f, bobHeight = 0.004f, pitchWave = 0.5f, rollWave = 1f, bounce = true, legSwing = 14f, headPitch = 10f, tailWag = 4f };
    public StateMotion eat = new StateMotion { pitch = 8f, cycleHz = 1f, pitchWave = 3f, frontLegAngle = 12f, backLegAngle = -6f, headPitch = 30f, headPitchWave = 10f, tailWag = 10f };
    public StateMotion drink = new StateMotion { height = -0.03f, pitch = 12f, cycleHz = 0.8f, pitchWave = 3f, frontLegAngle = 18f, backLegAngle = -8f, headPitch = 40f, headPitchWave = 4f, tailWag = 5f };
    public StateMotion alert = new StateMotion { height = 0.04f, pitch = -3f, cycleHz = 2.5f, rollWave = 1f, frontLegAngle = -6f, backLegAngle = 4f, headPitch = -20f };
    [Tooltip("Crossing water: sunk to the shoulders, nose up, quick short paddling strokes.")]
    public StateMotion swim = new StateMotion { height = -0.35f, pitch = -8f, stepsPerMetre = 0.9f, maxCycleHz = 1.8f, bobHeight = 0.03f, rollWave = 3f, legSwing = 30f, headPitch = -15f, tailWag = 2f };

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

    [Header("Legs (static meshes: legs found automatically)")]
    [Tooltip("Swing the legs from the hips while moving.")] public bool animateLegs = true;
    [Tooltip("Legs are the lowest part of the model, as a fraction of its height.")]
    [Range(0.1f, 0.8f)] public float legHeightFraction = 0.4f;
    [Tooltip("Part of the leg (fraction of model height) below the hip that blends from body to leg, so the mesh doesn't tear.")]
    [Range(0.01f, 0.5f)] public float legBlendFraction = 0.12f;
    [Tooltip("How quickly the leg swing fades in and out when the state changes (per second).")]
    [Min(0f)] public float legSwingResponse = 8f;
    [Tooltip("Tick if the model's head is at -Z (it walks backwards otherwise).")]
    public bool modelFacesBackward;

    [Header("Head and tail (found automatically: front and back ends above the legs)")]
    public bool animateHead = true;
    [Tooltip("Head and neck = the front part of the body, as a fraction of its length.")]
    [Range(0.05f, 0.5f)] public float headLengthFraction = 0.25f;
    public bool animateTail = true;
    [Tooltip("Tail = the back part of the body, as a fraction of its length.")]
    [Range(0.02f, 0.4f)] public float tailLengthFraction = 0.1f;
    [Tooltip("Fraction of body length over which body blends into head/tail, so the mesh doesn't tear.")]
    [Range(0.01f, 0.3f)] public float headTailBlendFraction = 0.08f;
    [Tooltip("Most the head turns towards something it looks at (degrees).")]
    [Min(0f)] public float maxHeadYaw = 45f;
    [Tooltip("Tail wags per second.")]
    [Min(0f)] public float tailHz = 2.5f;

    [Header("Death (child-friendly: lies down, pauses, then shrinks away)")]
    [Tooltip("Seconds to lie down.")] [Min(0.01f)] public float deathSeconds = 1f;
    [Tooltip("Degrees it rolls onto its side while lying down.")] public float deathRoll = 70f;
    [Tooltip("Legs relax slightly as it lies down: front forward, back back (degrees).")]
    public float deathLegSpread = 8f;
    [Tooltip("Head rests down (degrees).")] public float deathHeadPitch = 15f;
    [Tooltip("Seconds it stays lying down before disappearing, so children notice what happened.")]
    [Min(0f)] public float deathPauseSeconds = 1f;
    [Tooltip("Seconds to shrink away (0 = stays).")] [Min(0f)] public float deathShrinkSeconds = 0.5f;
    [Tooltip("Optional effect spawned when it starts to shrink away (e.g. a sparkle or leaf puff prefab).")]
    public GameObject deathEffect;
    [Tooltip("Metres it sinks into the ground after lying down (0 = none).")] public float deathSink;
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
        MotionState.Swim => swim,
        _ => idle,
    };
}
// [AI-GENERATED END]
