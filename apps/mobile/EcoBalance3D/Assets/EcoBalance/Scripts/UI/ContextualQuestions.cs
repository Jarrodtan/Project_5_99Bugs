// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contextual Questions CQ01-CQ05 (Shu Min's research, "Contextual Questions &amp; Reflection"), Free Exploration.
/// Each question fires once per run when its condition happens. The child picks a prediction (A/B/C) and
/// gets the matching feedback; the game then watches for the first outcome that really happens and shows
/// the prediction next to it, with a reflection message. Predictions are never marked right or wrong.
/// Uses Shu Min's ContextualPanel in UI.unity by object name: PredictionPanel, ConfirmPredictionPanel,
/// EventOccuredPanel.
/// </summary>
public class ContextualQuestions : MonoBehaviour
{
    /// <summary>True while a question or reflection is on screen (the mascot and hints stay quiet).</summary>
    public static bool Showing { get; private set; }

    // ---------- The questions (text from the research doc) ----------

    class Outcome { public string reflection; public System.Func<bool> happened; }
    class Question
    {
        public string id, situation, question, learning;
        public string[] options, feedback;
        public System.Func<bool> trigger;
        public System.Action begin;      // baseline values when the prediction is submitted
        public Outcome[] outcomes;       // checked in order; the first that happens is shown
    }

    List<Question> questions;
    readonly HashSet<string> asked = new HashSet<string>();
    readonly Queue<Question> waiting = new Queue<Question>();

    // Live numbers, updated twice a second.
    int sheep, leopards, maxSheep, startSheep, lastSheep, lastLeopards;
    float settleUntil;   // [AI-GENERATED] Claude Code (Claude Opus 5.5): wait for the chosen animals to spawn after Start
    float veg;
    // Since the current prediction.
    int sheepAtPrediction, sheepBirths, sheepDeaths, sheepStarved, leopardsStarved, catches;
    float vegAtPrediction, predictionDay, lowVegSinceDay = -1f;
    bool leopardGotHungry, vegWasLow;
    readonly Dictionary<AnimalBrain, bool> leopardAbove30 = new Dictionary<AnimalBrain, bool>();

    ClockService clock;
    float Day => clock != null ? clock.Day + clock.TimeOfDay01 : Time.time / Mathf.Max(1f, ClockService.DaySeconds);

    void BuildQuestions()
    {
        string[] Fb(string a, string b, string c) => new[] { a, b, c };
        bool DaysPassed(float n) => Day - predictionDay >= n;
        questions = new List<Question>
        {
            new Question {
                id = "CQ01", situation = "The grass is running low!",
                question = "What might happen to the Blue Sheep if there isn't enough grass?",
                learning = "Food scarcity affects herbivores: without enough grass, Blue Sheep struggle to survive.",
                options = new[] { "There will be more Blue Sheep", "Some Blue Sheep may starve", "Nothing will change" },
                feedback = Fb("Interesting prediction! Let's watch what happens to the Blue Sheep!",
                              "Interesting prediction! Let's see if the Blue Sheep can find enough food!",
                              "Interesting prediction! Let's observe how the Blue Sheep respond!"),
                trigger = () => veg < 30f && sheep > 0,
                outcomes = new[] {
                    new Outcome { happened = () => sheepStarved > 0, reflection = "Some Blue Sheep couldn't find enough food and died. How does this compare with your prediction?" },
                    new Outcome { happened = () => veg >= 50f && sheep > 0, reflection = "The grass has grown back, and some Blue Sheep survived! How does this compare with your prediction?" },
                    new Outcome { happened = () => lowVegSinceDay >= 0f && Day - lowVegSinceDay >= 3f && sheep > 0 && sheepStarved == 0, reflection = "The grass is still scarce, but some Blue Sheep are surviving. What do you think might happen next?" } } },
            new Question {
                id = "CQ02", situation = "There are a lot fewer Blue Sheep now.",
                question = "How might fewer Blue Sheep affect the Snow Leopards?",
                learning = "Predators depend on prey: when prey become scarce, predators can go hungry.",
                options = new[] { "Snow Leopards may have less food", "Snow Leopards will have more food", "Nothing will change" },
                feedback = Fb("Interesting prediction! Let's watch whether the Snow Leopards can find enough food!",
                              "Interesting prediction! Let's observe what happens!",
                              "Interesting prediction! Let's see how the Snow Leopards respond!"),
                trigger = () => maxSheep > 0 && sheep <= maxSheep * 0.7f && sheep > 0 && leopards > 0,
                outcomes = new[] {
                    new Outcome { happened = () => leopardGotHungry && sheep > 0, reflection = "A Snow Leopard is getting very hungry while fewer Blue Sheep are around. How does this compare with your prediction?" },
                    new Outcome { happened = () => catches > 0, reflection = "A Snow Leopard managed to catch a Blue Sheep and find food! How does this compare with your prediction?" },
                    new Outcome { happened = () => DaysPassed(3f) && leopards > 0 && leopardsStarved == 0, reflection = "The Snow Leopards are still surviving even though there are fewer Blue Sheep. What do you think might happen next?" } } },
            new Question {
                id = "CQ03", situation = "There are no Snow Leopards left.",
                question = "What might happen to the Blue Sheep without Snow Leopards?",
                learning = "Without predators, prey numbers can grow, but food and water still limit them.",
                options = new[] { "Blue Sheep may increase", "Blue Sheep may decrease", "Nothing will change" },
                feedback = Fb("Interesting prediction! Let's observe how the Blue Sheep population changes!",
                              "Interesting prediction! Let's see what happens to the Blue Sheep!",
                              "Let's watch whether the Blue Sheep population stays the same!"),
                trigger = () => lastLeopards >= 1 && leopards == 0 && sheep > 0,
                outcomes = new[] {
                    new Outcome { happened = () => sheepBirths > 0 && sheep > sheepAtPrediction, reflection = "The Blue Sheep population has grown without Snow Leopards around! How does this compare with your prediction?" },
                    new Outcome { happened = () => sheepDeaths > 0 && sheep < sheepAtPrediction, reflection = "Even without Snow Leopards, the Blue Sheep population has decreased. What else might affect their survival?" },
                    new Outcome { happened = () => DaysPassed(5f) && sheep == sheepAtPrediction && sheepBirths == 0 && sheepDeaths == 0, reflection = "The Blue Sheep population has stayed the same so far. What might happen if we observe it for longer?" } } },
            new Question {
                id = "CQ04", situation = "There are no Blue Sheep left.",
                question = "What might happen to the Snow Leopards without Blue Sheep to hunt?",
                learning = "Predators need prey to survive: with no prey, they cannot find food.",
                options = new[] { "Snow Leopards may struggle to survive", "Snow Leopards will have more food", "Nothing will change" },
                feedback = Fb("Interesting prediction! Let's observe how the Snow Leopards manage without food!",
                              "Interesting prediction! Let's see what happens!",
                              "Interesting prediction! Let's watch how the Snow Leopards respond!"),
                trigger = () => lastSheep >= 1 && sheep == 0 && leopards > 0,
                outcomes = new[] {
                    new Outcome { happened = () => leopardsStarved > 0, reflection = "A Snow Leopard couldn't find enough food and died. How does this compare with your prediction?" },
                    new Outcome { happened = () => leopardGotHungry && sheep == 0, reflection = "A Snow Leopard is becoming very hungry because there are no Blue Sheep left to hunt. What do you think might happen next?" },
                    new Outcome { happened = () => DaysPassed(3f) && leopards > 0 && leopardsStarved == 0, reflection = "The Snow Leopards have survived without Blue Sheep for now. Do you think they can continue surviving without food?" } } },
            new Question {
                id = "CQ05", situation = "The Blue Sheep herd is growing!",
                question = "What might happen to the grass if there are more Blue Sheep?",
                learning = "More herbivores eat more plants: grazing and regrowth decide how much grass is left.",
                options = new[] { "Grass may increase", "Grass may decrease", "Nothing will change" },
                feedback = Fb("Interesting prediction! Let's watch how the grass changes as the Blue Sheep graze!",
                              "Interesting prediction! Let's observe what happens to the grass!",
                              "Interesting prediction! Let's see whether the amount of grass stays the same!"),
                trigger = () => startSheep > 0 && sheep >= startSheep * 1.3f,
                outcomes = new[] {
                    new Outcome { happened = () => veg <= vegAtPrediction - 10f, reflection = "The amount of grass has decreased as the Blue Sheep keep grazing. How does this compare with your prediction?" },
                    new Outcome { happened = () => veg >= vegAtPrediction + 10f, reflection = "The grass has grown even with more Blue Sheep around! What might have helped it grow back?" },
                    new Outcome { happened = () => DaysPassed(3f) && Mathf.Abs(veg - vegAtPrediction) <= 5f, reflection = "The amount of grass has stayed almost the same so far. What do you think might happen if the Blue Sheep keep grazing?" } } },
        };
    }

    // ---------- Shu Min's panels ----------

    Transform root, predictionPanel, confirmPanel, occurredPanel;
    Button confirmBtn;
    Question active, watching;
    int picked = -1;
    SimulationSpeedController speed;
    bool wasRunning;
    float nextCheck;

    /// <summary>Adds the questions to the Free Exploration HUD (does nothing if the panel isn't there).</summary>
    public static void Attach(Transform hud)
    {
        var panel = Find(hud, "ContextualPanel");
        if (panel == null) return;
        var cq = hud.gameObject.AddComponent<ContextualQuestions>();
        cq.root = panel;
    }

    void Start()
    {
        BuildQuestions();
        clock = FindFirstObjectByType<ClockService>();
        speed = FindFirstObjectByType<SimulationSpeedController>();
        predictionPanel = Find(root, "PredictionPanel");
        confirmPanel = Find(root, "ConfirmPredictionPanel");
        occurredPanel = Find(root, "EventOccuredPanel");
        root.gameObject.SetActive(false);
        OnTop(root);

        for (int i = 0; i < 3; i++)
        {
            int n = i;
            OnClick(predictionPanel, $"OptionBtn_0{i + 1}", () => Pick(n));
        }
        var cb = Find(predictionPanel, "ConfirmBtn");
        if (cb != null) confirmBtn = cb.GetComponent<Button>();
        OnClick(predictionPanel, "ConfirmBtn", Confirm);
        OnClick(confirmPanel, "GotItBtn", Close);
        OnClick(occurredPanel, "ViewJournalBtn", () => { Close(); ScreenFlow.OpenJournalOverlay(); });
        OnClick(root, "CrossBtn", Close);

        AnimalBrain.Starved += OnStarved;
        AnimalBrain.Caught += OnCaught;
        AnimalBrain.Born += OnBorn;
    }

    void OnDestroy()
    {
        AnimalBrain.Starved -= OnStarved;
        AnimalBrain.Caught -= OnCaught;
        AnimalBrain.Born -= OnBorn;
        Showing = false;
    }

    void OnStarved(AnimalBrain a) { if (a.IsPredatorSpecies) leopardsStarved++; else { sheepStarved++; sheepDeaths++; } }
    void OnCaught(AnimalBrain predator, AnimalBrain prey) { catches++; if (prey != null && !prey.IsPredatorSpecies) sheepDeaths++; }
    void OnBorn(AnimalBrain baby) { if (baby != null && !baby.IsPredatorSpecies) sheepBirths++; }

    void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.5f;
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): behind the Free setup menu the world still has the previous
        // run's animals. Counting them made "a lot fewer Blue Sheep" fire on Day 1 (e.g. 50 before, 10 chosen).
        // Start counting only once the chosen run is on screen and its animals have spawned.
        if (ConfigurationScreen.Open) { maxSheep = startSheep = lastSheep = lastLeopards = 0; settleUntil = Time.unscaledTime + 2f; return; }
        if (Time.unscaledTime < settleUntil) return;
        Count();

        if (watching != null && !Showing)
        {
            foreach (var o in watching.outcomes)
                if (o.happened()) { ShowOutcome(watching, o); break; }
        }

        foreach (var q in questions)
            if (!asked.Contains(q.id) && q.trigger()) { asked.Add(q.id); waiting.Enqueue(q); }
        // One at a time: the next waits until the current one has been answered and its outcome shown.
        if (!Showing && watching == null && waiting.Count > 0 && !OtherPopupOpen()) Ask(waiting.Dequeue());

        lastSheep = sheep;
        lastLeopards = leopards;
    }

    void Count()
    {
        sheep = leopards = 0;
        foreach (var a in AnimalBrain.All)
        {
            if (a == null || a.IsCaught) continue;
            if (a.IsPredatorSpecies)
            {
                leopards++;
                bool above = a.curr_hunger > 30f;
                if (leopardAbove30.TryGetValue(a, out bool was) && was && !above && watching != null) leopardGotHungry = true;
                leopardAbove30[a] = above;
            }
            else sheep++;
        }
        veg = ResourcePatch.VegetationPercent();
        if (startSheep == 0 && sheep > 0) startSheep = sheep;
        maxSheep = Mathf.Max(maxSheep, sheep);
        if (veg < 30f) { if (!vegWasLow) lowVegSinceDay = Day; vegWasLow = true; }
        else { vegWasLow = false; lowVegSinceDay = -1f; }
    }

    static bool OtherPopupOpen() =>
        UnityEngine.SceneManagement.SceneManager.GetSceneByName(ScreenFlow.Journal).isLoaded;

    // ---------- Flow: question -> prediction feedback -> (watch) -> what happened ----------

    void Ask(Question q)
    {
        active = q;
        picked = -1;
        Showing = true;
        wasRunning = speed != null && speed.IsRunning;
        if (speed != null) speed.Pause();

        SetText(predictionPanel, "TitleTxt", "Let's Predict!");
        SetText(predictionPanel, "ContextualTxt", q.situation);
        SetText(predictionPanel, "QuestionTxt", q.question);
        SetText(predictionPanel, "ExplainationTxt", "Pick what you think will happen, then press Confirm.");
        for (int i = 0; i < 3; i++)
        {
            var b = Find(predictionPanel, $"OptionBtn_0{i + 1}");
            SetText(b, "OptionTxt", q.options[i]);
            Highlight(b, false);
        }
        if (confirmBtn != null) confirmBtn.interactable = false;
        Show(predictionPanel);
    }

    void Pick(int i)
    {
        picked = i;
        for (int n = 0; n < 3; n++) Highlight(Find(predictionPanel, $"OptionBtn_0{n + 1}"), n == i);
        if (confirmBtn != null) confirmBtn.interactable = true;
    }

    void Confirm()
    {
        if (active == null || picked < 0) return;
        SetText(confirmPanel, "TitleTxt", "Your Prediction");
        SetText(confirmPanel, "YouChoseTxt", "You chose:");
        SetText(confirmPanel, "PredictionChoiceTxt", active.options[picked]);
        SetText(confirmPanel, "InterestingTxt", active.feedback[picked]);
        SetText(confirmPanel, "WatchTxt", "Keep watching the highlands. We'll tell you what happens!");
        SetText(confirmPanel, "EducationalTxt", active.learning);
        Show(confirmPanel);

        // Baseline for the outcomes.
        watching = active;
        sheepAtPrediction = sheep; vegAtPrediction = veg; predictionDay = Day;
        sheepBirths = sheepDeaths = sheepStarved = leopardsStarved = catches = 0;
        leopardGotHungry = false;
        watchingPick = picked;
    }

    int watchingPick;

    void ShowOutcome(Question q, Outcome o)
    {
        Showing = true;
        wasRunning = speed != null && speed.IsRunning;
        if (speed != null) speed.Pause();
        SetText(occurredPanel, "TitleTxt", "Something Happened!");
        SetText(occurredPanel, "YouChoseTxt", "You predicted:");
        SetText(occurredPanel, "PredictionChoiceTxt", q.options[watchingPick]);
        SetText(occurredPanel, "ObservedTxt", o.reflection);
        SetText(occurredPanel, "WatchTxt", "There's no right or wrong answer: scientists learn by predicting and watching!");
        SetText(occurredPanel, "EducationalTxt", q.learning);
        watching = null;
        active = null;
        Show(occurredPanel);
    }

    void Show(Transform panel)
    {
        root.gameObject.SetActive(true);
        foreach (var p in new[] { predictionPanel, confirmPanel, occurredPanel })
            if (p != null) p.gameObject.SetActive(p == panel);
    }

    void Close()
    {
        root.gameObject.SetActive(false);
        Showing = false;
        if (wasRunning && speed != null) speed.Play();
        if (watching == null) active = null;   // closed with X before confirming: the question is skipped
    }

    // ---------- Helpers (names in the mock-up may carry quotes or spaces) ----------

    static Transform Find(Transform under, string name)
    {
        if (under == null) return null;
        foreach (var t in under.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim().Trim('\'').Trim() == name) return t;
        return null;
    }

    static void SetText(Transform under, string name, string text)
    {
        var t = Find(under, name);
        if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = text;
    }

    static void OnClick(Transform under, string name, UnityEngine.Events.UnityAction action)
    {
        var t = Find(under, name);
        if (t == null) return;
        if (!t.TryGetComponent(out Button b)) b = t.gameObject.AddComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    static void Highlight(Transform option, bool on)
    {
        if (option != null && option.TryGetComponent(out Image img)) img.color = on ? new Color(0.8f, 0.94f, 0.82f) : Color.white;
    }

    // Above the HUD, the season banner and the live weather line.
    static void OnTop(Transform panel)
    {
        if (panel.GetComponent<Canvas>() != null) return;
        var c = panel.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 80;
        panel.gameObject.AddComponent<GraphicRaycaster>();
    }
}
