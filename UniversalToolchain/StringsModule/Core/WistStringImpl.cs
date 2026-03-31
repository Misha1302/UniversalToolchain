namespace StringsModule.Core;

public readonly struct WistStringImpl(string value) : IComparable<WistStringImpl>
{
    public string GetValue() => value;

    public static WistStringImpl Create(string value) => new(value);

    public static WistStringImpl Add(WistStringImpl a, WistStringImpl b) => new(string.Concat(a.GetValue(), b.GetValue()));

    public int CompareTo(WistStringImpl other) => string.CompareOrdinal(GetValue(), other.GetValue());

    public override string ToString() => value;
}
