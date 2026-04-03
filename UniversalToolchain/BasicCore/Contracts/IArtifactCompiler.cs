namespace BasicCore.Contracts;

public interface IArtifactCompiler<TCompilationOutput>
{
    ICompiledArtifact<TCompilationOutput> Compile(string code, OrderedDictionary<string, Type>? parameters = null);
    ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input);
}