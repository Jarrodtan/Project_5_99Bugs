using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MascotManager : MonoBehaviour
{
    [Header("Mascot UI")]
    [SerializeField] private GameObject mascotPanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button dismissButton;
    [SerializeField] private Button helpButton;

    [Header("Simulation Introduction")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] introductionDialogue =
    {
        "Hello Explorer! Welcome to your ecosystem!",
        "You can explore the environment and observe different animals.",
        "Try selecting an animal to learn more about its needs and behaviour!",
        "You can also control the simulation speed and observe how the ecosystem changes."
    };

    [Header("Help Dialogue")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] helpDialogue =
    {
        "Need some help, Explorer?",
        "Select an animal to inspect its information and learn about its behaviour.",
        "Use the simulation controls to play, pause and change the simulation speed."
    };

    private string[] currentDialogue;
    private int currentIndex;
    private bool isSequenceActive;

    private void Awake()
    {
        nextButton.onClick.AddListener(NextDialogue);
        skipButton.onClick.AddListener(SkipDialogue);
        dismissButton.onClick.AddListener(DismissDialogue);
        helpButton.onClick.AddListener(ShowHelp);
    }

    private void OnEnable()
    {
        DramaDirector.MomentDisplayed += HandleDramaEvent;
    }

    private void Start()
    {
        mascotPanel.SetActive(false);

        // The welcome dialogue is an intentional trigger when this simulation scene opens.
        if (introductionDialogue != null && introductionDialogue.Length > 0)
            ShowIntroduction();
    }

    private void OnDisable()
    {
        DramaDirector.MomentDisplayed -= HandleDramaEvent;
    }

    private void HandleDramaEvent(DramaDirector.Moment moment, string eventCaption)
    {
        // An accepted ecosystem event takes priority over the current dialogue.
        // The event banner and mascot explanation therefore appear together.
        ShowContextualMessage(BuildMascotMessage(moment, eventCaption));
    }

    private string BuildMascotMessage(DramaDirector.Moment moment, string eventCaption)
    {
        switch (moment)
        {
            case DramaDirector.Moment.Hunt:
                return "Look, a predator is hunting! Predators hunt other animals for food.";
            case DramaDirector.Moment.Birth:
                return "A new animal is on the way! This helps the population grow.";
            case DramaDirector.Moment.Mating:
                return "These animals are starting a family. Healthy animals can reproduce.";
            case DramaDirector.Moment.Starvation:
                return "An animal has run out of energy. Animals need enough food to stay healthy.";
            case DramaDirector.Moment.Catch:
                if (!string.IsNullOrEmpty(eventCaption) &&
                    eventCaption.ToLowerInvariant().Contains("got away"))
                    return "The prey escaped this time! A hunt does not always succeed.";

                return "The predator caught its prey. This is called predation, and it is how the predator gets food.";
            default:
                return eventCaption;
        }
    }

    public void ShowIntroduction()
    {
        ShowDialogue(introductionDialogue);
    }

    public void ShowHelp()
    {
        ShowDialogue(helpDialogue);
    }

    public void ShowContextualMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        currentDialogue = null;
        isSequenceActive = false;

        mascotPanel.SetActive(true);
        dialogueText.text = message;

        nextButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        dismissButton.gameObject.SetActive(true);
    }

    private void ShowDialogue(string[] dialogue)
    {
        if (dialogue == null || dialogue.Length == 0)
            return;

        currentDialogue = dialogue;
        currentIndex = 0;
        isSequenceActive = true;

        mascotPanel.SetActive(true);
        nextButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(true);
        dismissButton.gameObject.SetActive(false);

        UpdateDialogue();
    }

    private void UpdateDialogue()
    {
        if (currentDialogue == null || currentDialogue.Length == 0)
            return;

        dialogueText.text = currentDialogue[currentIndex];

        bool isLastDialogue = currentIndex >= currentDialogue.Length - 1;
        nextButton.gameObject.SetActive(!isLastDialogue);
        skipButton.gameObject.SetActive(true);
        dismissButton.gameObject.SetActive(isLastDialogue);
    }

    private void NextDialogue()
    {
        if (currentDialogue == null)
            return;

        if (currentIndex < currentDialogue.Length - 1)
        {
            currentIndex++;
            UpdateDialogue();
        }
    }

    private void SkipDialogue()
    {
        DismissDialogue();
    }

    public void DismissDialogue()
    {
        isSequenceActive = false;
        currentDialogue = null;
        mascotPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        nextButton.onClick.RemoveListener(NextDialogue);
        skipButton.onClick.RemoveListener(SkipDialogue);
        dismissButton.onClick.RemoveListener(DismissDialogue);
        helpButton.onClick.RemoveListener(ShowHelp);
    }
}
