// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One click sound for every button in the game (SCRUM-338 follow-up, audio consultation 17 Nov).
/// On every scene load it adds this component to each Button. It listens to the pointer click itself,
/// not to Button.onClick, because ScreenFlow / Mascot / GuidedChallenge replace onClick lists in code.
///   Normal buttons:  Resources/UISound/Click
///   Mascot buttons:  Resources/UISound/MascotClick (Vuk's mascot SFX; until it is added, the normal click)
/// The old per-button "AudioSource.Play" click listeners are switched off so nothing plays twice.
/// </summary>
public class UIClickSound : MonoBehaviour, IPointerClickHandler
{
    static AudioClip click, mascotClick;
    static AudioSource source;

    bool mascot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;   // Enter Play Mode skips domain reload: never hook twice
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddToAll();                                  // the first scene is already loaded
    }

    static void OnSceneLoaded(Scene _, LoadSceneMode __) => AddToAll();

    /// <summary>Gives every Button in the open scenes the click sound. Safe to call again for buttons made in code.</summary>
    public static void AddToAll()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!b.TryGetComponent(out UIClickSound _)) b.gameObject.AddComponent<UIClickSound>();
    }

    void Awake()
    {
        mascot = IsMascotButton(transform);
        if (TryGetComponent(out Button b)) MuteOldClick(b);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (!TryGetComponent(out Button b) || !b.IsInteractable()) return;
        Play(mascot);
    }

    static void Play(bool forMascot)
    {
        if (click == null) click = Resources.Load<AudioClip>("UISound/UI Button Click Positive_2");
        if (mascotClick == null) mascotClick = Resources.Load<AudioClip>("UISound/Mouth-Pop-Pick-up_SFX");
        var clip = forMascot && mascotClick != null ? mascotClick : click;
        if (clip == null) return;
        if (source == null)
        {
            var go = new GameObject("UIClickSound");
            DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;          // UI: same loudness everywhere
            source.ignoreListenerPause = true; // still clicks while the run is paused
        }
        // Same mixer group as the other SFX, so the volume slider still works.
        if (AudioManager.Instance != null && AudioManager.Instance.sfx != null)
            source.outputAudioMixerGroup = AudioManager.Instance.sfx.outputAudioMixerGroup;
        source.PlayOneShot(clip);
    }

    // Buttons inside the mascot's speech bubble (Next, Skip, Got It, Explore!).
    static bool IsMascotButton(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.name.Contains("Mascot")) return true;
        return false;
    }

    // The Inspector click sounds (AudioSource.Play in onClick) would play on top of this one.
    static void MuteOldClick(Button b)
    {
        for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            if (b.onClick.GetPersistentMethodName(i) == "Play" && b.onClick.GetPersistentTarget(i) is AudioSource)
                b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
    }
}
