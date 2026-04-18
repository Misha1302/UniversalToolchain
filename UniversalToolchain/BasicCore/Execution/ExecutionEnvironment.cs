namespace BasicCore.Execution;

public sealed class ExecutionEnvironment : IExecutionEnvironment, IExternalBindingsLayoutProvider
{
    private readonly object?[] _values;

    public ExecutionEnvironment(IReadOnlyList<ExternalBinding> bindings, ExternalBindingsLayout? externalBindingsLayout = null)
    {
        bindings = bindings.ArgNotNull();

        _values = new object?[bindings.Count];
        for (var i = 0; i < bindings.Count; i++)
            _values[i] = bindings[i].Value;

        ExternalBindingsLayout = externalBindingsLayout ?? ExternalBindingsLayout.FromDeclaredBindings(bindings);
    }

    public object? GetExternalValue(int slot) => _values[slot];

    public void SetExternalValue(int slot, object? value) => _values[slot] = value;

    public ExternalBindingsLayout ExternalBindingsLayout { get; }
}