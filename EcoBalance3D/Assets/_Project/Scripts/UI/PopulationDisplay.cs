using UnityEngine;
using TMPro;

public class PopulationDisplay : MonoBehaviour {
    public TMP_Text text;
    public EcosystemSimulator simulator;
    public SpeciesDefinition species;

    public void Refresh() {
        if (simulator.populations.ContainsKey(species)) {
            text.text = $"{species.speciesName}: {Mathf.RoundToInt(simulator.populations[species])}";
        }
    }
}