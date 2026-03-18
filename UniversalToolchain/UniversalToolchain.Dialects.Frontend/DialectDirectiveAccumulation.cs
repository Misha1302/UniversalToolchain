using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveAccumulation
{
    private readonly Dictionary<string, List<string>> _lists = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public List<string> UseModules => GetOrCreateList(nameof(UseModules));

    public List<string> ExcludeModules => GetOrCreateList(nameof(ExcludeModules));

    public List<string> RequiresModules => GetOrCreateList(nameof(RequiresModules));

    public List<string> BeforeModules => GetOrCreateList(nameof(BeforeModules));

    public List<string> AfterModules => GetOrCreateList(nameof(AfterModules));

    public List<string> Backends => GetOrCreateList(nameof(Backends));

    public List<string> AllowedIntrinsics => GetOrCreateList(nameof(AllowedIntrinsics));

    public List<string> ForbiddenIntrinsics => GetOrCreateList(nameof(ForbiddenIntrinsics));

    public List<string> EnabledOptimizers => GetOrCreateList(nameof(EnabledOptimizers));

    public List<string> DisabledOptimizers => GetOrCreateList(nameof(DisabledOptimizers));

    public List<string> Capabilities => GetOrCreateList(nameof(Capabilities));

    public DialectSecurityProfile? SecurityProfile
    {
        get => GetValue<DialectSecurityProfile?>(nameof(SecurityProfile));
        set => _values[nameof(SecurityProfile)] = value;
    }

    public List<string> GetOrCreateList(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Thrower.Argument(nameof(key), "Accumulation key must not be empty.");
        }

        if (_lists.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new List<string>();
        _lists[key] = created;
        return created;
    }

    public T GetValue<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Thrower.Argument(nameof(key), "Accumulation value key must not be empty.");
        }

        if (!_values.TryGetValue(key, out var value) || value == null)
        {
            return default!;
        }

        if (value is not T)
        {
            Thrower.InvalidOpEx<T>($"Accumulation value '{key}' has incompatible runtime type '{value.GetType().FullName}'.");
        }

        return (T)value;
    }
}
