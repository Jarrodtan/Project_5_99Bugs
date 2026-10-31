/// <summary>
/// Marker for any object that can be picked by the selection system.
/// Implement this on agents, resources, or future object kinds — do not check concrete class types in the picker.
/// </summary>
public interface ISelectable
{
    /// <summary>Stable handle identifying this selectable (id + type id).</summary>
    SelectionHandle Handle { get; }
}
