namespace BasicCore.Compilation;

/// <summary>
/// Represents an immutable compiled artifact snapshot that can spawn execution sessions.
/// </summary>
/// <typeparam name="TCompilationOutput">Compilation backend output type.</typeparam>
public interface ICompiledArtifact<out TCompilationOutput>
{
    /// <summary>
    /// Gets source text used to produce this artifact.
    /// </summary>
    string SourceText { get; }

    /// <summary>
    /// Gets declared external bindings snapshot used at compilation time.
    /// </summary>
    IReadOnlyList<ExternalBinding> DeclaredBindings { get; }

    /// <summary>
    /// Gets name-to-slot mapping for external bindings.
    /// </summary>
    IReadOnlyDictionary<string, int> SlotsByName { get; }

    /// <summary>
    /// Gets compiled output payload.
    /// </summary>
    TCompilationOutput CompilationOutput { get; }

    /// <summary>
    /// Creates a new execution session initialized with declared binding values.
    /// </summary>
    IExecutionEnvironment CreateSession();
}
