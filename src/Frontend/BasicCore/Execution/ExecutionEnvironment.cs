namespace BasicCore.Execution;

public sealed class ExecutionEnvironment : IExecutionEnvironment
{
    private readonly object?[] _values;

    public ExecutionEnvironment(IReadOnlyList<ExternalBinding> bindings)
    {
        _values = new object?[bindings.Count];
        for (var i = 0; i < bindings.Count; i++)
            _values[i] = bindings[i].Value;
    }

    public object? GetExternalValue(int slot) => _values[slot];

    public void SetExternalValue(int slot, object? value) => _values[slot] = value;
}