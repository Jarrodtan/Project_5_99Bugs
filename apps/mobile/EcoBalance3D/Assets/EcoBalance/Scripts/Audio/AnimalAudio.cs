using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimalAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] idle;

    [Header("Herd (shared by every animal with the same Herd Id)")]
    public string herdId = "Sheep";                       // e.g. "SnowLeopard" on the leopard prefab
    public Vector2 herdGap = new Vector2(6f, 20f);        // seconds of quiet between calls near you
    [Range(0f, 1f)] public float burstChance = 0.2f;      // chance the next call comes quickly
    public Vector2 burstGap = new Vector2(1.5f, 4f);

    [Header("This animal")]
    public float personalCooldown = 30f;                  // min seconds before THIS animal calls again
    public Vector2 playLength = new Vector2(1.5f, 3.5f);  // each call plays only this many seconds of the clip (0 = whole clip)
    public float maxClipSeconds = 6f;                     // cut long clips short with a fade-out (0 = play whole clip)

    [Header("Debug")]
    public bool debugLog = true;   // turn off once it works

    [Header("3D Sound")]
    public float maxDistance = 50f;
    [Tooltip("Volume (up) vs distance (right). 0 = at the animal, 1 = at Max Distance. Edit the curve to make calls louder or softer at range.")]
    public AnimationCurve rolloff = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.25f, 0.65f), new Keyframe(0.5f, 0.3f),
        new Keyframe(0.75f, 0.1f), new Keyframe(1f, 0f));
    public Vector2 volumeRange = new Vector2(0.85f, 1f);
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f);
    public bool muffleWithDistance = true;

    // Shared across ALL animals using this script
    const int MaxSimultaneous = 3;
    static int activeVoices = 0;
    static Transform listener;
    static readonly Dictionary<string, float> nextCallTimes = new Dictionary<string, float>();

    static readonly Dictionary<string, float> debugTimes = new Dictionary<string, float>();
    static float nextAuditTime = 0f;

    AudioSource source;
    AudioLowPassFilter lowPass;
    bool counted = false;
    float lastCallTime = -999f;
    float spawnTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        activeVoices = 0;
        listener = null;
        nextCallTimes.Clear();
        debugTimes.Clear();
        nextAuditTime = 0f;
    }

    void Awake()
    {
        source = GetComponent<AudioSource>();
        spawnTime = Time.time;
        source.playOnAwake = false;
        source.loop = false;
        source.mute = true; // only unmuted while AnimalAudio itself is playing a call

        // Any other AudioSource on this animal (or its children) must not auto-play or loop
        foreach (AudioSource other in GetComponentsInChildren<AudioSource>(true))
        {
            if (other == source) continue;
            other.playOnAwake = false;
            other.loop = false;
            other.Stop();
        }

        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Custom;
        AnimationCurve curve = (rolloff != null && rolloff.length >= 2)
            ? rolloff
            : new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.65f), new Keyframe(0.5f, 0.3f),
                                 new Keyframe(0.75f, 0.1f), new Keyframe(1f, 0f));
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

        if (muffleWithDistance)
        {
            lowPass = GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 22000f;
        }
    }

    void Start()
    {
        source.Stop();
    }

    // Right-click the "Animal Audio" component header while the game is running and choose this.
    [ContextMenu("Log all playing AudioSources")]
    void LogPlayingSources()
    {
        AudioSource[] all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        int playing = 0;
        foreach (AudioSource a in all)
        {
            if (!a.isPlaying) continue;
            playing++;
            Debug.Log($"[AUDIO AUDIT] PLAYING: {a.gameObject.name} | clip: {(a.clip ? a.clip.name : "none")} | loop: {a.loop} | has AnimalAudio: {a.GetComponent<AnimalAudio>() != null}", a.gameObject);
        }
        Debug.Log($"[AUDIO AUDIT] {playing} of {all.Length} AudioSources are playing right now");
    }

    // TEST HELPER: right-click the "Animal Audio" header on a specific animal (e.g. a snow leopard) while the game is running.
    // It makes THAT animal call right now and reports its distance to your AudioListener.
    [ContextMenu("Test: make THIS animal call now")]
    void TestThisCall()
    {
        Transform l = GetListener();
        float d = l != null ? Vector3.Distance(l.position, transform.position) : -1f;
        int clips = idle != null ? idle.Length : 0;

        string verdict = clips == 0 ? "NO CLIPS in the Idle array." :
                         d < 0f ? "No AudioListener found." :
                         d <= maxDistance ? "In range, you should hear it." :
                         "TOO FAR (beyond Max Distance), you will hear nothing.";

        Debug.Log($"[ANIMAL AUDIO TEST] '{name}' (Herd Id '{herdId}') is {d:F1} units from the listener. Max Distance {maxDistance}. Clips: {clips}. {verdict}", gameObject);

        if (clips > 0)
            StartCoroutine(PlayOnce());
    }

    // TEST HELPER: right-click the "Animal Audio" header on any animal while the game is running.
    // It finds the animal closest to your AudioListener, tells you how far away it is, and makes it call right now.
    [ContextMenu("Test: make the nearest animal call now")]
    void TestNearestCall()
    {
        Transform l = GetListener();
        if (l == null)
        {
            Debug.Log("[ANIMAL AUDIO TEST] No AudioListener found in the scene.");
            return;
        }

        AnimalAudio nearest = null;
        float best = float.MaxValue;
        foreach (AnimalAudio a in FindObjectsByType<AnimalAudio>(FindObjectsSortMode.None))
        {
            if (!a.isActiveAndEnabled) continue;
            float d = Vector3.Distance(l.position, a.transform.position);
            if (d < best) { best = d; nearest = a; }
        }

        if (nearest == null)
        {
            Debug.Log("[ANIMAL AUDIO TEST] No active animals with AnimalAudio found.");
            return;
        }

        string verdict = best <= nearest.maxDistance
            ? "In range, you should hear it."
            : "TOO FAR (beyond Max Distance), you will hear nothing. Move the AudioListener/camera closer or raise Max Distance.";

        Debug.Log($"[ANIMAL AUDIO TEST] AudioListener is on '{l.name}'. Nearest animal: '{nearest.name}' at {best:F1} units. Max Distance is {nearest.maxDistance}. {verdict}", nearest.gameObject);

        nearest.StartCoroutine(nearest.PlayOnce());
    }

    // Safety net: this AudioSource may only play calls started by AnimalAudio.
    // If anything else calls Play() on it, it is already muted; here we also stop it and report it.
    void LateUpdate()
    {
        if (counted || !source.isPlaying) return;

        if (debugLog)
        {
            debugTimes.TryGetValue("foreign", out float last);
            if (Time.time > last + 3f)
            {
                debugTimes["foreign"] = Time.time;
                Debug.Log($"[ANIMAL AUDIO] Stopped a Play() that did NOT come from AnimalAudio on '{name}' | animal age: {Time.time - spawnTime:F2}s | clip: {(source.clip ? source.clip.name : "none")} | playhead: {source.time:F2}s | loop: {source.loop}", gameObject);
            }
        }

        source.Stop();
    }

    void OnEnable()
    {
        StartCoroutine(CallLoop());
    }

    void OnDisable()
    {
        ReleaseVoice();
    }

    static Transform GetListener()
    {
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the camera hands its AudioListener to an "ears" point on the
        // ground (TerrainCameraController), so use the enabled listener, not the first one found.
        if (listener == null || !listener.TryGetComponent(out AudioListener cur) || !cur.isActiveAndEnabled)
        {
            listener = null;
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l.isActiveAndEnabled) { listener = l.transform; break; }
        }
        return listener;
    }

    float GetNextCallTime()
    {
        if (!nextCallTimes.TryGetValue(herdId, out float t))
        {
            t = Time.time + Random.Range(herdGap.x, herdGap.y);
            nextCallTimes[herdId] = t;
        }
        return t;
    }

    // Cheap check every ~second. Whichever animal in earshot checks first after the
    // herd's quiet gap ends gets to call, so the herd size doesn't change how often you hear calls.
    IEnumerator CallLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

            if (debugLog) AutoAudit();

            if (CanCall())
                yield return PlayOnce();
        }
    }

    // Every ~8 seconds, report any AudioSource that is playing but was NOT started by this script.
    void AutoAudit()
    {
        if (Time.time < nextAuditTime) return;
        nextAuditTime = Time.time + 8f;

        AudioSource[] all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        int stray = 0;
        foreach (AudioSource a in all)
        {
            if (!a.isPlaying) continue;

            AnimalAudio aa = a.GetComponent<AnimalAudio>();
            if (aa != null && aa.source == a && aa.counted) continue; // started by us, fine

            stray++;
            if (stray <= 5)
                Debug.Log($"[AUDIO AUDIT] Playing but NOT started by AnimalAudio: '{a.gameObject.name}' (root: {a.transform.root.name}) | clip: {(a.clip ? a.clip.name : "none")} | loop: {a.loop} | has AnimalAudio: {aa != null}", a.gameObject);
        }

        if (stray > 0)
            Debug.Log($"[AUDIO AUDIT] {stray} stray AudioSource(s) playing out of {all.Length} total");
    }

    void Why(string reason)
    {
        if (!debugLog) return;
        debugTimes.TryGetValue(reason, out float last);
        if (Time.time < last + 5f) return;
        debugTimes[reason] = Time.time;
        Debug.Log($"[ANIMAL AUDIO] blocked ({name}): {reason}");
    }

    bool CanCall()
    {
        if (idle == null || idle.Length == 0) { Why("no clips assigned"); return false; }
        if (source.isPlaying) { Why("already playing"); return false; }
        if (activeVoices >= MaxSimultaneous) { Why("too many voices"); return false; }
        if (Time.time < lastCallTime + personalCooldown) { Why("personal cooldown"); return false; }
        if (Time.time < GetNextCallTime()) { Why("herd gap not over yet"); return false; }

        Transform l = GetListener();
        if (l == null) { Why("no AudioListener found"); return false; }

        float maxSqr = maxDistance * maxDistance;
        if ((l.position - transform.position).sqrMagnitude > maxSqr) { Why("listener too far"); return false; }

        return true;
    }

    IEnumerator PlayOnce()
    {
        lastCallTime = Time.time;

        Vector2 gap = Random.value < burstChance ? burstGap : herdGap;
        nextCallTimes[herdId] = Time.time + Random.Range(gap.x, gap.y);

        activeVoices++;
        counted = true;

        AudioClip clip = idle[Random.Range(0, idle.Length)];
        source.clip = clip;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float targetVolume = Random.Range(volumeRange.x, volumeRange.y);

        // Sound-library clips are often long recordings. Play only a short slice of it.
        float len = clip.length;
        float start = 0f;
        if (playLength.y > 0f)
        {
            len = Mathf.Min(Random.Range(playLength.x, playLength.y), clip.length);
            start = clip.length > len ? Random.Range(0f, clip.length - len) : 0f;
        }
        source.time = start;
        source.volume = 0f;
        source.mute = false;
        source.Play();

        if (debugLog)
            Debug.Log($"[ANIMAL AUDIO] PLAYING {clip.name} (clip is {clip.length:F1}s, playing {len:F1}s from {start:F1}s) on {name}");

        float elapsed = 0f;
        while (source.isPlaying && elapsed < len)
        {
            float fadeIn = Mathf.Clamp01(elapsed / 0.1f);
            float fadeOut = Mathf.Clamp01((len - elapsed) / 0.4f);
            source.volume = targetVolume * Mathf.Min(fadeIn, fadeOut);
            UpdateMuffle();

            elapsed += Time.deltaTime;
            yield return null;
        }

        source.Stop();
        ReleaseVoice();
    }

    void UpdateMuffle()
    {
        if (lowPass == null) return;
        Transform l = GetListener();
        if (l == null) return;

        float d01 = Mathf.Clamp01(Vector3.Distance(l.position, transform.position) / maxDistance);
        lowPass.cutoffFrequency = Mathf.Lerp(22000f, 1500f, d01);
    }

    void ReleaseVoice()
    {
        if (!counted) return;

        activeVoices--;
        counted = false;
        if (source != null) source.mute = true;

        // Quiet gap starts when the call ENDS, not when it began
        Vector2 gap = Random.value < burstChance ? burstGap : herdGap;
        nextCallTimes[herdId] = Time.time + Random.Range(gap.x, gap.y);
    }
}
