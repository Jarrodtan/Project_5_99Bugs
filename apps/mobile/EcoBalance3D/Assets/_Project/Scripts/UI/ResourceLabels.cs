// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Adds one on-screen label per resource (e.g. "Grass: 100", "Water: 100") by copying an
// existing population label, so the new labels match its font, size and anchoring.
public class ResourceLabels : MonoBehaviour
{
    [Tooltip("Existing label to copy (e.g. the SnowLeopard population text).")]
    [SerializeField] TMP_Text template;

    [SerializeField] List<ResourceDefinition> resources = new();

    [Tooltip("Vertical gap between labels, in canvas units.")]
    [SerializeField] float spacing = 60f;

    void Start()
    {
        if (template == null) return;

        var templateRect = (RectTransform)template.transform;
        for (int i = 0; i < resources.Count; i++)
        {
            if (resources[i] == null) continue;

            TMP_Text label = Instantiate(template, template.transform.parent);
            label.name = $"{resources[i].name}Text";
            var rect = (RectTransform)label.transform;
            rect.anchoredPosition = templateRect.anchoredPosition + Vector2.down * spacing * (i + 1);

            // Reuse a copied PopulationDisplay if the template had one, otherwise add one.
            var display = label.GetComponent<PopulationDisplay>();
            if (display == null) display = label.gameObject.AddComponent<PopulationDisplay>();
            display.text = label;
            display.species = null;
            display.resource = resources[i];
            display.Refresh();
        }
    }
}
// [AI-GENERATED END]
