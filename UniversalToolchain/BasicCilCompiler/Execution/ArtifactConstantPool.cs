namespace BasicCilCompiler.Execution;

public sealed class ArtifactConstantPool
{
    private readonly object[] _values;

    public ArtifactConstantPool(IEnumerable<object> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values.ToArray();
    }

    public int Count => _values.Length;

    public T GetValue<T>(int index) => (T)_values[index];
}
