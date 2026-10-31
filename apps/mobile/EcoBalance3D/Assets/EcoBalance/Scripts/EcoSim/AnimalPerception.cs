using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AnimalPerception : MonoBehaviour
{
    [Header("Vision Settings")]
    [Min(0f)]
    public float visionRange = 20f;

    [Range(0f, 360f)]
    public float visionAngle = 120f;

    [Tooltip("How often the animal scans for other animals, in seconds.")]
    [Min(0.05f)]
    public float scanInterval = 0.2f;

    [Header("Eye Position")]
    [Tooltip("Height of the animal's eyes above its root position.")]
    public float eyeHeight = 1.5f;

    [Tooltip("Height of the target point on another animal.")]
    public float targetHeight = 1.2f;

    [Header("Debug")]
    public bool showVisionGizmo = true;
    public bool showDetectionLines = true;

    private float scanTimer;

    private readonly List<AnimalBrain> visibleAnimals =
        new List<AnimalBrain>();

    private readonly HashSet<AnimalBrain> checkedAnimals =
        new HashSet<AnimalBrain>();

    public IReadOnlyList<AnimalBrain> VisibleAnimals => visibleAnimals;

    public AnimalBrain NearestPredator { get; private set; }
    public AnimalBrain NearestPrey { get; private set; }

    private void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            ScanForAnimals();
            scanTimer = scanInterval;
        }
    }

    private void ScanForAnimals()
    {
        visibleAnimals.Clear();
        checkedAnimals.Clear();

        NearestPredator = null;
        NearestPrey = null;

        AnimalBrain ownBrain = GetComponent<AnimalBrain>();

        if (ownBrain == null || ownBrain.species_data == null)
            return;

        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            visionRange,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider col in nearbyColliders)
        {
            if (col == null)
                continue;

            AnimalBrain other = col.GetComponentInParent<AnimalBrain>();

            if (other == null || other == ownBrain)
                continue;

            if (other.IsCaught)
                continue;

            // Avoid processing the same animal more than once
            // if it has multiple colliders.
            if (!checkedAnimals.Add(other))
                continue;

            Vector3 targetPosition = other.transform.position
                                     + Vector3.up * targetHeight;

            // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
            Vector3 flat = other.transform.position - transform.position; flat.y = 0f;
            float flatDistance = flat.magnitude;

            // A predator creeping low (stalking) is only noticed up close, so the stalk-and-pounce works.
            if (other.IsStalking && flatDistance > ownBrain.spotStalkerDistance)
                continue;

            // Very close by, an animal hears or smells the other one: no cone or line-of-sight needed.
            bool sensed = flatDistance <= ownBrain.senseAnywayDistance;
            // [AI-GENERATED END]

            if (!sensed && !IsInsideVisionCone(targetPosition))
                continue;

            if (!sensed && !HasLineOfSight(ownBrain, other, targetPosition))
                continue;

            visibleAnimals.Add(other);

            IdentifyPredatorOrPrey(ownBrain, other);
        }
    }

    private bool IsInsideVisionCone(Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition - (transform.position + Vector3.up * eyeHeight);

        float distance = direction.magnitude;

        if (distance > visionRange || distance <= 0.001f)
            return false;

        float angle = Vector3.Angle(transform.forward, direction);

        return angle <= visionAngle * 0.5f;
    }

    private bool HasLineOfSight(
        AnimalBrain ownBrain,
        AnimalBrain targetBrain,
        Vector3 targetPosition)
    {
        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        Vector3 direction = targetPosition - eyePosition;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            eyePosition,
            direction.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        // Sort so the closest obstruction is checked first.
        System.Array.Sort(hits,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            AnimalBrain hitBrain =
                hit.collider.GetComponentInParent<AnimalBrain>();

            // Ignore this animal's own colliders.
            if (hitBrain == ownBrain)
                continue;

            // We reached the target animal without an obstruction.
            if (hitBrain == targetBrain)
                return true;

            // [AI-GENERATED] Claude Code (Claude Opus 5.5): other animals don't hide a leopard (a sheep at the back
            // of the herd still sees it past the others), and a bump in the ground right at either end isn't a hill.
            if (hitBrain != null)
                continue;
            if (hit.collider is TerrainCollider && (hit.distance < 2f || hit.distance > distance - 2f))
                continue;

            // Something else is blocking the view.
            if (showDetectionLines) Debug.DrawLine(eyePosition, hit.point, Color.red, scanInterval);
            return false;
        }

        return true;
    }

    private void IdentifyPredatorOrPrey(
        AnimalBrain ownBrain,
        AnimalBrain other)
    {
        if (ownBrain.species_data == null ||
            other.species_data == null)
            return;

        // Is the other animal something this species eats?
        bool isPrey = ownBrain.species_data
            .GetSpeciesTargets(RelationshipType.Eats)
            .Contains(other.species_data);

        // Does the other animal eat this species?
        bool isPredator = other.species_data
            .GetSpeciesTargets(RelationshipType.Eats)
            .Contains(ownBrain.species_data);

        float distance = Vector3.Distance(
            transform.position,
            other.transform.position
        );

        if (isPredator &&
            (NearestPredator == null ||
             distance < Vector3.Distance(
                 transform.position,
                 NearestPredator.transform.position)))
        {
            NearestPredator = other;
        }

        if (isPrey &&
            (NearestPrey == null ||
             distance < Vector3.Distance(
                 transform.position,
                 NearestPrey.transform.position)))
        {
            NearestPrey = other;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVisionGizmo)
            return;

        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 leftBoundary =
            Quaternion.Euler(0f, -visionAngle * 0.5f, 0f)
            * transform.forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0f, visionAngle * 0.5f, 0f)
            * transform.forward;

        Gizmos.DrawLine(
            eyePosition,
            eyePosition + leftBoundary * visionRange
        );

        Gizmos.DrawLine(
            eyePosition,
            eyePosition + rightBoundary * visionRange
        );

        // Draw the horizontal boundary of the vision cone.
        Vector3 previousPoint =
            eyePosition + leftBoundary * visionRange;

        int segments = 30;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(
                -visionAngle * 0.5f,
                visionAngle * 0.5f,
                i / (float)segments
            );

            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) * transform.forward;

            Vector3 nextPoint =
                eyePosition + direction * visionRange;

            Gizmos.DrawLine(previousPoint, nextPoint);

            previousPoint = nextPoint;
        }

        if (!showDetectionLines || !Application.isPlaying)
            return;

        foreach (AnimalBrain animal in visibleAnimals)
        {
            if (animal == null)
                continue;

            Gizmos.color = Color.green;

            if (animal == NearestPredator)
                Gizmos.color = Color.red;
            else if (animal == NearestPrey)
                Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                eyePosition,
                animal.transform.position + Vector3.up * targetHeight
            );
        }
    }
}
