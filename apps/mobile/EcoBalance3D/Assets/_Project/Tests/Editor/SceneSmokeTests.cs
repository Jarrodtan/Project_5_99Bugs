// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Smoke tests (rubric: "smoke tests"): every scene in the build opens without errors and without
// missing scripts, and the simulation scene is wired to real species/resources. Catches a deleted
// script, a broken prefab link or an empty simulator slot before it reaches the tablet. Runs in
// EditMode inside the normal CI job, so it costs seconds, not a second Unity run.
public class SceneSmokeTests
{
    static IEnumerable<string> BuildScenes() =>
        EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);

    [TearDown]
    public void TearDown() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void BuildSettings_HasScenes()
    {
        Assert.IsNotEmpty(BuildScenes().ToList(), "no enabled scenes in Build Settings");
    }

    [TestCaseSource(nameof(BuildScenes))]
    public void Scene_OpensWithoutMissingScripts(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), $"{path} did not open");

        var missing = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    missing.Add(PathOf(t));
        Assert.IsEmpty(missing, $"{path}: missing scripts on\n  " + string.Join("\n  ", missing));
    }

    [Test]
    public void SimulationScene_SimulatorIsWiredToRealAssets()
    {
        var path = BuildScenes().FirstOrDefault(p => p.EndsWith("HimalayaSimulation.unity"));
        Assume.That(path, Is.Not.Null, "HimalayaSimulation is not in Build Settings");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        var sims = Object.FindObjectsByType<EcosystemSimulator>(FindObjectsSortMode.None);
        Assert.AreEqual(1, sims.Length, "expected exactly one EcosystemSimulator in HimalayaSimulation");
        var sim = sims[0];
        Assert.IsNotEmpty(sim.species, "simulator has no species");
        Assert.IsTrue(sim.species.All(s => s != null), "simulator has an empty species slot");
        Assert.IsTrue(sim.resources == null || sim.resources.All(r => r != null), "simulator has an empty resource slot");
        foreach (var s in sim.species)
            Assert.IsTrue(s.relationships == null || s.relationships.All(r => r != null && r.Target != null),
                $"{s.name} has a relationship with no target");
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
// [AI-GENERATED END]
