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
/// 3. a pop-in when the object appears.
/// </summary>
[DisallowMultipleComponent]
public class ProcAnimator : MonoBehaviour
{
    public const string PivotName = "ProcAnimPivot";

    [Tooltip("Motion settings. Leave empty to use the built-in defaults.")]
    public MotionProfile profile;

    readonly MotionStateMachine machine = new MotionStateMachine();
    SpringFloat lean, accelPitch;
    Transform pivot;
    Vector3 basePosition, baseScale;
    Quaternion baseRotation;
    Vector3 lastPosition;
    float lastYaw, lastSpeed, enabledTime;

    public MotionState State => machine.Current;
    /// <summary>Ground speed (m/s) measured last frame.</summary>
    public float Speed { get; private set; }
    /// <summary>The final pose applied last frame (for debugging and tests).</summary>
    public MotionPose LastPose { get; private set; } = MotionPose.Identity;
    public Transform Pivot => pivot;

    public void SetState(MotionState state) => machine.SetState(state);

    void Awake()
    {
        if (profile == null) profile = ScriptableObject.CreateInstance<MotionProfile>();
        pivot = EnsurePivot(transform);
        basePosition = pivot.localPosition;
        baseRotation = pivot.localRotation;
        baseScale = pivot.localScale;
    }

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
