using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Colors the Play / Pause buttons to show the simulation's current state (green while playing,
/// red while paused) and briefly flashes Reset when pressed. Reads <see cref="SimulationSpeedController.IsRunning"/>
/// every frame, so it stays correct however the state changes (e.g. Reset also resumes play).
/// </summary>
public class PlaybackButtonHighlight : MonoBehaviour
{
    [SerializeField]
    SimulationSpeedController controller;

    [SerializeField]
    Button playButton;

    [SerializeField]
    Button pauseButton;

    [SerializeField]
    Button resetButton;

    [Header("Colors")]
    [Tooltip("Play button background while the simulation is running.")]
    [SerializeField]
    Color playingColor = new Color(0.25f, 0.7f, 0.3f, 1f);

    [Tooltip("Pause button background while the simulation is paused.")]
    [SerializeField]
    Color pausedColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    [Tooltip("Reset button background at the start of its press flash.")]
    [SerializeField]
    Color resetFlashColor = new Color(0.95f, 0.65f, 0.15f, 1f);

    [Tooltip("Seconds for the Reset flash to fade back.")]
    [Min(0f)]
    [SerializeField]
    float resetFlashSeconds = 0.4f;

    [Tooltip("Label color on a colored (active) button.")]
    [SerializeField]
    Color activeTextColor = Color.white;

    [Tooltip("Button background when not active.")]
    [SerializeField]
    Color idleColor = Color.white;

    TMP_Text playLabel;
    TMP_Text pauseLabel;
    TMP_Text resetLabel;
    Color idleTextColor = Color.black;
    float resetFlashStart = float.NegativeInfinity;

    void Awake()
    {
        if (controller == null)
            Debug.LogError($"{nameof(PlaybackButtonHighlight)} requires a {nameof(SimulationSpeedController)}.", this);

        playLabel = LabelOf(playButton);
        pauseLabel = LabelOf(pauseButton);
        resetLabel = LabelOf(resetButton);
        if (playLabel != null)
            idleTextColor = playLabel.color;
    }

    void OnEnable()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(FlashReset);
    }

    void OnDisable()
    {
        if (resetButton != null)
            resetButton.onClick.RemoveListener(FlashReset);
    }

    void Update()
    {
        if (controller == null)
            return;

        var running = controller.IsRunning;
        Paint(playButton, playLabel, running ? playingColor : idleColor, running);
        Paint(pauseButton, pauseLabel, running ? idleColor : pausedColor, !running);

        var flash = resetFlashSeconds > 0f ? 1f - (Time.unscaledTime - resetFlashStart) / resetFlashSeconds : 0f;
        flash = Mathf.Clamp01(flash);
        Paint(resetButton, resetLabel, Color.Lerp(idleColor, resetFlashColor, flash), flash > 0.5f);
    }

    void FlashReset() => resetFlashStart = Time.unscaledTime;

    void Paint(Button button, TMP_Text label, Color background, bool active)
    {
        if (button == null)
            return;

        // The Image color is multiplied by the Button's ColorTint, so hover/press feedback still works.
        if (button.image != null)
            button.image.color = background;
        if (label != null)
            label.color = active ? activeTextColor : idleTextColor;
    }

    static TMP_Text LabelOf(Button button) => button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
}
