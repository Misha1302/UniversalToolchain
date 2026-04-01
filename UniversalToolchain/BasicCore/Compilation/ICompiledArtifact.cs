namespace BasicCore.Compilation;

/// <summary>
/// Represents a compiled artifact snapshot that can spawn execution sessions.
/// The artifact structure is fixed after creation: <see cref="DeclaredBindings"/> order/content and
/// <see cref="SlotsByName"/> mapping do not change.
/// Binding values are copied by reference and are not deep-cloned.
/// If <see cref="ExternalBinding.Value"/> references a mutable object graph, that graph can still be mutated externally.
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
    /// Binding entries and their order are stable for the lifetime of the artifact.
    /// <see cref="ExternalBinding.Value"/> references are preserved as-is and are not deep-cloned.
    /// </summary>
    IReadOnlyList<ExternalBinding> DeclaredBindings { get; }

    /// <summary>
    /// Gets name-to-slot mapping for external bindings.
    /// The mapping is created once from declared bindings and is stable for the artifact lifetime.
    /// </summary>
    IReadOnlyDictionary<string, int> SlotsByName { get; }

    /// <summary>
    /// Gets compiled output payload.
    /// </summary>
    TCompilationOutput CompilationOutput { get; }

    /// <summary>
    /// Creates a new execution session initialized with declared binding values.
    /// </summary>
    ICompiledArtifactSession CreateSession();
}
