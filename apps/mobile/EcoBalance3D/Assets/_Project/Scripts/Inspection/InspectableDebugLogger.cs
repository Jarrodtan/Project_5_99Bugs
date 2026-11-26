using System.Text;
using UnityEngine;

/// <summary>
/// Type-agnostic verification logger: prints <see cref="IInspectable"/> stats on selection change.
/// Not part of production UI — remove when Inspector Panel exists.
/// </summary>
public class InspectableDebugLogger : MonoBehaviour
{
    [Tooltip("Shared selection session to listen to.")]
    [SerializeField]
    SelectionContext selectionContext;

    [Tooltip("Inspection knobs (logStatsOnSelection).")]
    [SerializeField]
    InspectionConfig inspectionConfig;

    void OnEnable()
    {
        if (selectionContext == null)
        {
            Debug.LogError($"{nameof(InspectableDebugLogger)} requires a {nameof(SelectionContext)}.", this);
            return;
        }

        selectionContext.Changed += OnSelectionChanged;
    }

    void OnDisable()
    {
        if (selectionContext != null)
            selectionContext.Changed -= OnSelectionChanged;
    }

    void OnSelectionChanged(SelectionChange change)
    {
        if (inspectionConfig != null && !inspectionConfig.logStatsOnSelection)
            return;

        if (!change.Current.IsValid)
        {
            Debug.Log("[Inspection] Cleared");
            return;
        }

        if (!SelectableRegistry.TryGet(change.Current, out var selectable))
        {
            Debug.LogWarning($"[Inspection] No selectable registered for {change.Current}");
            return;
        }

        var behaviour = selectable as Component;
        if (behaviour == null)
        {
            Debug.LogWarning("[Inspection] Selectable is not a Component; cannot resolve IInspectable.");
            return;
        }

        var inspectable = behaviour.GetComponentInParent<IInspectable>();
        if (inspectable == null)
        {
            Debug.LogWarning($"[Inspection] No IInspectable on {behaviour.name}");
            return;
        }

        var stats = inspectable.GetStats();
        var sb = new StringBuilder();
        sb.Append("[Inspection] ").Append(inspectable.GetDisplayName());
        if (stats != null)
        {
            for (var i = 0; i < stats.Count; i++)
            {
                sb.Append(" | ").Append(stats[i].Label).Append('=').Append(stats[i].Value);
            }
        }

        Debug.Log(sb.ToString());
    }
}
