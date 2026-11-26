using UnityEngine;

/// <summary>
/// Peer of ObjectSelector / InspectorPanel: listens to <see cref="SelectionContext"/> and drives
/// an <see cref="ISelectionHighlightVisual"/>. Does not own selection state or picking.
/// </summary>
public class SelectionHighlighter : MonoBehaviour
{
    [Tooltip("Shared selection session to listen to.")]
    [SerializeField]
    SelectionContext selectionContext;

    [Tooltip("All visual knobs for the highlight ring.")]
    [SerializeField]
    SelectionHighlightConfig highlightConfig;

    ISelectionHighlightVisual visual;

    /// <summary>
    /// Replaces the highlight visual module. Call before Enable if customizing.
    /// </summary>
    /// <param name="highlightVisual">Visual implementation to use.</param>
    public void SetVisual(ISelectionHighlightVisual highlightVisual)
    {
        visual?.Clear();
        visual = highlightVisual;
    }

    void Awake()
    {
        if (visual == null)
            visual = new RingSelectionHighlight();
    }

    void OnEnable()
    {
        if (selectionContext == null)
        {
            Debug.LogError($"{nameof(SelectionHighlighter)} requires a {nameof(SelectionContext)}.", this);
            return;
        }

        if (highlightConfig == null)
        {
            Debug.LogError($"{nameof(SelectionHighlighter)} requires a {nameof(SelectionHighlightConfig)}.", this);
            return;
        }

        selectionContext.Changed += OnSelectionChanged;

        if (selectionContext.Current.IsValid)
            ApplyForHandle(selectionContext.Current);
        else
            visual?.Clear();
    }

    void OnDisable()
    {
        if (selectionContext != null)
            selectionContext.Changed -= OnSelectionChanged;

        visual?.Clear();
    }

    void OnSelectionChanged(SelectionChange change)
    {
        if (highlightConfig == null || visual == null)
            return;

        visual.Clear();

        if (!change.Current.IsValid)
            return;

        ApplyForHandle(change.Current);
    }

    void ApplyForHandle(SelectionHandle handle)
    {
        if (!SelectableRegistry.TryGet(handle, out var selectable))
            return;

        var behaviour = selectable as Component;
        if (behaviour == null)
            return;

        visual.Apply(behaviour.transform, highlightConfig);
    }
}
