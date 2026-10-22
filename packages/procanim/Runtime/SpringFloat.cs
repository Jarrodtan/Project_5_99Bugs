// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// A damped spring that chases a target value (mass-spring-damper, unit mass).
/// <c>frequencyHz</c> sets how fast it reacts; <c>dampingRatio</c> sets the wobble:
/// below 1 it overshoots and settles (bouncy), 1 is critically damped (fastest with no overshoot).
/// Steps are split into small sub-steps so the motion looks the same at 30 or 120 fps.
/// </summary>
[System.Serializable]
public struct SpringFloat
{
    /// <summary>Largest sub-step in seconds; keeps the integration stable at low frame rates.</summary>
    public const float MaxSubStep = 1f / 120f;

    public float Value;
    public float Velocity;

    public void Step(float target, float frequencyHz, float dampingRatio, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        float omega = 2f * Mathf.PI * Mathf.Max(0f, frequencyHz);
        int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / MaxSubStep));
        float h = deltaTime / steps;
        for (int i = 0; i < steps; i++)
        {
            // Semi-implicit Euler: update velocity first, then position (stable for springs).
            float accel = omega * omega * (target - Value) - 2f * dampingRatio * omega * Velocity;
            Velocity += accel * h;
            Value += Velocity * h;
        }
    }

    public void Reset(float value = 0f)
    {
        Value = value;
        Velocity = 0f;
    }
}
// [AI-GENERATED END]
