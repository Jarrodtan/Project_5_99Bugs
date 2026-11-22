// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5): rewritten as the only vision for animals.
//
// What one animal can see of the predators and prey around it. AnimalBrain calls Scan() at each decision
// (simulation time, so it keeps up at x8 and stops while paused) and reads NearestPredator / NearestPrey.
// Nothing else looks for other animals, so every vision setting lives here.
//
// Another animal is seen when all of these are true:
//   - it is a predator of this animal, or prey for it (herd mates are ignored),
//   - it is inside the vision cone and within visionRange (a predator's camouflage stretches or shrinks that),
//   - no hill or solid object is between the eyes and it,
//   - and, if it is a stalking predator, it is close enough to spot.
// Closer than senseAnywayDistance it is heard or smelled instead, with no cone or line of sight needed.

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AnimalBrain))]
public class AnimalPerception : MonoBehaviour
{
    [Header("Vision")]
    [Tooltip("How far this animal sees, in metres. A predator's camouflage changes how far away it can be seen " +
             "(less on snow, more on grass; see the Camouflage settings on AnimalBrain).")]
    [Min(0f)]
    public float visionRange = 20f;

    [Tooltip("Width of the field of view in degrees (360 = all round).")]
    [Range(0f, 360f)]
    public float visionAngle = 120f;

    [Header("Hearing / smell")]
    [Tooltip("Closer than this, another animal is noticed without seeing it (hearing, smell), even behind a hill. " +
             "0 = sight only.")]
    [Min(0f)]
    public float senseAnywayDistance = 10f;

    [Header("Stalking predators")]
    [Tooltip("A crouching, stalking predator is only spotted this close (scaled by its camouflage).")]
    [Min(0f)]
    public float spotStalkerDistance = 9f;

    [Tooltip("While eating or drinking (head down), a stalker is spotted at only this fraction of that distance.")]
    [Range(0.1f, 1f)]
    public float grazingAlertness = 0.6f;

    [Header("Line of sight")]
    [Tooltip("Height of the eyes above the animal's root.")]
    public float eyeHeight = 1.5f;

    [Tooltip("Height of the point looked at on the other animal.")]
    public float targetHeight = 1.2f;

    [Tooltip("The view is blocked when the ground rises at least this far above the sight line.")]
    [Min(0f)]
    public float groundClearance = 0.25f;

    [Tooltip("Ground this close to either end of the sight line is ignored (the ground right under the feet).")]
    [Min(0f)]
    public float ignoreNearEnds = 1f;

    [Header("Debug")]
    public bool showVisionGizmo = true;

    [Tooltip("Scene view lines at each scan: green = seen, red = view blocked (the line stops where).")]
    public bool showDetectionLines = true;

    struct Candidate
    {
        public AnimalBrain brain;
        public float distance;
        public bool isPredator;
        public bool isPrey;
    }

    static readonly List<Terrain> terrains = new List<Terrain>();
    static readonly RaycastHit[] hitBuffer = new RaycastHit[32];
    static readonly System.Comparison<Candidate> nearestFirst = (a, b) => a.distance.CompareTo(b.distance);

    readonly List<Candidate> candidates = new List<Candidate>();
    readonly List<AnimalBrain> visibleAnimals = new List<AnimalBrain>();
    AnimalBrain ownBrain;

    /// <summary>The predator and prey found at the last scan (at most one of each).</summary>
    public IReadOnlyList<AnimalBrain> VisibleAnimals => visibleAnimals;

    /// <summary>Nearest predator this animal noticed at the last scan, or null.</summary>
    public AnimalBrain NearestPredator { get; private set; }

    /// <summary>Nearest prey this animal noticed at the last scan, or null.</summary>
    public AnimalBrain NearestPrey { get; private set; }

    Vector3 Eye => transform.position + Vector3.up * eyeHeight;

    void Awake() => ownBrain = GetComponent<AnimalBrain>();

    /// <summary>Looks around once and updates NearestPredator / NearestPrey. Called by AnimalBrain.</summary>
    public void Scan()
    {
        visibleAnimals.Clear();
        NearestPredator = null;
        NearestPrey = null;

        if (ownBrain == null) ownBrain = GetComponent<AnimalBrain>();
        if (ownBrain == null || ownBrain.species_data == null)
            return;

        // Predators and prey that could be in range. A predator standing out on grass can be seen
        // beyond visionRange, so look out to the largest camouflage value.
        Vector3 pos = transform.position;
        float reach = Mathf.Max(visionRange * AnimalBrain.MaxVisibility, senseAnywayDistance);
        float reachSqr = reach * reach;

        candidates.Clear();
        foreach (var other in AnimalBrain.All)
        {
            if (other == null || other == ownBrain || other.IsCaught || other.species_data == null)
                continue;

            bool isPredator = other.species_data.HasRelationship(RelationshipType.Eats, ownBrain.species_data);
            bool isPrey = ownBrain.species_data.HasRelationship(RelationshipType.Eats, other.species_data);
            // [AI-GENERATED] Claude Code (Claude Opus 5.5): Guided focal animal protection (SCRUM-381): Milo is not
            // picked as prey while another Blue Sheep is alive, so predators never start a hunt on him.
            if (isPrey && !FamilyManager.CanBeHunted(other)) isPrey = false;
            if (!isPredator && !isPrey)
                continue;

            Vector3 flat = other.transform.position - pos;
            flat.y = 0f;
            if (flat.sqrMagnitude > reachSqr)
                continue;

            candidates.Add(new Candidate
            {
                brain = other,
                distance = flat.magnitude,
                isPredator = isPredator,
                isPrey = isPrey
            });
        }

        if (candidates.Count == 0)
            return;

        // Nearest first, so the first one noticed of each kind is the nearest, and the rest need no checks.
        Terrain.GetActiveTerrains(terrains);
        candidates.Sort(nearestFirst);

        foreach (var c in candidates)
        {
            bool needPredator = c.isPredator && NearestPredator == null;
            bool needPrey = c.isPrey && NearestPrey == null;
            if (!needPredator && !needPrey)
                continue;

            if (!Notices(c.brain, c.distance, c.isPredator))
                continue;

            visibleAnimals.Add(c.brain);
            if (needPredator) NearestPredator = c.brain;
            if (needPrey) NearestPrey = c.brain;
        }
    }

    // Can this animal tell that `other` is there right now?
    bool Notices(AnimalBrain other, float flatDistance, bool otherIsPredator)
    {
        // Camouflage only matters for a predator being spotted by its prey.
        float visibility = otherIsPredator ? other.Visibility : 1f;

        // A predator creeping low is only noticed up close, and even closer by an animal with its head down.
        if (other.IsStalking)
        {
            float spot = spotStalkerDistance * visibility * (ownBrain.IsHeadDown ? grazingAlertness : 1f);
            if (flatDistance > spot)
                return false;
        }

        // Very close by, it is heard or smelled: no cone or line of sight needed.
        if (flatDistance <= senseAnywayDistance)
            return true;

        Vector3 eye = Eye;
        Vector3 target = other.transform.position + Vector3.up * targetHeight;

        if (!IsInsideVisionCone(eye, target, visionRange * visibility))
            return false;

        return HasLineOfSight(eye, target);
    }

    // range: how far this target can be seen (vision range x its camouflage).
    bool IsInsideVisionCone(Vector3 eye, Vector3 target, float range)
    {
        Vector3 direction = target - eye;
        float distance = direction.magnitude;

        if (distance > range || distance <= 0.001f)
            return false;

        return Vector3.Angle(transform.forward, direction) <= visionAngle * 0.5f;
    }

    bool HasLineOfSight(Vector3 eye, Vector3 target)
    {
        // Hills: checked against the terrain's height map, not with a raycast. Physics.RaycastAll returns only
        // one hit per collider, so when the single terrain hit was a small rise in front of the eyes (and
        // ignored as a bump), the whole hill behind it was missed and animals saw straight through it.
        if (GroundBlocks(eye, target, out Vector3 blockedAt))
        {
            DrawLine(eye, blockedAt, Color.red);
            return false;
        }

        // Anything else solid in the way (rocks, buildings). Other animals don't hide each other, so a sheep at
        // the back of the herd still sees a leopard past the others.
        Vector3 direction = target - eye;
        float distance = direction.magnitude;
        if (distance > 0.001f)
        {
            int hits = Physics.RaycastNonAlloc(eye, direction / distance, hitBuffer, distance, ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits; i++)
            {
                Collider col = hitBuffer[i].collider;
                if (col is TerrainCollider) continue;                       // ground was checked above
                if (col.GetComponentInParent<AnimalBrain>() != null) continue;

                DrawLine(eye, hitBuffer[i].point, Color.red);
                return false;
            }
        }

        DrawLine(eye, target, Color.green);
        return true;
    }

    // True when the ground rises above the line between the two points anywhere along it
    // (except right at either end), sampling every terrain's heights about once per height-map cell.
    bool GroundBlocks(Vector3 from, Vector3 to, out Vector3 blockedAt)
    {
        blockedAt = default;

        Vector3 line = to - from;
        float flatLength = new Vector2(line.x, line.z).magnitude;
        if (flatLength <= ignoreNearEnds * 2f)
            return false;

        foreach (var terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.GetPosition();
            Vector3 size = data.size;
            Vector3 cell = data.heightmapScale;

            float step = Mathf.Max(0.5f, Mathf.Min(cell.x, cell.z));
            int samples = Mathf.CeilToInt(flatLength / step);

            for (int i = 1; i < samples; i++)
            {
                float t = i / (float)samples;
                float along = flatLength * t;
                if (along < ignoreNearEnds || along > flatLength - ignoreNearEnds)
                    continue;

                Vector3 p = from + line * t;
                float localX = p.x - origin.x;
                float localZ = p.z - origin.z;
                if (localX < 0f || localZ < 0f || localX > size.x || localZ > size.z)
                    continue;   // this part of the line isn't over this terrain

                float ground = origin.y + terrain.SampleHeight(p);
                if (ground - p.y >= groundClearance)
                {
                    blockedAt = new Vector3(p.x, ground, p.z);
                    return true;
                }
            }
        }

        return false;
    }

    void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        if (showDetectionLines)
            Debug.DrawLine(from, to, color, 0.25f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showVisionGizmo)
            return;

        Vector3 eyePosition = Eye;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 leftBoundary = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary * visionRange);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary * visionRange);

        // Horizontal edge of the vision cone.
        Vector3 previousPoint = eyePosition + leftBoundary * visionRange;
        const int segments = 30;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-visionAngle * 0.5f, visionAngle * 0.5f, i / (float)segments);
            Vector3 nextPoint = eyePosition + Quaternion.Euler(0f, angle, 0f) * transform.forward * visionRange;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        if (!showDetectionLines || !Application.isPlaying)
            return;

        // Yellow = the prey it found, red = the predator it found.
        foreach (AnimalBrain animal in visibleAnimals)
        {
            if (animal == null)
                continue;

            Gizmos.color = animal == NearestPredator ? Color.red : Color.yellow;
            Gizmos.DrawLine(eyePosition, animal.transform.position + Vector3.up * targetHeight);
        }
    }
}
