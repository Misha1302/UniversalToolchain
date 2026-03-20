namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines named boolean capabilities for a dialect.
/// </summary>
public sealed class CapabilityPolicy
{
    private readonly ReadOnlyDictionary<string, bool> _capabilities;

    public CapabilityPolicy(IEnumerable<KeyValuePair<string, bool>>? capabilities = null)
    {
        var dictionary = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (capabilities != null)
            foreach (var capability in capabilities)
            {
                if (string.IsNullOrWhiteSpace(capability.Key))
                    Thrower.Argument(nameof(capabilities), "Capability name must not be null or empty.");

                dictionary[capability.Key] = capability.Value;
            }

        _capabilities = new ReadOnlyDictionary<string, bool>(dictionary);
    }

    public IReadOnlyDictionary<string, bool> Capabilities => _capabilities;

    public bool TryGetCapability(string capabilityName, out bool enabled)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
            Thrower.Argument(nameof(capabilityName), "Capability name must not be null or empty.");

        return _capabilities.TryGetValue(capabilityName, out enabled);
    }
}