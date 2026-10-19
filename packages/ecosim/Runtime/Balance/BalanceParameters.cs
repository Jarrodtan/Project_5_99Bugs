// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Tuning values for <see cref="BalanceModel"/>. Defaults are the M2 values from design's
/// "EcoBalance - Ecosystem Research & Simulation Reference" doc (1 step = 1 simulated day).
/// Values the doc does not specify are marked "assumption".
/// </summary>
[System.Serializable]
public class BalanceParameters
{
    [Header("Run")]
    [Min(1)] public int days = 200;
    public int seed = 1;

    [Header("Starting state")]
    [Min(0)] public int sheepStart = 10;
    [Min(0)] public int leopardStart = 2;
    [Tooltip("Number of grass patches, each 0-100. (assumption)")]
    [Min(1)] public int grassPatches = 10;

    [Header("Fullness (0-100, starts at 100)")]
    public float sheepHungerPerDay = 20f;
    public float leopardHungerPerDay = 12f;
    [Tooltip("Animals look for food at or below this fullness.")]
    public float seekFoodAt = 50f;
    public float sheepMeal = 40f;
    public float leopardMeal = 80f;

    [Header("Health (0-100)")]
    [Tooltip("HP lost per day at 0 fullness.")]
    public float starvationDamagePerDay = 20f;
    [Tooltip("HP regained per day while not starving. (assumption)")]
    public float healthRegenPerDay = 10f;

    [Header("Hunting")]
    [Range(0f, 1f)] public float huntSuccess = 0.7f;
    [Min(1)] public int huntCooldownDays = 1;
    [Tooltip("Chance a hungry leopard finds a sheep = 1 - exp(-sheep / scale). Larger = harder to find " +
             "when sheep are few (stands in for the 10-unit detection range). (assumption)")]
    [Min(0.01f)] public float encounterScale = 10f;

    [Header("Breeding")]
    [Tooltip("Both parents need at least this fullness.")]
    public float breedFullness = 70f;
    [Min(1)] public int sheepBreedEveryDays = 5;
    [Min(1)] public int leopardBreedEveryDays = 12;
    [Min(1)] public int maxPopulationPerSpecies = 200;

    [Header("Grass (per patch)")]
    public float bite = 5f;
    public float regrowPerDay = 5f;
    [Tooltip("A patch can't be grazed below this.")]
    public float minGraze = 5f;

    public BalanceParameters Clone() => (BalanceParameters)MemberwiseClone();
}
// [AI-GENERATED END]
