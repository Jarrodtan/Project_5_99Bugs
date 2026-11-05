using UnityEngine;
using TMPro;

/// <summary>
/// One animal row on the Configuration screen: + / - buttons or typing a number
/// sets how many of this species the simulation starts with.
/// </summary>
public class SpeciesCounter : MonoBehaviour
{
    public SpeciesDefinition species;
    [Tooltip("Box that shows the number and lets the player type one.")]
    public TMP_InputField countInput;

    [Min(0)] public int startCount = 0;
    [Min(0)] public int minCount = 0;
    [Tooltip("Keep at or below the spawner's Max Visible so every animal gets a model.")]
    [Min(0)] public int maxCount = 50;   // 100 of each jammed the NavMesh on tablets

    public int Count { get; private set; }

    void Awake()
    {
        Count = Mathf.Clamp(startCount, minCount, maxCount);
        if (countInput != null)
            countInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        Refresh();
    }

    // Hooked to the + button's OnClick.
    public void Add()
    {
        Count = Mathf.Min(Count + 1, maxCount);
        Refresh();
    }

    // Hooked to the - button's OnClick.
    public void Subtract()
    {
        Count = Mathf.Max(Count - 1, minCount);
        Refresh();
    }

    // Hooked to the input box's On End Edit (player finished typing).
    // Out-of-range numbers snap to min/max; anything unreadable keeps the old number.
    public void SetFromText(string typed)
    {
        if (int.TryParse(typed, out int value))
            Count = Mathf.Clamp(value, minCount, maxCount);
        Refresh();
    }

    public void SetCount(int count)
    {
        startCount = count;
        Count = Mathf.Clamp(count, minCount, maxCount);
        if (countInput != null)
        {
            countInput.text = Count.ToString();
        }
    }

    void Refresh()
    {
        if (countInput != null)
            countInput.SetTextWithoutNotify(Count.ToString());
    }

   
}
