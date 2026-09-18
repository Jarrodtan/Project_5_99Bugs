using UnityEngine;
using UnityEditor;

public class AnimalCreator : EditorWindow
{
    string animal_names = "Blue Sheep";
    float max_health = 100.0f;
    float max_hunger = 100.0f;
    float max_thirst = 100.0f;
    float hunger_decay = 2.0f;
    float thirst_decay = 3.0f;

    string food_tag = "Grass";
    string water_tag = "Water";
    string predator_tag = "Snow Leopard";

    [MenuItem("EcoBalance/Open Animal Creator")]
    public static void show_win()
    {
        GetWindow<AnimalCreator>("Animal Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Define New EcoBalance Animal", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        animal_names = EditorGUILayout.TextField("Animal Name", animal_names);

        EditorGUILayout.Space();
        GUILayout.Label("Animal Core Stats", EditorStyles.boldLabel);
        max_health = EditorGUILayout.FloatField("Max Health", max_health);
        max_hunger = EditorGUILayout.FloatField("Max Hunger", max_hunger);
        max_thirst = EditorGUILayout.FloatField("Max Thirst", max_thirst);

        EditorGUILayout.Space();
        GUILayout.Label("Animal Decay Rates", EditorStyles.boldLabel);
        hunger_decay = EditorGUILayout.FloatField("Hunger Decay", hunger_decay);
        thirst_decay = EditorGUILayout.FloatField("Thirst Decay", thirst_decay);

        EditorGUILayout.Space();
        GUILayout.Label("Animal Relationship", EditorStyles.boldLabel);
        food_tag = EditorGUILayout.TextField("Eat", food_tag);
        water_tag = EditorGUILayout.TextField("Drink", water_tag);
        predator_tag = EditorGUILayout.TextField("Predator", predator_tag);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Data", GUILayout.Height(40)))
        {
            save_data();
        }
    }

    void save_data()
    {
        AnimalData new_data = ScriptableObject.CreateInstance<AnimalData>();

        new_data.animal_name = animal_names;
        new_data.max_health = max_health;
        new_data.max_hunger = max_hunger;
        new_data.max_thirst = max_thirst;

        new_data.food = food_tag.Replace(" ", "").Split(',');
        new_data.water = water_tag.Replace(" ", "").Split(',');
        new_data.predator = predator_tag.Replace(" ", "").Split(',');

        string path = "Assets/ScriptableObjects/";

        if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }

        string full_path = path + animal_names.Replace(" ", "") + "Data.asset";
        AssetDatabase.CreateAsset(new_data, full_path);
        AssetDatabase.SaveAssets();

        Debug.Log("Successfully created new data at: " + full_path);
    }

}
