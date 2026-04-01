namespace BasicCore.Compilation;

/// <summary>
/// Default immutable implementation of <see cref="ICompiledArtifact{TCompilationOutput}"/>.
/// </summary>
/// <typeparam name="TCompilationOutput">Compilation backend output type.</typeparam>
public sealed class CompiledArtifact<TCompilationOutput> : ICompiledArtifact<TCompilationOutput>
{
    private readonly ExternalBinding[] _declaredBindings;
    private readonly IReadOnlyDictionary<string, int> _slotsByName;

    public CompiledArtifact(string sourceText, IReadOnlyList<ExternalBinding> declaredBindings, TCompilationOutput compilationOutput)
    {
        if (sourceText is null)
            Thrower.ArgumentNull(nameof(sourceText));

        if (declaredBindings is null)
            Thrower.ArgumentNull(nameof(declaredBindings));

        _declaredBindings = SnapshotBindings(declaredBindings);
        _slotsByName = BuildSlots(_declaredBindings);

        SourceText = sourceText;
        CompilationOutput = compilationOutput;
    }

    public string SourceText { get; }

    public IReadOnlyList<ExternalBinding> DeclaredBindings => _declaredBindings;

    public IReadOnlyDictionary<string, int> SlotsByName => _slotsByName;

    public TCompilationOutput CompilationOutput { get; }

    public IExecutionEnvironment CreateSession() => new ExecutionEnvironment(_declaredBindings);

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

    private static IReadOnlyDictionary<string, int> BuildSlots(IReadOnlyList<ExternalBinding> declaredBindings)
    {
        var slots = new Dictionary<string, int>(declaredBindings.Count, StringComparer.Ordinal);

        for (var i = 0; i < declaredBindings.Count; i++)
        {
            var name = declaredBindings[i].Name;
            if (!slots.TryAdd(name, i))
                Thrower.Argument(nameof(declaredBindings), $"Declared binding '{name}' is duplicated.");
        }

        return slots;
    }
}
