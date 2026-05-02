namespace BasicCore.Contracts;

public interface IArtifactCompiler
{
    ICompiledArtifact Compile(string code, OrderedDictionary<string, Type>? parameters = null);
    ICompiledArtifact Compile(CompilationInput input);
}

public interface IArtifactCompiler<TCompilationOutput> : IArtifactCompiler
{
    new ICompiledArtifact<TCompilationOutput> Compile(string code, OrderedDictionary<string, Type>? parameters = null);
    new ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input);
}