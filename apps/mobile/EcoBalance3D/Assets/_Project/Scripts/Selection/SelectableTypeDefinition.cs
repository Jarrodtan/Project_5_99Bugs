using UnityEngine;

/// <summary>
/// Data asset that defines a selectable object kind. The <see cref="id"/> string is stored on
/// <see cref="SelectionHandle"/> so new kinds can be added without changing selection code.
/// </summary>
[CreateAssetMenu(menuName = "EcoSim/Selectable Type", fileName = "SelectableType")]
public class SelectableTypeDefinition : ScriptableObject
{
    [Tooltip("Stable key copied onto SelectionHandle.TypeId (e.g. entity, resource). Do not rename after objects are authored.")]
    public string id;

    [Tooltip("Human-readable label for logs and future UI.")]
    public string displayName;
}
