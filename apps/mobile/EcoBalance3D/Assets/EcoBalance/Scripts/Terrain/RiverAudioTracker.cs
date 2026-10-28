using UnityEngine;
using System.Collections.Generic;

public class RiverAudioTracker : MonoBehaviour
{
    private Transform listener;
    private List<Transform> water = new List<Transform>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Camera.main != null) {
            listener = Camera.main.transform;
        }

        Invoke(nameof(FindWater), 5f);
        InvokeRepeating(nameof(TrackClosestWater), 5.5f, 0.2f);
    }

    public void FindWater ()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (GameObject obj in allObjects) {
            if (obj.name.Contains("River") || obj.name.Contains("Water") || obj.name.Contains("Drinking"))
            {
                water.Add(obj.transform);
            }
        }
    }

    public void TrackClosestWater()
    {
        if (listener == null || water.Count == 0)
        {
            return;
        }

        float closest_dist = float.MaxValue;
        Vector3 best = transform.position;

        foreach (Transform node in water) {
            if (node == null) continue;
            float dist = Vector3.Distance(listener.position, node.position);

            if (dist < closest_dist) {
                closest_dist = dist;
                best = node.position;
            }
        }

        transform.position = best;
    }
}
