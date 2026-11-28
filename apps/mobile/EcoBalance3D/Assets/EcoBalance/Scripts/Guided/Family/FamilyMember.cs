// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - SCRUM-380
using UnityEngine;

/// <summary>Who this animal is in the Guided family (research doc: Family Interaction, Focal Animal).</summary>
public enum FamilyRole { Main, Mother, Sibling, Offspring }

/// <summary>How a family member died (for endings and story text).</summary>
public enum FamilyDeathCause { Caught, Hunger, Thirst, Other }

/// <summary>
/// Tag on a normal animal that belongs to the Guided family: its name, role and a stable id.
/// Added and removed only by <see cref="FamilyManager"/>; everything else about the animal stays normal.
/// </summary>
[DisallowMultipleComponent]
public class FamilyMember : MonoBehaviour
{
    /// <summary>Shown above the animal and in story text, e.g. "Milo", "Mum".</summary>
    public string DisplayName { get; private set; }
    public FamilyRole Role { get; private set; }
    /// <summary>Stable id for this run, e.g. "milo", "milo-mum", "milo-offspring-1" (for Save &amp; Exit later).</summary>
    public string Id { get; private set; }
    public AnimalBrain Brain { get; private set; }

    public bool IsMain => Role == FamilyRole.Main;
    public bool IsAlive => !dead && Brain != null && !Brain.IsCaught;

    bool dead;

    internal void Init(string displayName, FamilyRole role, string id)
    {
        DisplayName = displayName;
        Role = role;
        Id = id;
        Brain = GetComponent<AnimalBrain>();
    }

    internal void MarkDead() => dead = true;

    // Removed without dying (the run was reset): the manager forms the family again.
    void OnDestroy()
    {
        if (!dead) FamilyManager.OnMemberRemoved(this);
    }
}
