// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Free Exploration: after every simulated day, checks the still-locked investigation cases against
/// the population history and unlocks those whose conditions are met. UI listens to
/// <see cref="CaseUnlocked"/> to show the "New case discovered" popup and refresh the Field Journal.
/// Put it next to the EcosystemSimulator and PopulationHistoryRecorder.
/// </summary>
public class CaseDetector : MonoBehaviour
{
    [Tooltip("History to watch. Defaults to one on this GameObject.")]
    public PopulationHistoryRecorder recorder;

    [Tooltip("All investigation cases in the Field Journal.")]
    public List<InvestigationCase> cases = new();

    /// <summary>Raised once per case, the first time it unlocks (ever, not per run).</summary>
    public event System.Action<InvestigationCase> CaseUnlocked;

    CaseJournal journal;
    PopulationHistory watched;

    /// <summary>The saved journal. Uses PlayerPrefs unless <see cref="UseStore"/> was called first.</summary>
    public CaseJournal Journal => journal ??= new CaseJournal(new PlayerPrefsCaseJournalStore());

    /// <summary>Use a different store (tests, editor tools). Call before anything reads <see cref="Journal"/>.</summary>
    public void UseStore(ICaseJournalStore store) => journal = new CaseJournal(store);

    void OnEnable()
    {
        if (recorder == null) recorder = GetComponent<PopulationHistoryRecorder>();
        if (recorder == null)
        {
            Debug.LogWarning($"[{name}] CaseDetector has no PopulationHistoryRecorder to watch.", this);
            return;
        }
        Watch(recorder.History);
    }

    void OnDisable()
    {
        if (watched != null) watched.SampleAdded -= OnSample;
        watched = null;
    }

    /// <summary>Starts checking cases whenever <paramref name="history"/> gets a new day (also used by tests).</summary>
    public void Watch(PopulationHistory history)
    {
        if (watched != null) watched.SampleAdded -= OnSample;
        watched = history;
        if (watched != null) watched.SampleAdded += OnSample;
    }

    void OnSample(PopulationHistory.Sample _) => CheckNow();

    /// <summary>Checks every locked case now. Returns the cases unlocked by this check.</summary>
    public List<InvestigationCase> CheckNow()
    {
        var newlyUnlocked = new List<InvestigationCase>();
        if (watched == null || cases == null) return newlyUnlocked;
        foreach (var c in cases)
        {
            if (c == null || Journal.IsUnlocked(c.Key)) continue;
            if (!CaseEvaluator.IsMet(c, watched)) continue;
            if (!Journal.Unlock(c.Key)) continue;
            newlyUnlocked.Add(c);
            CaseUnlocked?.Invoke(c);
        }
        return newlyUnlocked;
    }

    public bool IsUnlocked(InvestigationCase c) => c != null && Journal.IsUnlocked(c.Key);
}
// [AI-GENERATED END]
