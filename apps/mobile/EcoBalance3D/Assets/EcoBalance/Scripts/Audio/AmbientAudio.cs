using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Ambient sound for the simulation scene (put this object IN the simulation scene, not under AudioManager).
///   Day:   Day_1..3 play one after another (random order, never the same twice in a row), crossfaded.
///   Night: the Night track, repeated with a crossfade so there is no gap at the loop point.
///   Rain:  a loop that fades in while it rains and out when it stops; the music dips a little under it.
/// Day/night is read from ClockService.IsDaytime; rain comes from WeatherVisualController via SetRaining(bool).
/// Pause: while the sim is paused the music fades out and freezes (it does not move on to the next track
///        without the clock). Set Paused Volume above 0 to keep a quiet bed instead.
/// Speed: switch gaps and crossfades shrink with the sim speed, so the music keeps up at 8x.
/// Silent while the Free setup menu is open. Fades use unscaled time, so they work while Time.timeScale is 0.
/// Volumes follow the player's Sounds slider (AudioPrefs.Sounds), like the other ambience.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }

    [Header("Clips")]
    [Tooltip("Day_1, Day_2, Day_3")]
    public AudioClip[] dayTracks;
    public AudioClip nightTrack;
    public AudioClip rainLoop;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float rainVolume = 0.6f;
    [Tooltip("Music level while it rains (1 = no change).")]
    [Range(0f, 1f)] public float musicWhileRaining = 0.6f;
    public float crossfadeSeconds = 3f;
    public float rainFadeSeconds = 2f;

    [Header("Sim speed and pause")]
    [Tooltip("Real seconds to wait before switching between day and night music at 1x. It is divided by the sim speed, so it keeps up at 8x.")]
    public float minSecondsBetweenSwitches = 20f;
    [Tooltip("Music level while the sim is paused. 0 fades it out and freezes it until Play is pressed.")]
    [Range(0f, 1f)] public float pausedVolume = 0f;
    public float pauseFadeSeconds = 0.6f;

    [Header("Other")]
    [Tooltip("Mute AudioManager's old ambience loop while this scene is open so the two never play together. It is restored when the scene closes.")]
    public bool replaceOldAmbience = true;
    [Tooltip("Log which track starts and a status line every 5 seconds. Turn off once it works.")]
    public bool debugLog = true;
    [Tooltip("Send these sounds through AudioManager's ambience mixer group. Off by default: a snapshot that turns that group down would silence them.")]
    public bool useAudioManagerGroup = false;

    [Header("Optional mixer groups (leave empty for none)")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup rainGroup;

    readonly AudioSource[] music = new AudioSource[2];   // two sources so tracks can crossfade
    readonly float[] level = new float[2];               // 1 = playing / fading in, 0 = fading out
    readonly float[] own = new float[2];                 // each source's crossfade volume (before the pause fade)
    AudioSource rain;
    int active;
    int lastDay = -1;
    bool started, isDay = true, raining, frozen;
    float pauseMul = 1f;
    SimulationSpeedController speed;
    ClockService clock;
    float lastSwitch = -999f;
    AudioSource oldAmbience;
    bool oldWasMuted;
    float nextTrackAllowed;
    float nextReport;
    bool warnedNoClip;

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < 2; i++) music[i] = MakeSource($"Music_{i}", musicGroup, false);
        rain = MakeSource("Rain", rainGroup, true);
        rain.clip = rainLoop;
    }

    void Start()
    {
        started = true;

        if (debugLog && GetComponentInParent<AudioManager>() != null)
            Debug.LogWarning("[AMBIENT AUDIO] This object is under AudioManager, which survives scene changes. Move it into the simulation scene, or it will play in every scene.", this);
        if (useAudioManagerGroup) UseManagerGroup();

        // This scene's ambience replaces AudioManager's old loop while the scene is open (restored in OnDestroy).
        var manager = AudioManager.Instance;
        if (replaceOldAmbience && manager != null && manager.ambience != null)
        {
            oldAmbience = manager.ambience;
            oldWasMuted = oldAmbience.mute;
            oldAmbience.mute = true;
        }

        // Start in the right state: a saved run can load straight into the night, and the sim may open paused.
        clock = FindAnyObjectByType<ClockService>();
        if (clock != null) isDay = clock.IsDaytime;
        speed = FindAnyObjectByType<SimulationSpeedController>();
        pauseMul = speed != null && !speed.IsRunning ? pausedVolume : 1f;
        lastSwitch = Time.unscaledTime;

        PlayNext();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (oldAmbience != null) oldAmbience.mute = oldWasMuted;   // the menu and other scenes get their old loop back
    }

    /// <summary>Switch between the day tracks and the night track.</summary>
    public void SetDaytime(bool day)
    {
        if (day == isDay) return;
        isDay = day;
        lastSwitch = Time.unscaledTime;
        if (started) PlayNext();
    }

    /// <summary>Call when the weather changes (true while it is raining).</summary>
    public void SetRaining(bool on) { raining = on; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool quiet = ConfigurationScreen.Open;   // stay silent behind the Free setup menu
        if (speed == null) speed = FindAnyObjectByType<SimulationSpeedController>();
        bool paused = speed != null && !speed.IsRunning;
        float simSpeed = speed != null ? Mathf.Max(1f, speed.CurrentSpeed) : 1f;

        // Pause: fade the music down, then freeze it so it doesn't move on while the clock is stopped.
        float pauseGoal = paused ? pausedVolume : 1f;
        pauseMul = Mathf.MoveTowards(pauseMul, pauseGoal, dt / Mathf.Max(0.05f, pauseFadeSeconds));
        SetFrozen(paused && pausedVolume <= 0.001f && pauseMul <= 0.001f);

        // Follow the day-night clock (it has no day/night event, so check it every frame).
        // The wait shrinks with the sim speed, so at 8x the music still changes with the day.
        if (clock == null) clock = FindAnyObjectByType<ClockService>();
        float gap = Mathf.Max(2f, minSecondsBetweenSwitches / simSpeed);
        if (clock != null && !paused && clock.IsDaytime != isDay && Time.unscaledTime - lastSwitch >= gap)
            SetDaytime(clock.IsDaytime);

        // Shorter crossfades when time runs fast.
        float fadeSecs = Mathf.Clamp(crossfadeSeconds / simSpeed, 0.75f, Mathf.Max(0.75f, crossfadeSeconds));

        float vol = AudioPrefs.Sounds;   // the player's Sounds slider; ambience belongs to it

        if (!frozen)
        {
            // Start the next track while the current one is about to end, or if nothing is playing.
            // At most once a second, so a clip with a bad length can never restart itself every frame.
            var cur = music[active];
            if (!paused && Time.unscaledTime >= nextTrackAllowed)
            {
                bool ending = false;
                if (cur.clip != null && cur.isPlaying && cur.clip.length > 1f)
                {
                    float fade = Mathf.Min(fadeSecs, cur.clip.length * 0.5f);
                    ending = cur.time > 0.5f && cur.clip.length - cur.time <= fade;
                }
                if (ending || !cur.isPlaying) PlayNext();
            }

            float duck = raining ? musicWhileRaining : 1f;
            float step = dt / Mathf.Max(0.01f, fadeSecs) * musicVolume;
            for (int i = 0; i < 2; i++)
            {
                var s = music[i];
                if (!s.isPlaying) continue;
                float goal = quiet ? 0f : level[i] * musicVolume * duck * vol;
                own[i] = Mathf.MoveTowards(own[i], goal, step);
                s.volume = own[i] * pauseMul;
                if (level[i] == 0f && own[i] <= 0.001f) s.Stop();
            }
        }

        if (rain.clip != null)
        {
            float rainGoal = raining && !paused && !quiet ? rainVolume * vol : 0f;
            if (rainGoal > 0f && !rain.isPlaying) rain.Play();
            rain.volume = Mathf.MoveTowards(rain.volume, rainGoal, dt / Mathf.Max(0.01f, rainFadeSeconds) * rainVolume);
            if (rainGoal == 0f && rain.volume <= 0.001f && rain.isPlaying) rain.Stop();
        }

        if (debugLog && Time.unscaledTime >= nextReport)
        {
            nextReport = Time.unscaledTime + 5f;
            var a = music[active];
            Debug.Log($"[AMBIENT AUDIO] status: '{(a.clip ? a.clip.name : "none")}' playing={a.isPlaying} volume={a.volume:F2} mute={a.mute} time={a.time:F0}s | paused={paused} frozen={frozen} speed={simSpeed:0}x | day={isDay} | quiet(setup menu)={quiet} | Sounds={vol:F2} | raining={raining} | old ambience muted={(oldAmbience != null && oldAmbience.mute)} | parent={(transform.parent != null ? transform.parent.name : "scene root")}");
        }
    }

    // Freeze / unfreeze the music sources (the playhead stops while the sim is paused).
    void SetFrozen(bool freeze)
    {
        if (freeze == frozen) return;
        frozen = freeze;
        foreach (var s in music)
        {
            if (freeze) { if (s.isPlaying) s.Pause(); }
            else s.UnPause();
        }
    }

    // Fade the current track out and the next one in.
    void PlayNext()
    {
        AudioClip clip = isDay ? NextDay() : nightTrack;
        nextTrackAllowed = Time.unscaledTime + 1f;
        if (clip == null)
        {
            if (debugLog && !warnedNoClip)
            {
                warnedNoClip = true;
                Debug.LogWarning("[AMBIENT AUDIO] No clip to play. Are Day Tracks / Night Track assigned?", this);
            }
            return;
        }

        level[active] = 0f;
        active = 1 - active;
        var s = music[active];
        s.clip = clip;
        s.time = 0f;
        own[active] = 0f;
        s.volume = 0f;
        s.Play();
        level[active] = 1f;
        if (debugLog)
            Debug.Log($"[AMBIENT AUDIO] Playing '{clip.name}' ({clip.length:F1}s) | day: {isDay} | source: {s.name} | group: {(s.outputAudioMixerGroup != null ? s.outputAudioMixerGroup.name : "none")} | Sounds slider: {AudioPrefs.Sounds:F2}");
    }

    AudioClip NextDay()
    {
        if (dayTracks == null || dayTracks.Length == 0) return null;

        // Move to the next track sequentially, looping back to 0 when it reaches the end
        lastDay = (lastDay + 1) % dayTracks.Length;

        return dayTracks[lastDay];
    }

    // Optional: looked up in Start, when AudioManager is sure to exist.
    void UseManagerGroup()
    {
        var manager = AudioManager.Instance;
        var group = manager != null && manager.ambience != null ? manager.ambience.outputAudioMixerGroup : null;
        if (group == null) return;
        if (musicGroup == null) foreach (var m in music) m.outputAudioMixerGroup = group;
        if (rainGroup == null) rain.outputAudioMixerGroup = group;
    }

    AudioSource MakeSource(string objName, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;   // 2D: not tied to a place in the world
        s.volume = 0f;
        s.outputAudioMixerGroup = group;
        return s;
    }
}
