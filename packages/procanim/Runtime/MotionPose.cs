// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>Which motion an animated object is in. The app decides the state; ProcAnim animates it.</summary>
public enum MotionState
{
    Idle,
    Walk,
    Run,
    Flee,
    Stalk,
    Eat,
    Drink,
    Alert,
    Die,
    Swim    // crossing water: body low in the water, head up, legs paddling
}

/// <summary>
/// The offset ProcAnim applies to a model on top of where gameplay put it: lift in metres,
/// pitch (+ = nose down) and roll (+ = lean right) in degrees, and a uniform scale multiplier.
/// </summary>
public struct MotionPose
{
    public float Height;
    public float Pitch;
    public float Roll;
    public float Scale;

    public static MotionPose Identity => new MotionPose { Scale = 1f };

    public static MotionPose Lerp(MotionPose a, MotionPose b, float t) => new MotionPose
    {
        Height = Mathf.Lerp(a.Height, b.Height, t),
        Pitch = Mathf.Lerp(a.Pitch, b.Pitch, t),
        Roll = Mathf.Lerp(a.Roll, b.Roll, t),
        Scale = Mathf.Lerp(a.Scale, b.Scale, t),
    };
}
// [AI-GENERATED END]
