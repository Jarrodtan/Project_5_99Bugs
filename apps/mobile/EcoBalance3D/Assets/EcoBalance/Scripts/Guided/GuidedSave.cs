// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// SCRUM-375 Save &amp; Exit / Resume for the Guided story. One save slot, stored as JSON in PlayerPrefs:
/// day and time of day, chapter and its carry-over snapshots, events already answered, and every living
/// animal (species, position, hunger, thirst). Resume starts the run with the saved population, then puts
/// each animal back where it was. The save is deleted when the story ends or a new story starts.
/// </summary>
public partial class GuidedChallenge
{
    const string SaveKey = "EcoBalance.GuidedSave";

    [Serializable] class AnimalSave { public bool predator; public Vector3 pos; public float yaw, hunger, thirst; }
    [Serializable] class SnapshotSave { public int sheep, leopards; public float grass; }
    [Serializable] class SaveData
    {
        public int day, chapter;
        public float timeOfDay;
        public List<string> resolved = new List<string>();
        public List<SnapshotSave> snapshots = new List<SnapshotSave>();
        public List<AnimalSave> animals = new List<AnimalSave>();
        public int sheep, leopards;
        public int story;   // SCRUM-375: FamilyStory (0 = Milo / Blue Sheep, 1 = Snowy / Snow Leopard)
    }

    static GuidedChallenge instance;
    static SaveData pendingResume;   // set by ScreenFlow before the scene loads

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    /// <summary>Day of the saved story, for the "Continue your story?" box (0 = no save).</summary>
    public static int SavedDay => Load() is SaveData d ? d.day : 0;
    public static FamilyStory SavedStory => Load() is SaveData d ? (FamilyStory)d.story : FamilyStory.BlueSheep;

    public static void DeleteSave() { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); pendingResume = null; }

    /// <summary>Save the running Guided story (Save &amp; Exit). False when there is nothing to save.</summary>
    public static bool SaveNow()
    {
        var g = instance;
        if (g == null || g.finished || g.clock == null) return false;
        var d = new SaveData
        {
            day = g.clock.Day, timeOfDay = g.clock.TimeOfDay01, chapter = CurrentChapter,
            resolved = new List<string>(g.resolved), sheep = Sheep, leopards = Leopards,
            story = (int)FamilyManager.SelectedStory,
        };
        foreach (var s in g.snapshots) d.snapshots.Add(new SnapshotSave { sheep = s.sheep, leopards = s.leopards, grass = s.grass });
        foreach (var a in AnimalBrain.All)
            if (a != null && !a.IsCaught)
                d.animals.Add(new AnimalSave { predator = a.IsPredatorSpecies, pos = a.transform.position,
                                               yaw = a.transform.eulerAngles.y, hunger = a.curr_hunger, thirst = a.curr_thirst });
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Called by ScreenFlow when the player picks Continue: the run starts with the saved population.</summary>
    public static bool PrepareResume(out int sheep, out int leopards)
    {
        pendingResume = Load();
        sheep = pendingResume?.sheep ?? 0;
        leopards = pendingResume?.leopards ?? 0;
        if (pendingResume != null) FamilyManager.SelectedStory = (FamilyStory)pendingResume.story;   // Continue keeps Milo or Snowy
        return pendingResume != null;
    }

    static SaveData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;
        try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); }
        catch (Exception) { return null; }   // old or broken save: start fresh
    }

    /// <summary>Once the animals have spawned: restore day, chapter, events and each animal. Skips the intro.</summary>
    void ApplyResume()
    {
        var d = pendingResume;
        pendingResume = null;
        if (d == null) return;

        if (clock != null) clock.Restore(d.day, d.timeOfDay);
        lastDay = -1;
        foreach (var id in d.resolved) resolved.Add(id);
        CurrentChapter = Mathf.Clamp(d.chapter, 1, ChapterCount);
        snapshots.Clear();
        foreach (var s in d.snapshots) snapshots.Add(new ChapterSnapshot { sheep = s.sheep, leopards = s.leopards, grass = s.grass });

        // Put each saved animal back on one spawned animal of the same species.
        var used = new HashSet<AnimalBrain>();
        foreach (var save in d.animals)
            foreach (var a in AnimalBrain.All)
            {
                if (a == null || a.IsCaught || used.Contains(a) || a.IsPredatorSpecies != save.predator) continue;
                used.Add(a);
                if (a.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh) agent.Warp(save.pos);
                else a.transform.position = save.pos;
                a.transform.rotation = Quaternion.Euler(0f, save.yaw, 0f);
                a.curr_hunger = save.hunger;
                a.curr_thirst = save.thirst;
                break;
            }

        introDone = true;   // the story is already under way
        quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
        Mascot.Hide();
        DramaDirector.Announce($"Welcome back! {Chapters[CurrentChapter - 1].title}, Day {d.day}");
        if (speed != null) speed.Play();
    }
}
