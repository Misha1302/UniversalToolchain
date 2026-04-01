namespace BasicCore.Execution;

/// <summary>
/// Default implementation of <see cref="ICompiledArtifactSession"/> for compiled artifacts.
/// </summary>
public sealed class CompiledArtifactSession<TCompilationOutput> : ICompiledArtifactSession
{
    private readonly TCompilationOutput _compilationOutput;
    private readonly IExecutor<TCompilationOutput> _executor;
    private readonly IExecutionEnvironment _executionEnvironment;
    private readonly IReadOnlyList<ExternalBinding> _bindings;
    private readonly IReadOnlyDictionary<string, int> _slotsByName;

    public CompiledArtifactSession(
        TCompilationOutput compilationOutput,
        IExecutor<TCompilationOutput> executor,
        IExecutionEnvironment executionEnvironment,
        IReadOnlyList<ExternalBinding> bindings)
    {
        if (executor == null)
            Thrower.ArgumentNull(nameof(executor));

        if (executionEnvironment == null)
            Thrower.ArgumentNull(nameof(executionEnvironment));

        if (bindings == null)
            Thrower.ArgumentNull(nameof(bindings));

        _compilationOutput = compilationOutput;
        _executor = executor;
        _executionEnvironment = executionEnvironment;
        _bindings = bindings;
        _slotsByName = CreateSlotsByName(bindings);
    }

    public int ArgumentCount => _bindings.Count;

    public void SetArgument(int slot, object? value)
    {
        if (slot < 0 || slot >= _bindings.Count)
            Thrower.ArgumentOutOfRange<object>(nameof(slot), $"Argument slot '{slot}' is out of range [0, {_bindings.Count - 1}].");

        var binding = _bindings[slot];
        EnsureAssignable(binding, value, slot, binding.Name);
        _executionEnvironment.SetExternalValue(slot, value);
    }

    public void SetArgument(string name, object? value)
    {
        if (name == null)
            Thrower.ArgumentNull(nameof(name));

        if (!_slotsByName.TryGetValue(name, out var slot))
            Thrower.Argument(nameof(name), $"Unknown argument name '{name}'.");

        SetArgument(slot, value);
    }

    public object? Run() => _executor.Execute(_compilationOutput, _executionEnvironment);

    private static IReadOnlyDictionary<string, int> CreateSlotsByName(IReadOnlyList<ExternalBinding> bindings)
    {
        var slotsByName = new Dictionary<string, int>(bindings.Count, StringComparer.Ordinal);
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];

            if (string.IsNullOrWhiteSpace(binding.Name))
                Thrower.Argument(nameof(bindings), $"Binding at slot {i} must have a non-empty name.");

            if (!slotsByName.TryAdd(binding.Name, i))
                Thrower.Argument(nameof(bindings), $"Binding name '{binding.Name}' is duplicated.");
        }

        return slotsByName;
    }

    private static void EnsureAssignable(ExternalBinding binding, object? value, int slot, string name)
    {
        if (value == null)
        {
            if (binding.Type.IsValueType && Nullable.GetUnderlyingType(binding.Type) == null)
            {
                Thrower.Argument(
                    nameof(value),
                    $"Null cannot be assigned to non-nullable value-type argument '{name}' at slot {slot} ({binding.Type.Name}).");
            }

            return;
        }

        var valueType = value.GetType();
        if (!binding.Type.IsAssignableFrom(valueType))
        {
            Thrower.Argument(
                nameof(value),
                $"Value of type '{valueType}' is not assignable to argument '{name}' at slot {slot} with declared type '{binding.Type}'.");
        }
    }
}
