using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigurationScreen : MonoBehaviour
{
    public SpeciesCounter[] counters;

    [Tooltip("Scene containing the ecosystem.")]
    public string simulationSceneName = "SampleScene";

    [Tooltip("The whole configuration menu that disappears after Start.")]
    public GameObject configurationUI;

    private void Start()
    {
        // Pause the game while the player is configuring the ecosystem.
        Time.timeScale = 0f;

        // Load the ecosystem behind the configuration screen.
        if (!SceneManager.GetSceneByName(simulationSceneName).isLoaded)
        {
            SceneManager.LoadScene(
                simulationSceneName,
                LoadSceneMode.Additive
            );
        }
    }

    public void StartSimulation()
    {
        // --------------------------------
        // 1. Save chosen populations
        // --------------------------------

        EcosystemSimulator.StartingPopulationOverrides.Clear();

        foreach (var counter in counters)
        {
            if (counter != null && counter.species != null)
            {
                EcosystemSimulator.StartingPopulationOverrides[
                    counter.species.name
                ] = counter.Count;
            }
        }

        // --------------------------------
        // 2. Reset ecosystem
        // --------------------------------

        EcosystemSimulator simulator =
            FindAnyObjectByType<EcosystemSimulator>();

        if (simulator != null)
        {
            simulator.ResetSimulation();
        }

        // --------------------------------
        // 3. Resume the game
        // --------------------------------

        Time.timeScale = 1f;

        // --------------------------------
        // 4. Hide configuration UI
        // --------------------------------

        if (configurationUI != null)
        {
            configurationUI.SetActive(false);
        }
    }
}
