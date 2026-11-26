// [AI-GENERATED] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshes baked in the editor, so devices load the walk map instead of baking it (the slowest
/// part of the world build). Each entry is keyed by a hash of the map's inputs (config, mode,
/// family, tree/river counts), the same on every device. If the world a device builds has a key
/// that isn't here, it bakes at runtime as before, so a stale or missing entry can't break pathing.
/// Filled automatically: play each mode once in the editor and the new key is saved
/// (Resources/PrebakedNavMesh.asset), and a changed bake replaces the old one. Commit that asset before building the APK.
/// </summary>
public class PrebakedNavMesh : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string key;
        public NavMeshData data;
        public int verts;   // to spot a bake that changed after code edits
    }

    public const string ResourcePath = "PrebakedNavMesh";
    public List<Entry> entries = new();

    static PrebakedNavMesh loaded;
    static bool triedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { loaded = null; triedLoad = false; }

    public static PrebakedNavMesh Load()
    {
        if (!triedLoad) { loaded = Resources.Load<PrebakedNavMesh>(ResourcePath); triedLoad = true; }
        return loaded;
    }

    public NavMeshData Find(string key)
    {
        foreach (var e in entries)
            if (e != null && e.key == key && e.data != null) return e.data;
        return null;
    }

#if UNITY_EDITOR
    const string AssetPath = "Assets/EcoBalance/Resources/PrebakedNavMesh.asset";
    const int MaxEntries = 3;   // Guided Miloy, Guided Snowy, Free Exploration; oldest dropped

    /// <summary>Editor only: stores a copy of a freshly baked NavMesh under this key.</summary>
    public static void SaveInEditor(string key, NavMeshData baked)
    {
        if (baked == null || string.IsNullOrEmpty(key)) return;
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<PrebakedNavMesh>(AssetPath);
        if (asset == null)
        {
            asset = CreateInstance<PrebakedNavMesh>();
            UnityEditor.AssetDatabase.CreateAsset(asset, AssetPath);
        }
        // Saved once per world, so the asset doesn't change (and conflict in git) on every Play.
        // If the same world now bakes differently (code changed), the old copy is replaced on its own.
        int verts = NavMesh.CalculateTriangulation().vertices.Length;
        for (int i = asset.entries.Count - 1; i >= 0; i--)
        {
            var e = asset.entries[i];
            if (e == null || e.key != key) continue;
            if (e.data != null && e.verts == verts) return;   // unchanged
            if (e.data != null) UnityEditor.AssetDatabase.RemoveObjectFromAsset(e.data);
            asset.entries.RemoveAt(i);
        }

        while (asset.entries.Count >= MaxEntries)
        {
            var old = asset.entries[0];
            asset.entries.RemoveAt(0);
            if (old != null && old.data != null) UnityEditor.AssetDatabase.RemoveObjectFromAsset(old.data);
        }

        var copy = Instantiate(baked);
        copy.name = "NavMesh " + key;
        UnityEditor.AssetDatabase.AddObjectToAsset(copy, asset);
        asset.entries.Add(new Entry { key = key, data = copy, verts = verts });
        UnityEditor.EditorUtility.SetDirty(asset);
        UnityEditor.AssetDatabase.SaveAssets();
        loaded = asset; triedLoad = true;
        Debug.Log($"[Himalaya] Saved pre-baked NavMesh {key} to {AssetPath}. Commit it so devices skip the bake.");
    }
#endif
}
