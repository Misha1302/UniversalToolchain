// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
namespace BasicCore.ParserWrapper;

public class DynamicCollection
{
    private readonly Dictionary<string, object?> _collection = [];

    public T Get<T>(string key)
    {
        return (T)_collection[key]!;
    }

    public void Set<T>(string key, T value)
    {
        _collection[key] = value;
    }
}