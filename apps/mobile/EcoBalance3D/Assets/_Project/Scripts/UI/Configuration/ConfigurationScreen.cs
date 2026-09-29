using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Start button on the Configuration screen: hands each counter's number to the
/// simulator and opens the simulation scene, where the spawners create that many animals.
/// </summary>
public class ConfigurationScreen : MonoBehaviour
{
    public SpeciesCounter[] counters;

    [Tooltip("Scene to open on Start. It must be listed in File > Build Profiles.")]
    public string simulationSceneName = "SampleScene";

    // Hooked to the Start button's OnClick.
    public void StartSimulation()
    {
        EcosystemSimulator.StartingPopulationOverrides.Clear();
        foreach (var counter in counters)
        {
            if (counter != null && counter.species != null)
                EcosystemSimulator.StartingPopulationOverrides[counter.species.name] = counter.Count;
        }

        SceneManager.LoadScene(simulationSceneName);
    }
}
