namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveValidationContext
{
    private readonly Dictionary<DialectTypedStateKey, object> _sets = [];
    private readonly HashSet<string> _singletonDirectives = new(StringComparer.Ordinal);
    private readonly Dictionary<DialectTypedStateKey, object> _state = [];

    public IReadOnlySet<TValue> GetValues<TValue>(DialectSetStateKey<TValue> key) => GetOrCreateSet(key);

    public void AddValues<TValue>(DialectSetStateKey<TValue> key, IEnumerable<TValue> values, string duplicateMessage, LexemeValue? token)
    {
        if (values == null)
            Thrower.ArgumentNull(nameof(values));

        foreach (var value in values)
            AddValue(key, value, duplicateMessage, token);
    }

    public void AddValue<TValue>(DialectSetStateKey<TValue> key, TValue value, string duplicateMessage, LexemeValue? token)
    {
        if (!GetOrCreateSet(key).Add(value))
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, token);
    }

    public void EnsureSingleton(IDialectDirectiveFeature feature, LexemeValue? token)
    {
        if (feature == null)
            Thrower.ArgumentNull(nameof(feature));

        if (!_singletonDirectives.Add(feature.Id))
            DialectDefinitionSliceParseErrors.Fail(feature.SingletonViolationMessage, token);
    }

    public TState GetOrAddState<TState>(DialectValueStateKey<TState> key, Func<TState> factory) where TState : class
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));
        if (factory == null)
            Thrower.ArgumentNull(nameof(factory));

        if (_state.TryGetValue(key, out var existing))
        {
            if (existing is not TState)
                Thrower.InvalidOpEx<TState>($"Validation state '{key.Name}' has incompatible runtime type '{existing.GetType().FullName}'.");

            return (TState)existing;
        }

        var created = factory();
        if (created == null)
            Thrower.InvalidOpEx<TState>($"Validation state factory for '{key.Name}' returned null.");

        _state[key] = created;
        return created;
    }

    private HashSet<TValue> GetOrCreateSet<TValue>(DialectSetStateKey<TValue> key)
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));

        if (_sets.TryGetValue(key, out var existing))
        {
            if (existing is not HashSet<TValue>)
                Thrower.InvalidOpEx<HashSet<TValue>>($"Validation set '{key.Name}' has incompatible runtime type '{existing.GetType().FullName}'.");

            return (HashSet<TValue>)existing;
        }

        var created = new HashSet<TValue>(key.EffectiveComparer);
        _sets[key] = created;
        return created;
    }
}