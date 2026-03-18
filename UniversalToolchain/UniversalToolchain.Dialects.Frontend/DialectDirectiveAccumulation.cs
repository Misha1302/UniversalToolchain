using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveAccumulation
{
    internal static class Keys
    {
        public static DialectListStateKey<string> UseModules { get; } = new(nameof(UseModules));
        public static DialectListStateKey<string> ExcludeModules { get; } = new(nameof(ExcludeModules));
        public static DialectListStateKey<string> RequiresModules { get; } = new(nameof(RequiresModules));
        public static DialectListStateKey<string> BeforeModules { get; } = new(nameof(BeforeModules));
        public static DialectListStateKey<string> AfterModules { get; } = new(nameof(AfterModules));
        public static DialectListStateKey<string> Backends { get; } = new(nameof(Backends));
        public static DialectListStateKey<string> AllowedIntrinsics { get; } = new(nameof(AllowedIntrinsics));
        public static DialectListStateKey<string> ForbiddenIntrinsics { get; } = new(nameof(ForbiddenIntrinsics));
        public static DialectListStateKey<string> EnabledOptimizers { get; } = new(nameof(EnabledOptimizers));
        public static DialectListStateKey<string> DisabledOptimizers { get; } = new(nameof(DisabledOptimizers));
        public static DialectListStateKey<string> Capabilities { get; } = new(nameof(Capabilities));
        public static DialectValueStateKey<DialectSecurityProfile?> SecurityProfile { get; } = new(nameof(SecurityProfile));
    }

    private readonly Dictionary<DialectTypedStateKey, object> _lists = [];
    private readonly Dictionary<DialectTypedStateKey, object?> _values = [];

    public List<string> UseModules => GetOrCreateList(Keys.UseModules);

    public List<string> ExcludeModules => GetOrCreateList(Keys.ExcludeModules);

    public List<string> RequiresModules => GetOrCreateList(Keys.RequiresModules);

    public List<string> BeforeModules => GetOrCreateList(Keys.BeforeModules);

    public List<string> AfterModules => GetOrCreateList(Keys.AfterModules);

    public List<string> Backends => GetOrCreateList(Keys.Backends);

    public List<string> AllowedIntrinsics => GetOrCreateList(Keys.AllowedIntrinsics);

    public List<string> ForbiddenIntrinsics => GetOrCreateList(Keys.ForbiddenIntrinsics);

    public List<string> EnabledOptimizers => GetOrCreateList(Keys.EnabledOptimizers);

    public List<string> DisabledOptimizers => GetOrCreateList(Keys.DisabledOptimizers);

    public List<string> Capabilities => GetOrCreateList(Keys.Capabilities);

    public DialectSecurityProfile? SecurityProfile
    {
        get => GetValue(Keys.SecurityProfile);
        set => SetValue(Keys.SecurityProfile, value);
    }

    public List<TValue> GetOrCreateList<TValue>(DialectListStateKey<TValue> key)
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));

        if (_lists.TryGetValue(key, out var existing))
        {
            if (existing is not List<TValue>)
            {
                Thrower.InvalidOpEx<List<TValue>>($"Accumulation list '{key.Name}' has incompatible runtime type '{existing.GetType().FullName}'.");
            }

            return (List<TValue>)existing;
        }

        var created = new List<TValue>();
        _lists[key] = created;
        return created;
    }

    public TValue GetValue<TValue>(DialectValueStateKey<TValue> key)
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));

        if (!_values.TryGetValue(key, out var value) || value == null)
        {
            return default!;
        }

        if (value is not TValue)
        {
            Thrower.InvalidOpEx<TValue>($"Accumulation value '{key.Name}' has incompatible runtime type '{value.GetType().FullName}'.");
        }

        return (TValue)value;
    }

    public void SetValue<TValue>(DialectValueStateKey<TValue> key, TValue value)
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));
        _values[key] = value;
    }

    public void SetSingletonValue<TValue>(DialectValueStateKey<TValue> key, TValue value, string duplicateMessage)
    {
        DialectTypedStateGuards.EnsureKey(key, nameof(key));

        if (_values.ContainsKey(key))
        {
            DialectDefinitionSliceParseErrors.Fail(duplicateMessage, null);
        }

        _values[key] = value;
    }
}
