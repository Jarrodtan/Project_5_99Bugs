// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: EcoBalance > Build M1 Demo Scene.
/// Generates a clean Assets/Scenes/M1_Demo.unity (island, grass patches, sheep, snow leopards,
/// HUD) from the real species/resource assets and makes it the first scene in the build.
/// Safe to run again: it overwrites the demo scene, materials and prefabs it owns.
/// </summary>
public static class M1DemoSceneBuilder
{
    const string ScenePath = "Assets/Scenes/M1_Demo.unity";
    const string MaterialDir = "Assets/_Project/Art/Materials/Demo";
    const string PrefabDir = "Assets/_Project/Prefabs/Demo";
    const string SheepPath = "Assets/_Project/ScriptableObjects/Species/BlueSheep.asset";
    const string LeopardPath = "Assets/_Project/ScriptableObjects/Species/SnowLeopard.asset";
    const string GrassPath = "Assets/_Project/ScriptableObjects/Resources/Grass.asset";

    const float IslandRadius = 12f;

    static readonly Color SheepColor = new Color(0.82f, 0.84f, 0.9f);
    static readonly Color LeopardColor = new Color(0.95f, 0.65f, 0.2f);
    static readonly Color GrassColor = new Color(0.3f, 0.8f, 0.25f);

    [MenuItem("EcoBalance/Build M1 Demo Scene")]
    public static void Build()
    {
        try
        {
            BuildScene();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("M1 Demo - build failed", e.GetType().Name + ": " + e.Message + "\n\n" + e.StackTrace, "OK");
        }
    }

    static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sheep = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(SheepPath);
        var leopard = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(LeopardPath);
        var grass = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(GrassPath);
        if (sheep == null || leopard == null || grass == null)
        {
            EditorUtility.DisplayDialog("M1 Demo", "Could not find BlueSheep, SnowLeopard or Grass assets under _Project/ScriptableObjects.", "OK");
            return;
        }

        EnsureFolder(MaterialDir);
        EnsureFolder(PrefabDir);

        var islandMat = MakeMaterial("Island", new Color(0.42f, 0.55f, 0.3f));
        var grassMat = MakeMaterial("GrassPatch", GrassColor);
        var sheepMat = MakeMaterial("Sheep", SheepColor);
        var leopardMat = MakeMaterial("SnowLeopard", LeopardColor);

        var grassPrefab = MakePrefab("GrassPatch", PrimitiveType.Cylinder, new Vector3(1.2f, 0.06f, 1.2f), grassMat, typeof(GrassPatch));
        var sheepPrefab = MakePrefab("SheepAgent", PrimitiveType.Capsule, new Vector3(0.6f, 0.4f, 0.6f), sheepMat, typeof(EcoAgent));
        var leopardPrefab = MakePrefab("SnowLeopardAgent", PrimitiveType.Capsule, new Vector3(0.75f, 0.5f, 0.75f), leopardMat, typeof(EcoAgent));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 17f, -19f);
            cam.transform.LookAt(new Vector3(0f, 0f, -2f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.56f, 0.74f, 0.9f);
        }

        // Island: flat disc whose top surface is at y = 0.
        var island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        island.name = "Island";
        island.transform.position = new Vector3(0f, -0.5f, 0f);
        island.transform.localScale = new Vector3(IslandRadius * 2f + 2f, 0.5f, IslandRadius * 2f + 2f);
        island.GetComponent<Renderer>().sharedMaterial = islandMat;

        var ecoSim = new GameObject("EcoSim");
        var simulator = ecoSim.AddComponent<EcosystemSimulator>();
        simulator.species = new List<SpeciesDefinition> { sheep, leopard };
        simulator.resources = new List<ResourceDefinition> { grass };
        var controller = ecoSim.AddComponent<EcoDemoController>();
        controller.simulator = simulator;

        var grassField = new GameObject("GrassPatches").AddComponent<GrassField>();
        grassField.resource = grass;
        grassField.patchPrefab = grassPrefab;
        grassField.radius = IslandRadius - 1f;
        controller.grass = grassField;

        var animals = new GameObject("Animals").transform;
        controller.views = new List<EcoPopulationView>
        {
            MakeView(animals, simulator, sheep, sheepPrefab, false, 4f, 20),
            MakeView(animals, simulator, leopard, leopardPrefab, true, 2f, 10),
        };

        var hud = new GameObject("HUD").AddComponent<EcoDemoHud>();
        hud.controller = controller;
        hud.entries = new List<EcoDemoHud.Entry>
        {
            new EcoDemoHud.Entry { key = sheep, label = Name(sheep.speciesName, "Blue Sheep"), color = SheepColor },
            new EcoDemoHud.Entry { key = leopard, label = Name(leopard.speciesName, "Snow Leopard"), color = LeopardColor },
            new EcoDemoHud.Entry { key = grass, label = Name(grass.resourceName, "Grass"), color = GrassColor },
        };

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Demo scene first so the APK opens it; keep every other scene as it was.
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log($"M1 demo scene built at {ScenePath} and set as the first build scene. Press Play.");
    }

    static EcoPopulationView MakeView(Transform parent, EcosystemSimulator sim, SpeciesDefinition species,
        GameObject prefab, bool predator, float perVisual, int maxVisible)
    {
        var go = new GameObject(species.name);
        go.transform.SetParent(parent, false);
        var view = go.AddComponent<EcoPopulationView>();
        view.simulator = sim;
        view.species = species;
        view.agentPrefab = prefab;
        view.isPredator = predator;
        view.populationPerVisual = perVisual;
        view.maxVisible = maxVisible;
        view.spawnRadius = IslandRadius - 1f;
        return view;
    }

    static string Name(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;

    static Material MakeMaterial(string name, Color color)
    {
        string path = $"{MaterialDir}/Demo_{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject MakePrefab(string name, PrimitiveType shape, Vector3 scale, Material mat, System.Type component)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.AddComponent(component);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
// [AI-GENERATED END]
