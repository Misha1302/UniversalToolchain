namespace BasicCore.Contracts;

/// <summary>
/// Provides a reusable public contract for compiling source text into immutable artifacts.
/// </summary>
/// <typeparam name="TCompilationOutput">Compilation backend output type.</typeparam>
public interface IArtifactCompiler<out TCompilationOutput>
{
    /// <summary>
    /// Compiles source text with optional declared external bindings.
    /// </summary>
    ICompiledArtifact<TCompilationOutput> Compile(string code, OrderedDictionary<string, Type>? parameters = null);

    /// <summary>
    /// Compiles an explicit compilation input.
    /// </summary>
    ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input);
}
