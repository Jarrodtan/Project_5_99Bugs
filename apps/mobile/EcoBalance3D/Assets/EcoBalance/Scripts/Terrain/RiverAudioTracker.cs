using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Put this on an EMPTY GameObject (not the camera) that also has an AudioSource with your river clip.
/// The object slides along the river to the point nearest the AudioListener, so the sound gets
/// louder as you approach the water and fades to silence as you move away.
[RequireComponent(typeof(AudioSource))]
public class RiverAudioTracker : MonoBehaviour
{
    [Header("Find the river")]
    public string waterNameContains = "Water Patch";
    public int samplePointsPerPatch = 80;      // how many points of each patch mesh to track

    [Header("3D sound")]
    public float minDistance = 2f;             // full volume inside this distance
    public float maxDistance = 40f;            // silent beyond this distance
    [Range(0f, 1f)] public float volume = 1f;
    public float followSpeed = 80f;            // how fast the sound slides along the river (units/sec)

    [Header("Debug")]
    public bool debugLog = true;               // turn off once it works

    AudioSource source;
    Transform listener;
    readonly List<Vector3> points = new List<Vector3>();
    readonly List<Renderer> fallbackRenderers = new List<Renderer>();
    Vector3 target;
    bool hasTarget;
    float nextDebugTime;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;              // fully 3D
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;   // reaches true silence at maxDistance
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
        source.mute = false;
    }

    void Start()
    {
        if (source.clip == null)
            Debug.LogWarning("[RIVER AUDIO] The AudioSource has no clip assigned.", this);

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // 1. Wait for an AudioListener (normally on the Main Camera)
        while (listener == null)
        {
            AudioListener l = FindAnyObjectByType<AudioListener>();
            if (l != null) listener = l.transform;
            else yield return new WaitForSeconds(0.5f);
        }

        // 2. The river is spawned by script, so keep looking until the patches exist
        while (points.Count == 0 && fallbackRenderers.Count == 0)
        {
            FindWater();
            if (points.Count == 0 && fallbackRenderers.Count == 0)
                yield return new WaitForSeconds(2f);
        }

        if (debugLog)
            Debug.Log($"[RIVER AUDIO] Found water: {points.Count} sample points (+{fallbackRenderers.Count} unreadable patches). Listener is on '{listener.name}'.");

        if (source.clip != null && !source.isPlaying)
            source.Play();

        // 3. Keep the sound on the nearest river point
        while (true)
        {
            UpdateTarget();
            yield return new WaitForSeconds(0.2f);
        }
    }

    void Update()
    {
        if (hasTarget)
            transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
    }

    void FindWater()
    {
        points.Clear();
        fallbackRenderers.Clear();

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.Contains(waterNameContains)) continue;

            MeshFilter mf = t.GetComponent<MeshFilter>();
            Mesh m = mf != null ? mf.sharedMesh : null;

            if (m != null && m.isReadable && m.vertexCount > 0)
            {
                // Use the real mesh points (patch objects often sit at 0,0,0 with world-space vertices)
                Vector3[] v = m.vertices;
                int step = Mathf.Max(1, v.Length / Mathf.Max(1, samplePointsPerPatch));
                for (int i = 0; i < v.Length; i += step)
                    points.Add(t.TransformPoint(v[i]));
            }
            else
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null) fallbackRenderers.Add(r);
                else points.Add(t.position);
            }
        }
    }

    void UpdateTarget()
    {
        if (listener == null)
        {
            AudioListener l = FindAnyObjectByType<AudioListener>();
            if (l == null) return;
            listener = l.transform;
        }

        Vector3 p = listener.position;
        float bestSqr = float.MaxValue;
        Vector3 best = transform.position;

        for (int i = 0; i < points.Count; i++)
        {
            float d = (points[i] - p).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = points[i]; }
        }

        for (int i = 0; i < fallbackRenderers.Count; i++)
        {
            if (fallbackRenderers[i] == null) continue;
            Vector3 c = fallbackRenderers[i].bounds.ClosestPoint(p);
            float d = (c - p).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c; }
        }

        if (bestSqr == float.MaxValue) return;

        target = best;
        hasTarget = true;

        if (debugLog && Time.time > nextDebugTime)
        {
            nextDebugTime = Time.time + 5f;
            Debug.Log($"[RIVER AUDIO] Nearest water is {Mathf.Sqrt(bestSqr):F1} units away (silent beyond {maxDistance}). Playing: {source.isPlaying}");
        }
    }
}
