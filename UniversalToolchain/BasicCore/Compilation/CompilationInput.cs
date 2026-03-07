namespace BasicCore.Compilation;

public sealed class CompilationInput
{
    public required string SourceText { get; init; }

    public IReadOnlyList<ExternalBinding> ExternalBindings { get; init; } = [];

    public CompilationOptions Options { get; init; } = new();
}
