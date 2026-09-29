using System;

/// <summary>
/// One label/value row for inspection UI. Labels and values come from data assets / query sources — never invented in consumers.
/// </summary>
public readonly struct StatEntry : IEquatable<StatEntry>
{
    /// <summary>Display label for this row (from schema asset).</summary>
    public string Label { get; }

    /// <summary>Display value for this row.</summary>
    public string Value { get; }

    /// <summary>
    /// Creates a stat row.
    /// </summary>
    /// <param name="label">Row label.</param>
    /// <param name="value">Row value.</param>
    public StatEntry(string label, string value)
    {
        Label = label ?? string.Empty;
        Value = value ?? string.Empty;
    }

    public bool Equals(StatEntry other) =>
        string.Equals(Label, other.Label, StringComparison.Ordinal) &&
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is StatEntry other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Label != null ? StringComparer.Ordinal.GetHashCode(Label) : 0) * 397) ^
                   (Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0);
        }
    }

    public override string ToString() => $"{Label}: {Value}";
}
