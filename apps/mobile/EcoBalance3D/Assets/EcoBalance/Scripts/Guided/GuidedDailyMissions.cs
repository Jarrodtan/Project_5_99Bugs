// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the three daily missions (<see cref="GuidedAchievements"/>) inside a Guided story:
///   - a tracker card lists the current mission's steps and ticks them off as the child does them;
///   - the Mission 1 and 2 decisions are ordinary Guided events (Shu Min's event + feedback cards), worded for the
///     chosen story (Miloy's herd or Snowy's family);
///   - Mission 2's badge card shows what happened after the survival choice (catches, populations);
///   - after Day 3 a question asks how one species affects another (a wrong answer gets a hint, never a "fail");
///   - a finished mission pops its medal with confetti and unlocks the achievement for good.
/// Missions never block the story: a step missed on its day can still be done later.
/// </summary>
public partial class GuidedChallenge
{
    readonly Dictionary<string, string> chosenLabels = new Dictionary<string, string>();

    struct DayStart { public int sheep, leopards; public float grass; }
    readonly Dictionary<int, DayStart> dayStarts = new Dictionary<int, DayStart>();
    readonly Dictionary<int, int> catchesByDay = new Dictionary<int, int>();

    /// <summary>Badges first unlocked in this run (the ending screen pops them).</summary>
    readonly List<string> newBadges = new List<string>();
    string[] newBadgesThisRun => newBadges.ToArray();

    bool missionCardOpen;
    List<GuidedEvent> missionEvents;

    // A mission decision belongs to its day: it doesn't wait out the 12 real seconds between GE cards (at 8x that is
    // more than a whole day); a short breather after the last card is enough.
    const float MissionGapSeconds = 3f;
    bool MissionGapOver => Time.unscaledTime >= quietUntil - QuietSecondsBetweenEvents + MissionGapSeconds;

    static bool SnowyStory => FamilyManager.SelectedStory == FamilyStory.SnowLeopard;

    // ---------- Mission decisions (shown through the normal event cards) ----------

    List<GuidedEvent> MissionEvents()
    {
        Choice Nature(string feedback) => new Choice { label = "Let nature take its course", feedback = feedback, apply = () => { } };
        string name = FocalName;
        if (SnowyStory)
            return new List<GuidedEvent>
            {
                new GuidedEvent {
                    id = GuidedAchievements.ResourceDecision, title = "Mission 1: Find What You Need",
                    text = $"{name} and her family walked far today and they are tired, hungry and thirsty. Every animal needs food and water to survive. What should the family do first?",
                    eligible = () => Day >= 1 && Leopards > 0,
                    choices = new[] {
                        new Choice { label = "Go to the stream for a drink", feedback = "The Snow Leopards get thirsty 25% more slowly today. Tap one to watch its Hydration.",
                                     apply = () => GuidedEffects.Add(GuidedEffects.Kind.LeopardThirst, 0.75f, 1) },
                        new Choice { label = "Rest in the shade to save energy", feedback = "The Snow Leopards get hungry 25% more slowly today. Tap one to watch its Fullness.",
                                     apply = () => GuidedEffects.Add(GuidedEffects.Kind.LeopardHunger, 0.75f, 1) },
                        Nature("The family will look for food and water on its own. Watch where they go!") } },
                new GuidedEvent {
                    id = GuidedAchievements.SafetyDecision, title = "Mission 2: Stay Safe, Stay Strong",
                    text = $"{name}'s mother has spotted a herd of Blue Sheep. Hunting is hard and risky, but the family needs food to stay strong. What should they do?",
                    eligible = () => Day >= 2 && Sheep > 0 && Leopards > 0,
                    choices = new[] {
                        new Choice { label = "Help Mother hunt", feedback = "The Snow Leopards catch prey more often today.",
                                     apply = () => GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, 0.15f, 1) },
                        new Choice { label = "Stay hidden and wait", feedback = "The Snow Leopards save energy today, and the herd escapes more often.",
                                     apply = () => { GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, -0.15f, 1); GuidedEffects.Add(GuidedEffects.Kind.LeopardHunger, 0.85f, 1); } },
                        Nature("The hunters and the herd are on their own today. Watch what happens!") } },
            };
        return new List<GuidedEvent>
        {
            new GuidedEvent {
                id = GuidedAchievements.ResourceDecision, title = "Mission 1: Find What You Need",
                text = $"{name}'s herd is getting hungry and thirsty. Every animal needs food and water to survive. Where should the herd go first?",
                eligible = () => Day >= 1 && Sheep > 0,
                choices = new[] {
                    new Choice { label = "Lead them to fresh grass", feedback = "The Blue Sheep get hungry 25% more slowly today. Tap one to watch its Fullness.",
                                 apply = () => GuidedEffects.Add(GuidedEffects.Kind.SheepHunger, 0.75f, 1) },
                    new Choice { label = "Lead them to the stream", feedback = "The Blue Sheep get thirsty 25% more slowly today. Tap one to watch its Hydration.",
                                 apply = () => GuidedEffects.Add(GuidedEffects.Kind.SheepThirst, 0.75f, 1) },
                    Nature("The herd will look for food and water on its own. Watch where they go!") } },
            new GuidedEvent {
                id = GuidedAchievements.SafetyDecision, title = "Mission 2: Stay Safe, Stay Strong",
                text = $"A Snow Leopard is watching {name}'s herd from the rocks. It needs food for its family, and the Blue Sheep need to stay safe. What should we do?",
                eligible = () => Day >= 2 && Sheep > 0 && Leopards > 0,
                choices = new[] {
                    new Choice { label = "Help the herd stay alert", feedback = "The Blue Sheep escape more often today.",
                                 apply = () => GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, -0.15f, 1) },
                    new Choice { label = "Help the Snow Leopard hunt", feedback = "The Snow Leopards catch prey more often today.",
                                 apply = () => GuidedEffects.Add(GuidedEffects.Kind.HuntSuccess, 0.15f, 1) },
                    Nature("The herd and the hunter are on their own today. Watch what happens!") } },
        };
    }

    // ---------- Per-frame ----------

    void OnEnable() => AnimalBrain.Caught += OnCaught;
    void OnDisable() => AnimalBrain.Caught -= OnCaught;

    void OnCaught(AnimalBrain hunter, AnimalBrain prey)
    {
        if (finished) return;
        catchesByDay.TryGetValue(Day, out int n);
        catchesByDay[Day] = n + 1;
    }

    /// <summary>Ticks off steps the player or the clock completed. Cheap; called every frame of the run.</summary>
    void TrackMissionSteps(int day)
    {
        if (!dayStarts.ContainsKey(day)) dayStarts[day] = new DayStart { sheep = Sheep, leopards = Leopards, grass = Vegetation };
        foreach (var m in GuidedAchievements.All)
            if (day > m.day) resolved.Add(GuidedAchievements.DayDone(m.day));
        if (!resolved.Contains(GuidedAchievements.InspectStep))
        {
            var info = AnimalInfoUI.Instance;
            if (info != null && info.SelectedAnimal != null) resolved.Add(GuidedAchievements.InspectStep);
        }
        UpdateTracker(day);
    }

    /// <summary>Opens a mission card or decision when one is due. True when one is open or just opened.</summary>
    bool UpdateMissionCards()
    {
        if (missionCardOpen) return true;
        if (showing || chapterOpen) return false;

        foreach (var m in GuidedAchievements.All)
            if (GuidedAchievements.IsComplete(m, resolved) && !resolved.Contains(GuidedAchievements.Celebrated(m.id)))
            {
                ShowMissionComplete(m);
                return true;
            }
        if (GuidedAchievements.QuestionReady(resolved)) { ShowBalanceQuestion(); return true; }

        if (!MissionGapOver) return false;
        foreach (var e in missionEvents)
            if (!resolved.Contains(e.id) && e.eligible()) { Show(e); return true; }
        return false;
    }

    // ---------- Tracker ----------

    GameObject trackerBox;
    TextMeshProUGUI trackerText;
    RectTransform trackerMedal;
    int trackerKey = -1;

    // A mission whose decision can't come up any more (its animals are gone for good) can't be finished in this
    // run: the tracker skips it rather than ask for something impossible.
    bool MissionBlocked(GuidedAchievements.Mission m)
    {
        foreach (var e in missionEvents)
            if (!resolved.Contains(e.id) && !e.eligible() && Day > m.day)
                foreach (var s in m.steps)
                    if (s.id == e.id) return true;
        return false;
    }

    void UpdateTracker(int day)
    {
        if (trackerBox == null) return;
        var m = introDone ? GuidedAchievements.Current(day, resolved, MissionBlocked) : null;
        bool show = m != null && !finished && !missionCardOpen && !showing && !chapterOpen;
        if (trackerBox.activeSelf != show) trackerBox.SetActive(show);
        if (!show) return;

        int key = ((int)m.id + 1) * 100000 + resolved.Count;
        if (key == trackerKey) return;
        bool stepTicked = trackerKey > 0 && trackerKey / 100000 == (int)m.id + 1;
        trackerKey = key;

        var sb = new System.Text.StringBuilder();
        sb.Append($"<size=72%><color=#8A6D3B>MISSION {(int)m.id + 1} OF {GuidedAchievements.Count} · DAY {m.day}</color></size>\n");
        sb.Append($"<b>{m.name}</b>\n<size=82%>");
        foreach (var s in m.steps)
        {
            bool done = resolved.Contains(s.id);
            sb.Append(done ? $"<color=#2E7D4F>● <s>{s.text}</s></color>\n" : $"<color=#B9A27A>○</color> {s.text}\n");
        }
        sb.Append("</size>");
        trackerText.text = sb.ToString();
        if (trackerMedal != null && trackerMedal.TryGetComponent(out BadgeMedalGraphic g)) { g.fill = GuidedAchievements.Colour(m.id); g.SetVerticesDirty(); }
        if (trackerMedal != null) SetText(trackerMedal, "Text", GuidedAchievements.Symbol(m.id));
        if (stepTicked) StartCoroutine(EndingCelebration.PopIn((RectTransform)trackerBox.transform, 0f));   // a little bounce for each tick
    }

    // ---------- Mission cards (outcome, question, badge) ----------

    GameObject missionPanel, missionCard;
    TextMeshProUGUI missionHeader, missionTitle, missionBody;
    RectTransform missionMedal;
    Button[] missionButtons;
    TextMeshProUGUI[] missionButtonLabels;
    bool missionCardStyled;

    const float MissionButtonHeight = 95f, MissionButtonStep = 112f, MissionButtonBottom = 36f;

    void OpenMissionCard(string header, string title, string body, GuidedAchievements.Mission medal, params (string label, UnityEngine.Events.UnityAction onClick)[] buttons)
    {
        missionCardOpen = true;
        PopupOpen = true;
        if (speed != null) speed.Pause();
        Mascot.Hide();
        if (!missionCardStyled) { ScreenFlow.StyleLikeShumin(missionCard, null); missionCardStyled = true; }
        missionHeader.text = header;
        missionTitle.text = title;
        missionBody.text = body;
        SetMissionButtons(buttons);
        if (trackerBox != null) trackerBox.SetActive(false);
        missionMedal.gameObject.SetActive(medal != null);
        float top = medal != null ? 335f : 180f;
        missionBody.rectTransform.offsetMax = new Vector2(missionBody.rectTransform.offsetMax.x, -top);
        if (medal != null)
        {
            foreach (Transform c in missionMedal) Destroy(c.gameObject);
            var chip = EndingCelebration.BadgeChip(missionMedal, medal, true, 1.25f);
            var crt = (RectTransform)chip.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f); crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(330f, 150f);
            StartCoroutine(EndingCelebration.PopIn(crt, 0.15f));
            EndingCelebration.Sparkle(this, crt);
        }
        missionPanel.SetActive(true);
        missionPanel.transform.SetAsLastSibling();
        StartCoroutine(EndingCelebration.PopIn((RectTransform)missionCard.transform, 0f));
    }

    // Buttons stack up from the bottom of the card; the text gets whatever height is left above them.
    void SetMissionButtons((string label, UnityEngine.Events.UnityAction onClick)[] buttons)
    {
        int n = Mathf.Min(buttons.Length, missionButtons.Length);
        for (int i = 0; i < missionButtons.Length; i++)
        {
            bool has = i < n;
            missionButtons[i].gameObject.SetActive(has);
            if (!has) continue;
            missionButtonLabels[i].text = buttons[i].label;
            missionButtons[i].onClick.RemoveAllListeners();
            missionButtons[i].onClick.AddListener(buttons[i].onClick);
            ((RectTransform)missionButtons[i].transform).anchoredPosition = new Vector2(0f, MissionButtonBottom + (n - 1 - i) * MissionButtonStep);
        }
        missionBody.rectTransform.offsetMin = new Vector2(missionBody.rectTransform.offsetMin.x, MissionButtonBottom + n * MissionButtonStep + 10f);
    }

    void CloseMissionCard()
    {
        missionPanel.SetActive(false);
        missionCardOpen = false;
        PopupOpen = false;
        quietUntil = Time.unscaledTime + QuietSecondsBetweenEvents;
        if (speed != null && !finished) speed.Play();
    }

    // Survival Guardian: the badge card doubles as the Day 2 outcome ("observe the outcome of a survival decision").
    string SafetyOutcome()
    {
        var sb = new System.Text.StringBuilder();
        if (chosenLabels.TryGetValue(GuidedAchievements.SafetyDecision, out var choice)) sb.Append($"You chose: <b>{choice}</b>\n");
        catchesByDay.TryGetValue(2, out int caught);
        sb.Append(caught == 0 ? "On Day 2 no Blue Sheep were caught.\n"
                 : caught == 1 ? "On Day 2 the Snow Leopards caught 1 Blue Sheep.\n"
                 : $"On Day 2 the Snow Leopards caught {caught} Blue Sheep.\n");
        if (dayStarts.TryGetValue(2, out var start))
            sb.Append($"Blue Sheep: {start.sheep} at the start of the day, {Sheep} now.   Snow Leopards: {start.leopards}, now {Leopards}.\n");
        sb.Append("\nSnow Leopards must hunt to feed their family, and Blue Sheep must stay alert to survive. A choice that helps one animal also changes things for the other.");
        return sb.ToString();
    }

    /// <summary>The story is ending (last day, or Miloy / Snowy died) before a finished mission's badge card could
    /// show: the badge is still earned, and appears as new on the ending screen.</summary>
    void UnlockFinishedMissions()
    {
        foreach (var m in GuidedAchievements.All)
        {
            if (Day > m.day) resolved.Add(GuidedAchievements.DayDone(m.day));
            if (!GuidedAchievements.IsComplete(m, resolved) || !resolved.Add(GuidedAchievements.Celebrated(m.id))) continue;
            if (GuidedAchievements.Unlock(m.id)) newBadges.Add(m.badge);
        }
    }

    void ShowMissionComplete(GuidedAchievements.Mission m)
    {
        resolved.Add(GuidedAchievements.Celebrated(m.id));
        bool isNew = GuidedAchievements.Unlock(m.id);
        if (isNew) newBadges.Add(m.badge);
        var body = m.id == GuidedAchievements.Id.SurvivalGuardian ? SafetyOutcome() + "\n\n" : "";
        body += $"<b>{m.unlockText}</b>\n<size=80%>Learning: {m.objective}" + (isNew ? "" : " · You already have this badge. Great work again!") + "</size>";
        OpenMissionCard($"MISSION {(int)m.id + 1} COMPLETE!", isNew ? $"New badge: {m.badge}!" : m.badge, body, m, ("Yay! Continue", CloseMissionCard));
        EndingCelebration.Burst(missionPanel.transform, isNew ? 110 : 60);
        Mascot.Speak(isNew ? $"You earned the {m.badge} badge!" : "Mission complete!");
    }

    // Ecosystem Guardian: "identify how one species affects another".
    static readonly (string label, bool right)[] BalanceAnswers =
    {
        ("The herd would grow and eat much more grass", true),
        ("Nothing would change in the highlands", false),
        ("The Blue Sheep would start hunting", false),
    };

    void ShowBalanceQuestion()
    {
        catchesByDay.TryGetValue(3, out int caught);
        string today = caught > 0 ? $"Today the Snow Leopards caught {caught} Blue Sheep, and the herd kept eating grass.\n\n" : "Today the herd kept eating grass while the Snow Leopards watched.\n\n";
        var answers = new (string, UnityEngine.Events.UnityAction)[BalanceAnswers.Length];
        for (int i = 0; i < BalanceAnswers.Length; i++)
        {
            bool right = BalanceAnswers[i].right;
            int index = i;
            answers[i] = (BalanceAnswers[i].label, () => AnswerBalance(right, index));
        }
        OpenMissionCard("MISSION 3 · KEEP THE BALANCE", "How does one species affect another?",
            today + "Snow Leopards hunt Blue Sheep, and Blue Sheep eat grass. If all the Snow Leopards disappeared, what would most likely happen?",
            null, answers);
    }

    void AnswerBalance(bool right, int index)
    {
        if (!right)
        {
            missionBody.text = "Not quite! Think about who eats the grass, and who eats the Blue Sheep. Try another answer.";
            if (index < missionButtons.Length) EndingCelebration.Wiggle((RectTransform)missionButtons[index].transform);
            return;
        }
        resolved.Add(GuidedAchievements.BalanceQuestion);
        missionTitle.text = "That's right!";
        missionBody.text = "Without Snow Leopards, more Blue Sheep survive, and more sheep eat more grass. Each species in the food web affects the others.";
        EndingCelebration.Burst(missionPanel.transform, 50);
        // Continue goes straight on to the Ecosystem Guardian badge card.
        SetMissionButtons(new (string, UnityEngine.Events.UnityAction)[] { ("Continue", () => { CloseMissionCard(); UpdateMissionCards(); }) });
    }

    // ---------- UI ----------

    void BuildMissionUI(Transform root)
    {
        // Tracker on the left under the HUD's top boxes (the Everest temperature box ends near y 260); taps go
        // through it to the world.
        trackerBox = Box(root, "Mission tracker", new Vector2(0f, 1f), new Vector2(24f, -285f), new Vector2(560f, 230f), new Color(0.99f, 0.96f, 0.88f, 0.95f));
        trackerBox.GetComponent<Image>().sprite = RoundedSprite.Get(); trackerBox.GetComponent<Image>().type = Image.Type.Sliced;
        trackerBox.GetComponent<Image>().raycastTarget = false;
        var edge = trackerBox.AddComponent<Outline>(); edge.effectColor = new Color(0.55f, 0.42f, 0.25f, 0.8f); edge.effectDistance = new Vector2(3f, -3f);
        trackerMedal = new GameObject("Medal", typeof(RectTransform)).GetComponent<RectTransform>();
        trackerMedal.SetParent(trackerBox.transform, false);
        trackerMedal.anchorMin = trackerMedal.anchorMax = trackerMedal.pivot = new Vector2(1f, 1f);
        trackerMedal.anchoredPosition = new Vector2(-16f, -14f); trackerMedal.sizeDelta = new Vector2(60f, 60f);
        trackerMedal.gameObject.AddComponent<BadgeMedalGraphic>().raycastTarget = false;
        var mt = EndingCelebration.Text(trackerMedal, "1", 34f, Color.white, FontStyles.Bold);
        mt.rectTransform.anchorMin = Vector2.zero; mt.rectTransform.anchorMax = Vector2.one; mt.rectTransform.offsetMin = mt.rectTransform.offsetMax = Vector2.zero;
        trackerText = Label(trackerBox.transform, "", 30f, new Color(0.22f, 0.16f, 0.1f), 22f, 14f, 12f, TextAlignmentOptions.TopLeft);
        trackerText.rectTransform.offsetMax = new Vector2(-84f, -14f);
        trackerText.fontSizeMin = 22f;   // readable for young children
        trackerBox.SetActive(false);

        missionPanel = Dim(root, "Mission");
        missionCard = Box(missionPanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 900f), new Color(0.97f, 0.97f, 0.95f, 1f));
        var card = missionCard.transform;
        missionHeader = Label(card, "", 30f, new Color(0.55f, 0.42f, 0.25f), 60f, 34f, 820f, TextAlignmentOptions.Top);
        missionTitle = Label(card, "", 56f, new Color(0.12f, 0.16f, 0.2f), 60f, 80f, 740f, TextAlignmentOptions.Top);
        missionMedal = new GameObject("Medal", typeof(RectTransform)).GetComponent<RectTransform>();
        missionMedal.SetParent(card, false);
        missionMedal.anchorMin = missionMedal.anchorMax = missionMedal.pivot = new Vector2(0.5f, 1f);
        missionMedal.anchoredPosition = new Vector2(0f, -165f); missionMedal.sizeDelta = new Vector2(340f, 155f);
        missionBody = Label(card, "", 34f, new Color(0.2f, 0.24f, 0.28f), 70f, 180f, 160f, TextAlignmentOptions.Top);
        missionBody.fontSizeMin = 24f;
        missionButtons = new Button[3]; missionButtonLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            var b = Box(card, $"Answer {i + 1}", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(960f, MissionButtonHeight), new Color(0.3f, 0.62f, 0.38f), new Vector2(0.5f, 0f));
            missionButtons[i] = b.AddComponent<Button>();
            missionButtonLabels[i] = Label(b.transform, "", 34f, Color.white, 20f);
        }
        missionPanel.SetActive(false);
    }
}
