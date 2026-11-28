// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guided Mode's three daily missions and their achievements (research doc, "Badges and Achievement"):
///   Day 1  Find what you need      -> Resource Explorer   (resource dependency)
///   Day 2  Stay Safe, Stay Strong  -> Survival Guardian   (predator–prey interactions)
///   Day 3  Keep the Balance        -> Ecosystem Guardian  (ecosystem balance)
/// Each mission is a short list of steps; when every step is done its achievement unlocks for good (kept across
/// runs, Replay and Restart: doc, Reset &amp; Replay Rules). Step ids live in the run's answered events, so
/// Save &amp; Exit keeps them. This class holds the rules and the saved unlocks; GuidedMissions.cs runs them.
/// </summary>
public static class GuidedAchievements
{
    public enum Id { ResourceExplorer, SurvivalGuardian, EcosystemGuardian }

    public sealed class Step
    {
        public string id, text;
        public Step(string id, string text) { this.id = id; this.text = text; }
    }

    public sealed class Mission
    {
        public Id id;
        public int day;              // the day the mission starts
        public string name;          // mission title
        public string badge;         // achievement title
        public string unlockText;    // what the player did (shown on the badge card)
        public string objective;     // learning objective from the doc
        public Step[] steps;
    }

    // Step ids. "Dn" = Day n has ended (the clock); the others are done by the player.
    public const string InspectStep = "M1.inspect", ResourceDecision = "M1", SafetyDecision = "M2", BalanceQuestion = "M3.quiz";
    public static string DayDone(int day) => "D" + day;
    /// <summary>This run already showed the mission's "complete" card.</summary>
    public static string Celebrated(Id id) => "M" + ((int)id + 1) + ".done";

    public static readonly Mission[] All =
    {
        new Mission {
            id = Id.ResourceExplorer, day = 1, name = "Find What You Need", badge = "Resource Explorer",
            unlockText = "You found out how food and water keep animals alive.",
            objective = "Resource dependency",
            steps = new[] {
                new Step(InspectStep, "Tap an animal to check its food and water"),
                new Step(ResourceDecision, "Choose where the family finds food or water"),
                new Step(DayDone(1), "Watch the family until the end of Day 1"),
            } },
        new Mission {
            id = Id.SurvivalGuardian, day = 2, name = "Stay Safe, Stay Strong", badge = "Survival Guardian",
            unlockText = "You made a survival choice and watched what happened next.",
            objective = "Predator–prey interactions",
            steps = new[] {
                new Step(SafetyDecision, "Decide what to do about the hunt"),
                new Step(DayDone(2), "Watch what happens until the end of Day 2"),
            } },
        new Mission {
            id = Id.EcosystemGuardian, day = 3, name = "Keep the Balance", badge = "Ecosystem Guardian",
            unlockText = "You worked out how one species affects another.",
            objective = "Ecosystem balance",
            steps = new[] {
                new Step(DayDone(3), "Watch the grass, the herd and the hunters until the end of Day 3"),
                new Step(BalanceQuestion, "Answer: how does one species affect another?"),
            } },
    };

    public static int Count => All.Length;
    public static Mission Get(Id id) => All[(int)id];

    /// <summary>Medal colours: green for resources, orange for survival, blue for balance.</summary>
    public static Color Colour(Id id) => id switch
    {
        Id.ResourceExplorer => new Color32(0x3F, 0xB5, 0x6A, 0xFF),
        Id.SurvivalGuardian => new Color32(0xF2, 0x8C, 0x28, 0xFF),
        _ => new Color32(0x3D, 0x8B, 0xE0, 0xFF),
    };

    /// <summary>Medal number (the font always has digits).</summary>
    public static string Symbol(Id id) => ((int)id + 1).ToString();

    // ---------- Mission rules (no scene needed) ----------

    public static bool IsComplete(Mission m, ICollection<string> done)
    {
        foreach (var s in m.steps) if (!done.Contains(s.id)) return false;
        return true;
    }

    /// <summary>
    /// The mission the tracker shows: the first one that has started and is not complete (null = none).
    /// <paramref name="blocked"/> skips a mission that can't be finished in this run any more (e.g. no Snow
    /// Leopards left for Mission 2), so the tracker never asks for something impossible.
    /// </summary>
    public static Mission Current(int day, ICollection<string> done, System.Func<Mission, bool> blocked = null)
    {
        foreach (var m in All)
            if (day >= m.day && !IsComplete(m, done) && (blocked == null || !blocked(m))) return m;
        return null;
    }

    /// <summary>The Day 3 question is asked once Day 3 has ended.</summary>
    public static bool QuestionReady(ICollection<string> done) => done.Contains(DayDone(3)) && !done.Contains(BalanceQuestion);

    // ---------- Saved unlocks (separate from the run, like the Ending Gallery) ----------

    public const string Key = "EcoBalance.GuidedAchievements";
    static int Mask => PlayerPrefs.GetInt(Key, 0);

    public static bool IsUnlocked(Id id) => (Mask & (1 << (int)id)) != 0;

    /// <summary>Unlocks the achievement. True when it was not unlocked before.</summary>
    public static bool Unlock(Id id)
    {
        int mask = Mask, bit = 1 << (int)id;
        if ((mask & bit) != 0) return false;
        PlayerPrefs.SetInt(Key, mask | bit);
        PlayerPrefs.Save();
        return true;
    }

    public static int UnlockedCount
    {
        get { int n = 0; foreach (var m in All) if (IsUnlocked(m.id)) n++; return n; }
    }

    /// <summary>"2 / 3 Badges Earned".</summary>
    public static string Progress() => $"{UnlockedCount} / {Count} Badges Earned";
}
