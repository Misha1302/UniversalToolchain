namespace BasicCore.Execution;

/// <summary>
/// Default implementation of <see cref="ICompiledArtifactSession"/> for compiled artifacts.
/// </summary>
public sealed class CompiledArtifactSession<TCompilationOutput> : ICompiledArtifactSession
{
    private readonly ICompiledArtifact<TCompilationOutput> _artifact;
    private readonly IExecutor<TCompilationOutput> _executor;
    private readonly IExecutionEnvironment _executionEnvironment;

    public CompiledArtifactSession(
        ICompiledArtifact<TCompilationOutput> artifact,
        IExecutor<TCompilationOutput> executor,
        IExecutionEnvironment executionEnvironment)
    {
        if (artifact == null)
            Thrower.ArgumentNull(nameof(artifact));

        if (executor == null)
            Thrower.ArgumentNull(nameof(executor));

        if (executionEnvironment == null)
            Thrower.ArgumentNull(nameof(executionEnvironment));

        _artifact = artifact;
        _executor = executor;
        _executionEnvironment = executionEnvironment;
    }

    public int ArgumentCount => _artifact.DeclaredBindings.Count;

    public void SetArgument(int slot, object? value)
    {
        if (slot < 0 || slot >= _artifact.DeclaredBindings.Count)
            Thrower.ArgumentOutOfRange<object>(nameof(slot), $"Argument slot '{slot}' is out of range [0, {_artifact.DeclaredBindings.Count - 1}].");

        var binding = _artifact.DeclaredBindings[slot];
        EnsureAssignable(binding, value, slot, binding.Name);
        _executionEnvironment.SetExternalValue(slot, value);
    }

    public void SetArgument(string name, object? value)
    {
        if (name == null)
            Thrower.ArgumentNull(nameof(name));

        if (!_artifact.SlotsByName.TryGetValue(name, out var slot))
            Thrower.Argument(nameof(name), $"Unknown argument name '{name}'.");

        SetArgument(slot, value);
    }

    public object? Run() => _executor.Execute(_artifact.CompilationOutput, _executionEnvironment);

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
