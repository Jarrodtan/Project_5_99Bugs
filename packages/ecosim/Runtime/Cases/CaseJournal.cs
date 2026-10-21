// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Where unlocked case ids are saved. Swappable so tests don't touch PlayerPrefs.</summary>
public interface ICaseJournalStore
{
    string Load();
    void Save(string data);
}

/// <summary>Saves the journal on the device with PlayerPrefs (persists between app launches).</summary>
public class PlayerPrefsCaseJournalStore : ICaseJournalStore
{
    readonly string prefsKey;
    public PlayerPrefsCaseJournalStore(string prefsKey = "EcoBalance.CaseJournal") { this.prefsKey = prefsKey; }
    public string Load() => PlayerPrefs.GetString(prefsKey, "");
    public void Save(string data) { PlayerPrefs.SetString(prefsKey, data); PlayerPrefs.Save(); }
}

/// <summary>In-memory store for tests and editor previews.</summary>
public class MemoryCaseJournalStore : ICaseJournalStore
{
    public string Data = "";
    public string Load() => Data;
    public void Save(string data) => Data = data;
}

/// <summary>
/// The player's Field Journal progress: which investigation cases are unlocked. Unlocks are permanent
/// (kept across runs and app launches) and saved immediately.
/// </summary>
public class CaseJournal
{
    readonly ICaseJournalStore store;
    readonly HashSet<string> unlocked = new();

    public CaseJournal(ICaseJournalStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        foreach (var id in (store.Load() ?? "").Split('\n'))
            if (!string.IsNullOrWhiteSpace(id)) unlocked.Add(id.Trim());
    }

    public bool IsUnlocked(string caseKey) => caseKey != null && unlocked.Contains(caseKey);
    public int UnlockedCount => unlocked.Count;
    public IEnumerable<string> UnlockedKeys => unlocked;

    /// <summary>Unlocks and saves. Returns false if it was already unlocked.</summary>
    public bool Unlock(string caseKey)
    {
        if (string.IsNullOrWhiteSpace(caseKey) || !unlocked.Add(caseKey)) return false;
        store.Save(string.Join("\n", unlocked));
        return true;
    }

    /// <summary>Clears all progress (e.g. a "reset journal" option).</summary>
    public void ResetAll()
    {
        unlocked.Clear();
        store.Save("");
    }
}
// [AI-GENERATED END]
