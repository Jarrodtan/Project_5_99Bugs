using UnityEngine;

/// <summary>
/// Base type for optional island content. Concrete subclasses are their own assets
/// and are assigned on <see cref="IslandDefinition.modules"/>.
/// </summary>
public abstract class IslandModule : ScriptableObject
{
}
