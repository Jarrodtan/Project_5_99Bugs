using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One slider or toggle already placed in the settings modal. The shell fills it from a hub field.
/// </summary>
public class ActionMenuFieldView : MonoBehaviour
{
    [Tooltip("Assigned when this row is a slider.")]
    [SerializeField]
    Slider slider;

    [Tooltip("Assigned when this row is a toggle.")]
    [SerializeField]
    Toggle toggle;

    [Tooltip("Control caption.")]
    [SerializeField]
    Text label;

    /// <summary>True when this row has a slider.</summary>
    public bool IsSlider => slider != null;

    /// <summary>True when this row has a toggle.</summary>
    public bool IsToggle => toggle != null;

    /// <summary>Shows <paramref name="text"/> on the caption.</summary>
    public void SetCaption(string text)
    {
        if (label != null)
            label.text = text ?? string.Empty;
    }

    /// <summary>Applies slider range, whole-number mode, value, and locked state.</summary>
    public void BindSlider(float minimum, float maximum, bool wholeNumbers, float value, bool locked)
    {
        if (slider == null)
            return;

        slider.minValue = minimum;
        slider.maxValue = maximum;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(value);
        slider.interactable = !locked;
    }

    /// <summary>Applies toggle value and locked state.</summary>
    public void BindToggle(bool value, bool locked)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(value);
        toggle.interactable = !locked;
    }

    /// <summary>Slider on this row, or null.</summary>
    public Slider Slider => slider;

    /// <summary>Toggle on this row, or null.</summary>
    public Toggle Toggle => toggle;
}
