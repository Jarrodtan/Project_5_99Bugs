#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runtime TerrainData / NavMesh hold native GC handles. On Play exit the domain reloads before
/// deferred Destroy runs, which spams "Release of invalid GC handle... previous domain".
/// Flush them while the old domain is still alive.
/// </summary>
[InitializeOnLoad]
static class HimalayaPlayModeCleanup
{
    static HimalayaPlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
            return;

        var builders = Object.FindObjectsByType<HimalayaTerrainBuilder>(FindObjectsInactive.Include);
        for (var i = 0; i < builders.Length; i++)
        {
            if (builders[i] != null)
                builders[i].ReleaseRuntimeTerrainData();
        }

        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
        for (var i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] != null)
                surfaces[i].RemoveData();
        }
    }
}
#endif
