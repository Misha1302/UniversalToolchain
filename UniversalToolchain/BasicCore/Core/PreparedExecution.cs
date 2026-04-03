namespace BasicCore.Core;

internal sealed class PreparedExecution<TCompilationOutput>(
    string sourceText,
    ICompiledArtifact<TCompilationOutput> artifact,
    ICompiledArtifactSession session)
{
    public string SourceText { get; } = sourceText;

    public ICompiledArtifact<TCompilationOutput> Artifact { get; } = artifact;

    public ICompiledArtifactSession Session { get; } = session;
}