using System;

/// <summary>
/// Stable identity for a selectable object. Stores an integer id plus a type id string
/// from a <see cref="SelectableTypeDefinition"/> — never a live GameObject or component reference.
/// </summary>
public readonly struct SelectionHandle : IEquatable<SelectionHandle>
{
    /// <summary>Numeric instance id. Zero is reserved for <see cref="None"/>.</summary>
    public int Id { get; }

    /// <summary>Stable type key from <see cref="SelectableTypeDefinition.id"/>.</summary>
    public string TypeId { get; }

    /// <summary>True when both id and type id identify a real selectable.</summary>
    public bool IsValid => Id != 0 && !string.IsNullOrEmpty(TypeId);

    /// <summary>Empty handle meaning nothing is selected.</summary>
    public static SelectionHandle None => default;

    /// <summary>
    /// Creates a handle from an instance id and a type id string.
    /// </summary>
    /// <param name="id">Non-zero instance id.</param>
    /// <param name="typeId">Non-empty type key (e.g. "entity").</param>
    public SelectionHandle(int id, string typeId)
    {
        Id = id;
        TypeId = typeId ?? string.Empty;
    }

    public bool Equals(SelectionHandle other) =>
        Id == other.Id && string.Equals(TypeId, other.TypeId, StringComparison.Ordinal);

    public override bool Equals(object obj) =>
        obj is SelectionHandle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Id * 397) ^ (TypeId != null ? StringComparer.Ordinal.GetHashCode(TypeId) : 0);
        }
    }

    public static bool operator ==(SelectionHandle left, SelectionHandle right) => left.Equals(right);

    public static bool operator !=(SelectionHandle left, SelectionHandle right) => !left.Equals(right);

    public override string ToString() =>
        IsValid ? $"SelectionHandle(Id={Id}, TypeId={TypeId})" : "SelectionHandle.None";
}
