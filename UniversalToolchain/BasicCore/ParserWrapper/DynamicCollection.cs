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