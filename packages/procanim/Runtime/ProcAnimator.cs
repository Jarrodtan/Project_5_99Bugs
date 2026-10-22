// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Animates a static (unrigged) model procedurally, instead of Unity's Animator.
/// On Awake it moves the object's children under a new "ProcAnimPivot" child and animates only
/// that pivot, so whatever moves the root (NavMeshAgent, physics, scripts) is never fought.
/// Speed and turning are measured from how the root actually moved; the state comes from
/// <see cref="SetState"/>. Layers, every frame:
/// 1. the state machine's looping motion (cross-faded between states),
/// 2. springs: lean into turns, nose dip/lift when braking/speeding up,
/// 3. a pop-in when the object appears,
/// 4. body parts found automatically in the mesh (<see cref="BodyDeformer"/>): legs trot from the
///    hips, the head nods/grazes and turns to look at <see cref="SetLookTarget"/>, the tail wags,
/// 5. a child-friendly death: lies down, pauses, then shrinks away (optional effect).
/// </summary>
[DisallowMultipleComponent]
public class ProcAnimator : MonoBehaviour
{
    public const string PivotName = "ProcAnimPivot";

    [Tooltip("Motion settings. Leave empty to use the built-in defaults.")]
    public MotionProfile profile;

    readonly MotionStateMachine machine = new MotionStateMachine();
    SpringFloat lean, accelPitch, headYaw;
    Transform pivot;
    Vector3 basePosition, baseScale;
    Quaternion baseRotation;
    Vector3 lastPosition;
    float lastYaw, lastSpeed, enabledTime;

    BodyDeformer body;
    float gaitPhase, tailPhase;
    float legSwing, frontLegAngle, backLegAngle, headPitch;
    Vector3? lookTarget;
    bool deathEffectSpawned;

    public MotionState State => machine.Current;
    /// <summary>Ground speed (m/s) measured last frame.</summary>
    public float Speed { get; private set; }
    /// <summary>The final pose applied last frame (for debugging and tests).</summary>
    public MotionPose LastPose { get; private set; } = MotionPose.Identity;
    /// <summary>The leg/head/tail angles applied last frame.</summary>
    public BodyPartAngles LastBodyAngles { get; private set; }
    public Transform Pivot => pivot;
    /// <summary>True when legs were found in the mesh and are being animated.</summary>
    public bool HasLegs => body != null && body.Valid;
    public bool HasHead => HasLegs && body.HeadPart != null && body.HeadPart.Valid;
    public bool HasTail => HasLegs && body.TailPart != null && body.TailPart.Valid;

    public void SetState(MotionState state)
    {
        if (state == MotionState.Die && machine.Current != MotionState.Die) deathEffectSpawned = false;
        machine.SetState(state);
    }

    /// <summary>Something for the head to turn towards (world position), or null to look ahead.</summary>
    public void SetLookTarget(Vector3? worldPosition) => lookTarget = worldPosition;

    void Awake()
    {
        if (profile == null) profile = ScriptableObject.CreateInstance<MotionProfile>();
        pivot = EnsurePivot(transform);
        basePosition = pivot.localPosition;
        baseRotation = pivot.localRotation;
        baseScale = pivot.localScale;
        if (profile.animateLegs)
        {
            body = BodyDeformer.Build(transform, pivot, new BodyDeformer.Settings
            {
                legHeightFraction = profile.legHeightFraction,
                legBlendFraction = profile.legBlendFraction,
                facesBackward = profile.modelFacesBackward,
                head = profile.animateHead,
                tail = profile.animateTail,
                headLengthFraction = profile.headLengthFraction,
                tailLengthFraction = profile.tailLengthFraction,
                headTailBlendFraction = profile.headTailBlendFraction,
            });
        }
    }

    void OnDestroy() => body?.Dispose();

    /// <summary>
    /// Returns <paramref name="root"/>'s pivot, creating it on first use by moving every existing child
    /// under it (world positions kept). Components on the root itself (collider, agent) stay put.
    /// </summary>
    public static Transform EnsurePivot(Transform root)
    {
        var pivot = root.Find(PivotName);
        if (pivot != null) return pivot;
        pivot = new GameObject(PivotName).transform;
        pivot.SetParent(root, false);
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child != pivot) child.SetParent(pivot, true);
        }
        return pivot;
    }

    void OnEnable()
    {
        lastPosition = transform.position;
        lastYaw = transform.eulerAngles.y;
        lastSpeed = 0f;
        enabledTime = Time.time;
        lean.Reset();
        accelPitch.Reset();
        headYaw.Reset();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || pivot == null) return;

        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        Speed = moved.magnitude / dt;
        float yawRate = Mathf.DeltaAngle(lastYaw, transform.eulerAngles.y) / dt;
        float acceleration = (Speed - lastSpeed) / dt;
        lastPosition = transform.position;
        lastYaw = transform.eulerAngles.y;
        lastSpeed = Speed;

        var pose = machine.Evaluate(profile, Speed, dt);

        bool dying = machine.Current == MotionState.Die;
        lean.Step(dying ? 0f : MotionStateMachine.TurnLeanTarget(profile, yawRate, Speed), profile.springHz, profile.springDamping, dt);
        accelPitch.Step(dying ? 0f : MotionStateMachine.AccelPitchTarget(profile, acceleration), profile.springHz, profile.springDamping, dt);
        pose.Roll += lean.Value;
        pose.Pitch += accelPitch.Value;
        pose.Scale *= MotionStateMachine.SpawnScale(profile, Time.time - enabledTime);

        Apply(pose);
        if (HasLegs) AnimateBody(dt, dying);
        if (dying) SpawnDeathEffectOnce();
    }

    void AnimateBody(float dt, bool dying)
    {
        var motion = profile.Get(machine.Current);
        float swingTarget = motion.legSwing, frontTarget = motion.frontLegAngle, backTarget = motion.backLegAngle;
        float headTarget = motion.headPitch;
        if (dying)
        {
            float lie = MotionStateMachine.DeathLieProgress(profile, machine.TimeInState);
            swingTarget = 0f;
            frontTarget = profile.deathLegSpread * lie;
            backTarget = -profile.deathLegSpread * lie;
            headTarget = profile.deathHeadPitch * lie;
        }

        // Fade swing and poses in/out instead of snapping when the state changes.
        float k = 1f - Mathf.Exp(-profile.legSwingResponse * dt);
        legSwing = Mathf.Lerp(legSwing, swingTarget, k);
        frontLegAngle = Mathf.Lerp(frontLegAngle, frontTarget, k);
        backLegAngle = Mathf.Lerp(backLegAngle, backTarget, k);
        headPitch = Mathf.Lerp(headPitch, headTarget, k);
        gaitPhase = Mathf.Repeat(gaitPhase + motion.CycleRate(Speed) * dt, 1f);
        tailPhase = Mathf.Repeat(tailPhase + profile.tailHz * dt, 1f);

        float cycle = Mathf.Sin(2f * Mathf.PI * gaitPhase);
        float yawTarget = dying ? 0f : LookYaw() + motion.headYawWave * Mathf.Sin(Mathf.PI * gaitPhase * 2f + 1f);
        headYaw.Step(yawTarget, profile.springHz, 0.7f, dt);

        var angles = new BodyPartAngles
        {
            FrontLeft = LegLayout.LegAngle(LegLayout.FrontLeft, gaitPhase, legSwing, frontLegAngle, backLegAngle),
            FrontRight = LegLayout.LegAngle(LegLayout.FrontRight, gaitPhase, legSwing, frontLegAngle, backLegAngle),
            BackLeft = LegLayout.LegAngle(LegLayout.BackLeft, gaitPhase, legSwing, frontLegAngle, backLegAngle),
            BackRight = LegLayout.LegAngle(LegLayout.BackRight, gaitPhase, legSwing, frontLegAngle, backLegAngle),
            HeadPitch = headPitch + (dying ? 0f : motion.headPitchWave * cycle),
            HeadYaw = headYaw.Value,
            TailYaw = dying ? 0f : motion.tailWag * Mathf.Sin(2f * Mathf.PI * tailPhase),
        };
        LastBodyAngles = angles;
        body.Apply(angles);
    }

    /// <summary>Head turn (degrees, clockwise from above) towards the look target, clamped.</summary>
    float LookYaw()
    {
        if (lookTarget == null) return 0f;
        Vector3 local = transform.InverseTransformPoint(lookTarget.Value);
        if (body != null && body.Valid && body.Layout.FacesBackward) local = -local;
        local.y = 0f;
        if (local.sqrMagnitude < 1e-6f) return 0f;
        float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        return Mathf.Clamp(yaw, -profile.maxHeadYaw, profile.maxHeadYaw);
    }

    void SpawnDeathEffectOnce()
    {
        if (deathEffectSpawned || profile.deathEffect == null) return;
        if (machine.TimeInState < MotionStateMachine.DeathShrinkStart(profile)) return;
        deathEffectSpawned = true;
        Instantiate(profile.deathEffect, pivot.position, Quaternion.identity);
    }

    void Apply(MotionPose pose)
    {
        LastPose = pose;
        // Height is in metres; the pivot lives in the root's (possibly scaled) local space.
        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 1e-4f);
        pivot.localPosition = basePosition + Vector3.up * (pose.Height / scaleY);
        pivot.localRotation = baseRotation * Quaternion.Euler(pose.Pitch, 0f, -pose.Roll);
        pivot.localScale = baseScale * pose.Scale;
    }
}
// [AI-GENERATED END]
