// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// SCRUM-374 Chapter flow: the 20-day Guided run is one ecological year in four seasonal chapters
/// (Summer, Autumn, Winter, Spring; 5 days each, matching <see cref="SeasonWeather.daysPerSeason"/>).
/// At each new season the run pauses on a chapter card that shows what carried over from the last
/// chapter (sheep, leopards, grass, family losses). The story can end early through
/// <see cref="EndStoryEarly"/>, e.g. when the family's main animal dies (Zhi Jie's FamilyManager).
/// </summary>
public partial class GuidedChallenge
{
    public const int ChapterCount = 4;
    public const int DaysPerChapter = ChallengeDays / ChapterCount;   // 5

    static readonly (string title, string text)[] Chapters =
    {
        ("Chapter 1: Summer", "Grass is growing and the family is together. Help them grow strong before the cold comes."),
        ("Chapter 2: Autumn", "Days are getting shorter. The grass is drying out, so food gets harder to find."),
        ("Chapter 3: Winter", "Snow covers the mountain. Every animal must fight to stay warm and fed."),
        ("Chapter 4: Spring", "The snow melts and new life arrives. Can the family make it through the year?"),
    };

    /// <summary>1-based chapter for the current day.</summary>
    public static int CurrentChapter { get; private set; } = 1;

    /// <summary>Set by <see cref="EndStoryEarly"/>; the run finishes on the next frame.</summary>
    public static string StoryOverReason { get; private set; }

    /// <summary>End the Guided story now (e.g. the main animal died). Safe to call more than once.</summary>
    public static void EndStoryEarly(string reason)
    {
        if (string.IsNullOrEmpty(StoryOverReason)) StoryOverReason = reason;
    }

    // What the last chapter card saw, so the next one can show what changed.
    struct ChapterSnapshot { public int sheep, leopards; public float grass; }
    readonly List<ChapterSnapshot> snapshots = new List<ChapterSnapshot>();

    GameObject chapterPanel;
    TextMeshProUGUI chapterTitle, chapterText;
    bool chapterOpen;

    static int ChapterForDay(int day) => Mathf.Clamp((day - 1) / DaysPerChapter + 1, 1, ChapterCount);

    void ResetChapters()
    {
        CurrentChapter = 1;
        StoryOverReason = null;   // statics survive Play without a domain reload
        snapshots.Clear();
        ResetFires();   // SCRUM-390
        ResetPlanting();
    }

    ChapterSnapshot Snapshot() => new ChapterSnapshot { sheep = Sheep, leopards = Leopards, grass = Vegetation };

    /// <summary>Called from Update once the intro is done. True while a chapter card is open or just opened.</summary>
    bool UpdateChapters(int day)
    {
        if (chapterOpen) return true;
        if (snapshots.Count == 0) snapshots.Add(Snapshot());   // chapter 1 starts when the intro closes
        int ch = ChapterForDay(day);
        if (ch <= CurrentChapter || showing || ScreenFlow.JournalOpen) return false;   // 28 Nov: the chapter card waits for the Field Journal (it paused the run under it)
        CurrentChapter = ch;
        ShowChapter(ch);
        return true;
    }

    void ShowChapter(int ch)
    {
        chapterOpen = true;
        PopupOpen = true;
        if (speed != null) speed.Pause();
        Mascot.Hide();

        var before = snapshots[snapshots.Count - 1];
        var now = Snapshot();
        snapshots.Add(now);

        var (title, text) = Chapters[ch - 1];
        var prev = Chapters[ch - 2].title.Substring(Chapters[ch - 2].title.IndexOf(':') + 2);
        chapterTitle.text = title;
        chapterText.text =
            $"{text}\n\n<b>Since {prev}</b>\n" +
            $"Blue Sheep: {before.sheep} → {now.sheep}{Change(now.sheep - before.sheep)}\n" +
            $"Snow Leopards: {before.leopards} → {now.leopards}{Change(now.leopards - before.leopards)}\n" +
            $"Grass: {before.grass:0}% → {now.grass:0}%" + FireRecap() + PlantRecap();
        chapterPanel.SetActive(true);
    }

    static string Change(int d) => d > 0 ? $"  <color=#2E8B57>(+{d})</color>" : d < 0 ? $"  <color=#C0392B>({d})</color>" : "";

    void CloseChapter()
    {
        chapterPanel.SetActive(false);
        chapterOpen = false;
        PopupOpen = false;
        quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
        if (speed != null) speed.Play();
        DramaDirector.Announce(Chapters[CurrentChapter - 1].title);
    }

    void BuildChapterUI(Transform root)
    {
        chapterPanel = Dim(root, "Chapter");
        var card = Box(chapterPanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f), new Color(0.97f, 0.97f, 0.95f, 1f));
        chapterTitle = Label(card.transform, "", 60f, new Color(0.12f, 0.16f, 0.2f), 60f, 40f, 620f, TextAlignmentOptions.Top);
        chapterText = Label(card.transform, "", 36f, new Color(0.2f, 0.24f, 0.28f), 80f, 150f, 170f, TextAlignmentOptions.Top);
        Button(card.transform, "Continue", new Vector2(0f, 50f), new Color(0.3f, 0.62f, 0.38f), CloseChapter);
        ScreenFlow.StyleLikeShumin(card, null);   // cream card, brown edge, Chewy font, Shu Min's green button
        chapterPanel.SetActive(false);
    }
}
