// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Batch-mode entry point for the freeze check:
///   Unity.exe -batchmode -projectPath ... -executeMethod FreezeProbeRunner.Run -freezeProbe -logFile probe.log
/// Opens the Himalaya scene and enters Play Mode; FreezeProbe runs the 100 + 100 world and exits Unity.
/// </summary>
public static class FreezeProbeRunner
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HimalayaSimulation.unity");
        EditorApplication.EnterPlaymode();
    }
}
// [AI-GENERATED END]
