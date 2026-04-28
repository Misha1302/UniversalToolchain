namespace BasicCore.Compilation;

/// <summary>
///     Default implementation of <see cref="ICompiledArtifact{TCompilationOutput}" /> with fixed artifact structure.
///     Declared bindings and slots mapping are snapshotted at construction time and stay unchanged afterwards.
///     Binding values are copied by reference; no generic deep clone is performed.
///     Mutable object graphs referenced by binding values can still be mutated externally.
/// </summary>
/// <typeparam name="TCompilationOutput">Compilation backend output type.</typeparam>
public sealed class CompiledArtifact<TCompilationOutput> : ICompiledArtifact<TCompilationOutput>
{
    private readonly ExternalBinding[] _declaredBindings;
    private readonly IExecutor<TCompilationOutput> _executor;
    private readonly ExternalBindingsLayout _externalBindingsLayout;
    private readonly IReadOnlyList<Type> _allowedRuntimeProviderTypes;

    public CompiledArtifact(
        string sourceText,
        IReadOnlyList<ExternalBinding> declaredBindings,
        TCompilationOutput compilationOutput,
        IExecutor<TCompilationOutput> executor,
        IReadOnlyList<Type>? allowedRuntimeProviderTypes = null)
    {
        sourceText = sourceText.ArgNotNull();

        declaredBindings = declaredBindings.ArgNotNull();

        executor = executor.ArgNotNull();

        _declaredBindings = SnapshotBindings(declaredBindings);
        _externalBindingsLayout = ExternalBindingsLayout.FromDeclaredBindings(_declaredBindings);
        SlotsByName = _externalBindingsLayout.SlotsByName;
        _executor = executor;
        _allowedRuntimeProviderTypes = allowedRuntimeProviderTypes?.ToList() ?? [];

        SourceText = sourceText;
        CompilationOutput = compilationOutput;
    }

    public string SourceText { get; }

    public IReadOnlyList<ExternalBinding> DeclaredBindings => _declaredBindings;

    public IReadOnlyDictionary<string, int> SlotsByName { get; }

    public TCompilationOutput CompilationOutput { get; }

    public ICompiledArtifactSession CreateSession() => new CompiledArtifactSession<TCompilationOutput>(
        this,
        _executor,
        new ExecutionEnvironment(_declaredBindings, _externalBindingsLayout, _allowedRuntimeProviderTypes));

    private static ExternalBinding[] SnapshotBindings(IReadOnlyList<ExternalBinding> declaredBindings)
    {
        var snapshot = new ExternalBinding[declaredBindings.Count];

        for (var i = 0; i < declaredBindings.Count; i++)
        {
            var binding = declaredBindings[i];
            if (binding is null)
                Thrower.Argument(nameof(declaredBindings), "Declared bindings must not contain null entries.");

            if (string.IsNullOrWhiteSpace(binding.Name))
                Thrower.Argument(nameof(declaredBindings), "Declared binding name must not be empty.");

            if (binding.Type is null)
                Thrower.Argument(nameof(declaredBindings), "Declared binding type must not be null.");

            snapshot[i] = new ExternalBinding
            {
                Name = binding.Name,
                Type = binding.Type,
                Value = binding.Value,
                Kind = binding.Kind
            };
        }

        return snapshot;
    }
}
