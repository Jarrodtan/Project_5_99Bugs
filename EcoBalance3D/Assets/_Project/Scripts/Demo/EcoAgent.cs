// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight demo animal. Grazers walk to grass and flee predators; predators chase the
/// nearest animal they Eat (from the species relationships). Population numbers still come
/// from <see cref="EcosystemSimulator"/>; this only makes the interactions visible.
/// No NavMesh needed, so it works on any flat island.
/// </summary>
public class EcoAgent : MonoBehaviour
{
    public static readonly List<EcoAgent> All = new List<EcoAgent>();

    public SpeciesDefinition species;
    public bool isPredator;
    [Min(0.1f)] public float moveSpeed = 2f;
    [Min(1f)] public float senseRadius = 6f;
    [Min(1f)] public float arenaRadius = 11f;

    /// <summary>Time this agent was last caught by a predator (used to pick who disappears).</summary>
    public float CaughtTime { get; private set; } = float.NegativeInfinity;

    Vector3 wanderTarget;
    float wanderTimer;
    float restTimer;
    float baseY;

    void Awake()
    {
        baseY = transform.position.y;
        PickWanderTarget();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public void MarkCaught() => CaughtTime = Time.time;

    void Update()
    {
        float dt = Time.deltaTime * EcoDemoController.SimSpeed;
        if (dt <= 0f) return;

        if (restTimer > 0f)
        {
            // Eating: small hop in place.
            restTimer -= dt;
            var p = transform.position;
            p.y = baseY + Mathf.Abs(Mathf.Sin(restTimer * 10f)) * 0.15f;
            transform.position = p;
            return;
        }

        MoveTowards(isPredator ? PredatorGoal() : GrazerGoal(), dt);
    }

    Vector3 GrazerGoal()
    {
        var threat = Nearest(a => a.isPredator && a.species != null && a.species.HasRelationship(RelationshipType.Eats, species), senseRadius * 0.6f);
        if (threat != null)
        {
            Vector3 away = transform.position - threat.transform.position;
            away.y = 0f;
            away.Normalize();
            // Near the edge, bend the escape back towards the middle so prey don't get pinned.
            Vector3 toCenter = new Vector3(-transform.position.x, 0f, -transform.position.z);
            float edge = Mathf.InverseLerp(arenaRadius * 0.6f, arenaRadius, toCenter.magnitude);
            Vector3 dir = Vector3.Lerp(away, toCenter.normalized + Vector3.Cross(Vector3.up, away), edge).normalized;
            return ClampToArena(transform.position + dir * 3f);
        }

        var patch = GrassPatch.Nearest(transform.position, senseRadius);
        if (patch != null)
        {
            if (FlatDistance(patch.transform.position) < 0.6f)
                restTimer = Random.Range(1f, 2f);
            return patch.transform.position;
        }
        return Wander();
    }

    Vector3 PredatorGoal()
    {
        var prey = Nearest(a => species != null && species.HasRelationship(RelationshipType.Eats, a.species), senseRadius);
        if (prey != null)
        {
            if (FlatDistance(prey.transform.position) < 0.7f)
            {
                prey.MarkCaught();
                restTimer = 2f;
            }
            return prey.transform.position;
        }
        return Wander();
    }

    Vector3 Wander()
    {
        wanderTimer -= Time.deltaTime * EcoDemoController.SimSpeed;
        if (wanderTimer <= 0f || FlatDistance(wanderTarget) < 0.5f)
            PickWanderTarget();
        return wanderTarget;
    }

    void PickWanderTarget()
    {
        Vector2 r = Random.insideUnitCircle * arenaRadius;
        wanderTarget = new Vector3(r.x, 0f, r.y);
        wanderTimer = Random.Range(3f, 6f);
    }

    void MoveTowards(Vector3 goal, float dt)
    {
        Vector3 pos = transform.position;
        goal.y = baseY;
        Vector3 next = ClampToArena(Vector3.MoveTowards(new Vector3(pos.x, baseY, pos.z), goal, moveSpeed * dt));
        Vector3 dir = next - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
        transform.position = next;
    }

    Vector3 ClampToArena(Vector3 p)
    {
        Vector2 flat = Vector2.ClampMagnitude(new Vector2(p.x, p.z), arenaRadius);
        return new Vector3(flat.x, baseY, flat.y);
    }

    float FlatDistance(Vector3 other)
    {
        Vector3 d = other - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    EcoAgent Nearest(System.Predicate<EcoAgent> match, float maxDistance)
    {
        EcoAgent best = null;
        float bestDist = maxDistance;
        foreach (var a in All)
        {
            if (a == this || !match(a)) continue;
            float d = FlatDistance(a.transform.position);
            if (d < bestDist) { bestDist = d; best = a; }
        }
        return best;
    }
}
// [AI-GENERATED END]
