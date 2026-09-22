using UnityEngine;
using TMPro;

public class PopulationDisplay : MonoBehaviour {
    public TMP_Text text;
    public EcosystemSimulator simulator;
    public SpeciesDefinition species;

    private float previousValue = -1f;

    public void Refresh() {
        if (!simulator.populations.ContainsKey(species)) return;

        float current = simulator.populations[species];

        // Determine direction
        string arrow = "";
        Color color = Color.white;

        if (previousValue >= 0f) {
            if (current > previousValue) {
                arrow = " ▲";
                color = Color.green;
            } else if (current < previousValue) {
                arrow = " ▼";
                color = Color.red;
            }
        }

        // Handle extinction
        if (current <= 0f) {
            text.text = $"{species.speciesName}: 0 — Extinct";
            text.color = Color.gray;
        } else {
            text.text = $"{species.speciesName}: {current:F1}{arrow}";
            text.color = color;
        }

        previousValue = current;
        
        if (simulator.populations.ContainsKey(species)) {
            text.text = $"{species.speciesName}: {simulator.populations[species]:F0}";
        }
    }
}