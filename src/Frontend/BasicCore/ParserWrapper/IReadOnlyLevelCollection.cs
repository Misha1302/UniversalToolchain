namespace BasicCore.ParserWrapper;

public interface IReadOnlyLevelCollection<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>> where TKey : notnull
{
    public List<TValue> this[TKey key] { get; }
}