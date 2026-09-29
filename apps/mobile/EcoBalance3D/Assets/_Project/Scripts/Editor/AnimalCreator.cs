// Scriptable Object to store data


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimalCreator : EditorWindow
{
    string animal_names = "Default Name";
    float reproduction_rate = 0.1f;
    public SpeciesDefinition fileToEdit;
    float starvation_rate = 0.2f;
    GameObject prefab;

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



        GUILayout.Label("Define New EcoBalance Species", EditorStyles.boldLabel);

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

        if (GUILayout.Button("Save Data", GUILayout.Height(40)))
        {
            save_data();
        }
    }

    // Path to store the data in a proper and neat location
    void save_data()
    {
        SpeciesDefinition new_data = ScriptableObject.CreateInstance<SpeciesDefinition>();

        new_data.speciesName = animal_names;
        new_data.reproductionRate = reproduction_rate;
        new_data.starvationThreshold = starvation_rate;
        new_data.visualPrefab = prefab;

        new_data.relationships = new List<Relationship>();

        string path = "Assets/_Project/Data/Species/";

        if (!AssetDatabase.IsValidFolder("Assets/_Project")) {
            AssetDatabase.CreateFolder("Assets", "_Project");
        }

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Species"))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Species");
        }


        string full_path = path + animal_names.Replace(" ", "") + "Data.asset";
        AssetDatabase.CreateAsset(new_data, full_path);
        AssetDatabase.SaveAssets();

        Debug.Log("Successfully created new data at: " + full_path);
    }

}
