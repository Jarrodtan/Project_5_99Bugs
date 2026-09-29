using UnityEngine;
using TMPro;

public class PopulationDisplay : MonoBehaviour {
    public TMP_Text text;
    public EcosystemSimulator simulator;
    public SpeciesDefinition species;

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    [Tooltip("Optional. Set this instead of Species to show a resource amount, e.g. Grass.")]
    public ResourceDefinition resource;

    // Keeps the label current even if no SimulationClock references this display.
    void Update() => Refresh();
    // [AI-GENERATED END]

    public void Refresh() {
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        if (text == null) return;
        if (resource != null) {
            string label = string.IsNullOrEmpty(resource.resourceName) ? resource.name : resource.resourceName;
            text.text = $"{label}: {Mathf.RoundToInt(resource.currentAmount)}";
            return;
        }
        if (simulator == null || species == null) return;
        // [AI-GENERATED END]
        if (simulator.populations.ContainsKey(species)) {
            text.text = $"{species.speciesName}: {Mathf.RoundToInt(simulator.populations[species])}";
        }
    }
}
