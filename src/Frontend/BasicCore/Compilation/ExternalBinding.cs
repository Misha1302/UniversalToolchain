namespace BasicCore.Compilation;

public sealed class ExternalBinding
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public object? Value { get; init; }
    public ExternalBindingKind Kind { get; init; } = ExternalBindingKind.Variable;
}