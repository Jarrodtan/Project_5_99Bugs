// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line build entry points used by tools/unity.sh and tools/unity.bat, e.g.
///   Unity -batchmode -quit -projectPath apps/mobile/EcoBalance3D -executeMethod BuildScript.BuildAndroid
/// Builds every scene enabled in Build Settings into Build/&lt;Platform&gt;/ (ignored by git).
/// Exits with code 1 on failure so scripts and CI can detect it.
/// </summary>
public static class BuildScript
{
    public static void BuildAndroid() => Build(BuildTarget.Android, "Build/Android/EcoBalance.apk");

    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Windows/EcoBalance.exe");

    public static void BuildMacOS() => Build(BuildTarget.StandaloneOSX, "Build/macOS/EcoBalance.app");

    public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Build/Linux/EcoBalance.x86_64");

    static void Build(BuildTarget target, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("BuildScript: no scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None,
        });

        Debug.Log($"BuildScript: {target} -> {outputPath}: {report.summary.result} ({report.summary.totalErrors} errors)");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
// [AI-GENERATED END]
