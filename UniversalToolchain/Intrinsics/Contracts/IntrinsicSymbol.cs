namespace BasicCore.Contracts;

public readonly record struct IntrinsicSymbol(string Namespace, string Name)
{
    public override string ToString() => $"{Namespace}.{Name}";
}