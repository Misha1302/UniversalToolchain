namespace BasicCore.Execution;

/// <summary>
///     Default implementation of <see cref="ICompiledArtifactSession" /> for compiled artifacts.
/// </summary>
public sealed class CompiledArtifactSession<TCompilationOutput> : ICompiledArtifactSession
{
    private readonly ICompiledArtifact<TCompilationOutput> _artifact;
    private readonly IExecutionEnvironment _executionEnvironment;
    private readonly IExecutor<TCompilationOutput> _executor;

    public CompiledArtifactSession(
        ICompiledArtifact<TCompilationOutput> artifact,
        IExecutor<TCompilationOutput> executor,
        IExecutionEnvironment executionEnvironment)
    {
        artifact = artifact.ArgNotNull();

        executor = executor.ArgNotNull();

        executionEnvironment = executionEnvironment.ArgNotNull();

        _artifact = artifact;
        _executor = executor;
        _executionEnvironment = executionEnvironment;
    }

    public int ArgumentCount => _artifact.DeclaredBindings.Count;

    public void SetArgument(int slot, object? value)
    {
        ValidateAssignment(slot, value);
        _executionEnvironment.SetExternalValue(slot, value);
    }

    public void SetArgument(string name, object? value)
    {
        name = name.ArgNotNull();

        if (!_artifact.SlotsByName.TryGetValue(name, out var slot))
            Thrower.Argument(nameof(name), $"Unknown argument name '{name}'.");

        SetArgument(slot, value);
    }

    public object? Run() => _executor.Execute(_artifact.CompilationOutput, _executionEnvironment);

    private void ValidateAssignment(int slot, object? value)
    {
        if (slot < 0 || slot >= _artifact.DeclaredBindings.Count)
            Thrower.ArgumentOutOfRange<object>(nameof(slot), $"Argument slot '{slot}' is out of range [0, {_artifact.DeclaredBindings.Count - 1}].");

        var binding = _artifact.DeclaredBindings[slot];
        if (binding.Kind == ExternalBindingKind.Constant)
            Thrower.InvalidOpEx(
                $"Binding '{binding.Name}' at slot {slot} is constant and cannot be reassigned.");

        EnsureAssignable(binding, value, slot, binding.Name);
    }

    private static void EnsureAssignable(ExternalBinding binding, object? value, int slot, string name)
    {
        if (value == null)
        {
            if (binding.Type.IsValueType && Nullable.GetUnderlyingType(binding.Type) == null)
                Thrower.Argument(
                    nameof(value),
                    $"Null cannot be assigned to non-nullable value-type argument '{name}' at slot {slot} ({binding.Type.Name}).");

            return;
        }

        var valueType = value.GetType();
        if (!binding.Type.IsAssignableFrom(valueType))
            Thrower.Argument(
                nameof(value),
                $"Value of type '{valueType}' is not assignable to argument '{name}' at slot {slot} with declared type '{binding.Type}'.");
    }
}