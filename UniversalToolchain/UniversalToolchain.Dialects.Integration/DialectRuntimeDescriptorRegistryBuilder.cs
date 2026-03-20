namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Explicit builder for runtime descriptor registration used by dialect resolution.
/// </summary>
public sealed class DialectRuntimeDescriptorRegistryBuilder
{
    private readonly Dictionary<string, RuntimeBackendDescriptor> _backendNameMap = new(StringComparer.Ordinal);
    private readonly Dictionary<DialectBackendId, RuntimeBackendDescriptor> _backends = [];
    private readonly Dictionary<string, string> _intrinsicCanonicalNames = new(StringComparer.Ordinal);
    private readonly Dictionary<(string CanonicalId, DialectBackendSelector Target), RuntimeIntrinsicDescriptor> _intrinsics = [];
    private readonly Dictionary<string, RuntimeModuleDescriptor> _moduleNameMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeModuleDescriptor> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeOptimizerDescriptor> _optimizerNameMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeOptimizerDescriptor> _optimizers = new(StringComparer.Ordinal);

    public DialectRuntimeDescriptorRegistryBuilder RegisterModule(RuntimeModuleDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        RegisterUniqueDescriptor(descriptor.CanonicalId, descriptor.AllNames, descriptor, _modules, _moduleNameMap, "module");
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterOptimizer(RuntimeOptimizerDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        RegisterUniqueDescriptor(descriptor.CanonicalId, descriptor.AllNames, descriptor, _optimizers, _optimizerNameMap, "optimizer");
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterBackend(RuntimeBackendDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (_backends.ContainsKey(descriptor.BackendId))
            Thrower.Argument(nameof(descriptor), $"Backend descriptor for '{descriptor.CanonicalId}' is already registered.");

        _backends.Add(descriptor.BackendId, descriptor);
        RegisterNames(descriptor.AllNames, descriptor, _backendNameMap, "backend");
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterIntrinsic(RuntimeIntrinsicDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        var key = (descriptor.CanonicalId, descriptor.Target);
        if (_intrinsics.ContainsKey(key))
            Thrower.Argument(nameof(descriptor), $"Intrinsic descriptor '{descriptor.CanonicalId}' for '{DialectBackendSelectorText.ToText(descriptor.Target)}' is already registered.");

        if (_intrinsicCanonicalNames.TryGetValue(descriptor.CanonicalId, out var existingCanonicalId) && existingCanonicalId != descriptor.CanonicalId)
            Thrower.Argument(nameof(descriptor), $"Intrinsic descriptor canonical identifier '{descriptor.CanonicalId}' conflicts with '{existingCanonicalId}'.");

        _intrinsicCanonicalNames[descriptor.CanonicalId] = descriptor.CanonicalId;
        foreach (var name in descriptor.AllNames)
        {
            if (_intrinsicCanonicalNames.TryGetValue(name, out existingCanonicalId) && existingCanonicalId != descriptor.CanonicalId)
                Thrower.Argument(nameof(descriptor), $"Intrinsic alias '{name}' is already assigned to '{existingCanonicalId}'.");

            _intrinsicCanonicalNames[name] = descriptor.CanonicalId;
        }

        _intrinsics.Add(key, descriptor);
        return this;
    }

    public DialectRuntimeDescriptorRegistry Build() => new(_modules, _moduleNameMap, _optimizers, _optimizerNameMap, _backends, _backendNameMap, _intrinsics, _intrinsicCanonicalNames);

    private static void RegisterUniqueDescriptor<TDescriptor>(
        string canonicalId,
        IReadOnlyList<string> allNames,
        TDescriptor descriptor,
        IDictionary<string, TDescriptor> canonicalMap,
        IDictionary<string, TDescriptor> nameMap,
        string kind)
    {
        if (canonicalMap.ContainsKey(canonicalId))
            Thrower.Argument(nameof(descriptor), $"{kind} descriptor '{canonicalId}' is already registered.");

        canonicalMap.Add(canonicalId, descriptor);
        RegisterNames(allNames, descriptor, nameMap, kind);
    }

    private static void RegisterNames<TDescriptor>(IReadOnlyList<string> names, TDescriptor descriptor, IDictionary<string, TDescriptor> nameMap, string kind)
    {
        foreach (var name in names)
        {
            if (nameMap.ContainsKey(name))
                Thrower.Argument(nameof(descriptor), $"{kind} alias '{name}' is already registered.");

            nameMap.Add(name, descriptor);
        }
    }
}
