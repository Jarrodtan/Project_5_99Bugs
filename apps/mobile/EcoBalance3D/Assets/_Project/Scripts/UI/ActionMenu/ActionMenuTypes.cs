using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Slider or toggle row inside an action-menu settings modal. Authored on <see cref="ActionMenuConfig"/>.
/// </summary>
public enum ActionMenuFieldKind
{
    Slider,
    Toggle
}

/// <summary>
/// Whether a placed bubble is a category or one of its options.
/// </summary>
public enum ActionMenuBubbleRole
{
    Category,
    Option
}

/// <summary>
/// One option row authored on a category, or built by a handler from other assets.
/// </summary>
[Serializable]
public class ActionMenuOption
{
    [Tooltip("Stable id matched to a placed option bubble's binding id.")]
    public string id = string.Empty;

    [Tooltip("Label shown on the option bubble. Handlers may overwrite this from source assets.")]
    public string label = string.Empty;

    [Tooltip("Optional icon. Leave empty to hide the icon image.")]
    public Sprite icon;
}

/// <summary>
/// One settings control for a category modal.
/// </summary>
[Serializable]
public class ActionMenuField
{
    [Tooltip("Stable id. Handlers read runtime values by this id.")]
    public string id = string.Empty;

    [Tooltip("Label shown beside the control.")]
    public string label = string.Empty;

    [Tooltip("Slider or toggle.")]
    public ActionMenuFieldKind kind;

    [Tooltip("Slider minimum.")]
    public float minimum;

    [Tooltip("Slider maximum.")]
    public float maximum = 1f;

    [Tooltip("Slider value used until the player moves the control.")]
    public float sliderDefault;

    [Tooltip("When true, the slider only accepts whole numbers.")]
    public bool wholeNumbers;

    [Tooltip("Toggle value used until the player changes it.")]
    public bool toggleDefault;

    [Tooltip("When true, the control is shown but cannot be changed.")]
    public bool locked;
}

/// <summary>
/// A visual spawned from a category that does not use the species list.
/// </summary>
[Serializable]
public class ActionMenuPrefabOption
{
    [Tooltip("Stable id matched to a placed option bubble.")]
    public string id = string.Empty;

    [Tooltip("Label shown on the option bubble.")]
    public string label = string.Empty;

    [Tooltip("Visual spawned when this option is chosen.")]
    public GameObject prefab;
}

/// <summary>
/// One category in the action menu hub: identity, static options, species refs, and modal fields.
/// </summary>
[Serializable]
public class ActionMenuCategory
{
    [Tooltip("Stable id matched to a placed category bubble and to a handler's category id.")]
    public string id = string.Empty;

    [Tooltip("Label shown on the category bubble and the settings modal title.")]
    public string label = string.Empty;

    [Tooltip("Optional icon. Leave empty to hide the icon image.")]
    public Sprite icon;

    [Tooltip("When false, this category and its options stay in the hub but are hidden.")]
    public bool enabled = true;

    [Tooltip("Static options for handlers that do not build a list from another catalog.")]
    public List<ActionMenuOption> options = new();

    [Tooltip("Species used by the entity handler when its category id matches this entry.")]
    public List<SpeciesDefinition> species = new();

    [Tooltip("Prefab options used when this category has no species. The entity handler spawns these the same way.")]
    public List<ActionMenuPrefabOption> prefabOptions = new();

    [Tooltip("Settings modal controls, in display order.")]
    public List<ActionMenuField> fields = new();
}

/// <summary>
/// Runtime copy of modal values. Not written back to <see cref="ActionMenuConfig"/>.
/// </summary>
public class ActionMenuSettings
{
    readonly Dictionary<string, float> _sliders = new();
    readonly Dictionary<string, bool> _toggles = new();

    /// <summary>Stores a slider value for <paramref name="fieldId"/>.</summary>
    public void SetSlider(string fieldId, float value)
    {
        if (string.IsNullOrEmpty(fieldId))
            return;

        _sliders[fieldId] = value;
    }

    /// <summary>Returns the slider value for <paramref name="fieldId"/>, or zero when unset.</summary>
    public float GetSlider(string fieldId)
    {
        if (string.IsNullOrEmpty(fieldId))
            return 0f;

        return _sliders.TryGetValue(fieldId, out var value) ? value : 0f;
    }

    /// <summary>Stores a toggle value for <paramref name="fieldId"/>.</summary>
    public void SetToggle(string fieldId, bool value)
    {
        if (string.IsNullOrEmpty(fieldId))
            return;

        _toggles[fieldId] = value;
    }

    /// <summary>Returns the toggle value for <paramref name="fieldId"/>, or false when unset.</summary>
    public bool GetToggle(string fieldId)
    {
        if (string.IsNullOrEmpty(fieldId))
            return false;

        return _toggles.TryGetValue(fieldId, out var value) && value;
    }
}
