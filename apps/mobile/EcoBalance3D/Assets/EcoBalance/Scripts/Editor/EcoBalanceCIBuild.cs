// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI entry point for a Development (debug) Android build, used when "Run workflow" is started with
/// buildType = development. Release builds keep GameCI's default build method.
/// A development build includes the debug overlay (SimDebugOverlay) and lets the Unity profiler attach.
/// </summary>
public static class EcoBalanceCIBuild
{
    public static void BuildAndroidDevelopment()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string output = Arg("-customBuildPath") ?? "build/Android/EcoBalance.apk";
        Debug.Log($"[CIBuild] Development build of {scenes.Length} scenes -> {output}");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development,
        });

        Debug.Log($"[CIBuild] {report.summary.result}: {report.summary.totalErrors} errors, " +
                  $"{report.summary.totalWarnings} warnings, {report.summary.totalSize / 1048576} MB");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    /// <summary>
    /// Linux player for the headless soak test on the team VM (run with -batchmode -nographics -soakDays N;
    /// see HeadlessSoakRunner). A normal Linux player, so no extra Dedicated Server module is needed.
    /// </summary>
    public static void BuildLinuxHeadless()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string output = Arg("-customBuildPath") ?? "build/StandaloneLinux64/EcoBalanceHeadless.x86_64";
        Debug.Log($"[CIBuild] Linux headless build of {scenes.Length} scenes -> {output}");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        });

        Debug.Log($"[CIBuild] {report.summary.result}: {report.summary.totalErrors} errors, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    static string Arg(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
// [AI-GENERATED END]
