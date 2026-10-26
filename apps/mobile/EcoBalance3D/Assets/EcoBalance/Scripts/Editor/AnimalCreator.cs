// Scriptable Object to store data
// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong:
// saves next to the game's real species, updates a loaded file instead of copying it, never silently
// overwrites, and can add the species to the open scene (simulator + its own spawner) so it actually appears.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class AnimalCreator : EditorWindow
{
    string animal_names = "Default Name";
    float reproduction_rate = 0.1f;
    public SpeciesDefinition fileToEdit;
    float starvation_rate = 0.2f;
    GameObject prefab;
    SpeciesDefinition last_saved;

    [MenuItem("EcoBalance/Open Animal Creator")]
    public static void show_win()
    {
        GetWindow<AnimalCreator>("Species Creator");
    }

    // Custom Window for the datas & values
    void OnGUI()
    {
        GUILayout.Label("Edit Existing Species", EditorStyles.boldLabel);
        fileToEdit = (SpeciesDefinition)EditorGUILayout.ObjectField("Load File", fileToEdit, typeof(SpeciesDefinition), false);

        if (GUILayout.Button("Load Data"))
        {
            if (fileToEdit != null)
            {
                animal_names = fileToEdit.speciesName;
                reproduction_rate = fileToEdit.reproductionRate;
                starvation_rate = fileToEdit.starvationThreshold;
                prefab = fileToEdit.visualPrefab;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label(fileToEdit != null ? $"Editing: {fileToEdit.name}" : "Define New EcoBalance Species", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        animal_names = EditorGUILayout.TextField("Species Name", animal_names);

        EditorGUILayout.Space();
        GUILayout.Label("Simulation Stats", EditorStyles.boldLabel);
        reproduction_rate = EditorGUILayout.FloatField("Reproduction Rate", reproduction_rate);
        starvation_rate = EditorGUILayout.FloatField("Starvation Rate", starvation_rate);

        EditorGUILayout.Space();
        GUILayout.Label("Visuals", EditorStyles.boldLabel);
        prefab = (GameObject)EditorGUILayout.ObjectField("Visual Prefab", prefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        if (GUILayout.Button(fileToEdit != null ? "Save Changes" : "Save New Species", GUILayout.Height(40)))
        {
            save_data();
        }

        if (fileToEdit != null && GUILayout.Button("Clear (start a new species)"))
        {
            fileToEdit = null;
            animal_names = "Default Name";
            prefab = null;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Put it in the game", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Adds the species to the open scene's simulator and gives it its own spawner (a copy of an existing one). " +
            "Set its Eats / Needs on the species asset and an AnimalBrain on its prefab so it can move and eat.",
            MessageType.Info);
        var target = fileToEdit != null ? fileToEdit : last_saved;
        using (new EditorGUI.DisabledScope(target == null || prefab == null))
        {
            if (GUILayout.Button("Add to open scene", GUILayout.Height(32)))
                add_to_scene(target);
        }
    }

    // Path to store the data in a proper and neat location: the same folder as the game's existing species.
    static string species_folder()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:SpeciesDefinition"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("/ScriptableObjects/Species/")) return Path.GetDirectoryName(p).Replace('\\', '/');
        }
        return "Assets/_Project/ScriptableObjects/Species";
    }

    void save_data()
    {
        if (string.IsNullOrWhiteSpace(animal_names))
        {
            EditorUtility.DisplayDialog("Species Creator", "Give the species a name first.", "OK");
            return;
        }

        // Editing a loaded file: change that file, don't make a copy.
        if (fileToEdit != null)
        {
            Undo.RecordObject(fileToEdit, "Edit species");
            apply_fields(fileToEdit);
            EditorUtility.SetDirty(fileToEdit);
            AssetDatabase.SaveAssets();
            last_saved = fileToEdit;
            Debug.Log("Saved changes to: " + AssetDatabase.GetAssetPath(fileToEdit));
            return;
        }

        SpeciesDefinition new_data = ScriptableObject.CreateInstance<SpeciesDefinition>();
        apply_fields(new_data);
        new_data.relationships = new List<Relationship>();
        new_data.needs = new List<NeedDefinition>();
        new_data.goals = new List<GoalDefinition>();

        string folder = species_folder();
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogError("Species folder not found: " + folder);
            return;
        }

        // Never overwrite an existing species with the same name: Unity adds " 1", " 2", ...
        string full_path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{animal_names.Replace(" ", "")}.asset");
        AssetDatabase.CreateAsset(new_data, full_path);
        AssetDatabase.SaveAssets();
        last_saved = new_data;
        EditorGUIUtility.PingObject(new_data);

        Debug.Log("Successfully created new data at: " + full_path);
    }

    void apply_fields(SpeciesDefinition data)
    {
        data.speciesName = animal_names;
        data.reproductionRate = reproduction_rate;
        data.starvationThreshold = starvation_rate;
        data.visualPrefab = prefab;
    }

    // Registers the species with the scene's simulator and clones an existing spawner (with its clock) for it.
    static void add_to_scene(SpeciesDefinition species)
    {
        var sim = Object.FindFirstObjectByType<EcosystemSimulator>();
        var template = Object.FindFirstObjectByType<CreatureSpawner>();
        if (sim == null || template == null)
        {
            EditorUtility.DisplayDialog("Species Creator", "Open the simulation scene (with a simulator and a spawner) first.", "OK");
            return;
        }

        Undo.RecordObject(sim, "Add species");
        if (sim.species == null) sim.species = new List<SpeciesDefinition>();
        if (!sim.species.Contains(species)) sim.species.Add(species);

        var copy = (GameObject)Object.Instantiate(template.gameObject, template.transform.parent);
        copy.name = species.speciesName.Replace(" ", "") + "Spawner";
        Undo.RegisterCreatedObjectUndo(copy, "Add species spawner");
        var spawner = copy.GetComponent<CreatureSpawner>();
        spawner.species = species;

        // The spawner refreshes from a SimulationClock. If the template's clock lives on another object,
        // give the copy its own clock with the same settings.
        foreach (var clock in Object.FindObjectsByType<SimulationClock>(FindObjectsSortMode.None))
        {
            if (clock.spawner != template || clock.gameObject == template.gameObject) continue;
            var own = copy.AddComponent<SimulationClock>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(clock), own);
            own.spawner = spawner;
            break;
        }
        var ownClock = copy.GetComponent<SimulationClock>();
        if (ownClock != null) ownClock.spawner = spawner;

        EditorSceneManager.MarkSceneDirty(copy.scene);
        Selection.activeGameObject = copy;
        Debug.Log($"Added {species.speciesName} to the scene with spawner '{copy.name}'. Save the scene to keep it.");
    }
}
