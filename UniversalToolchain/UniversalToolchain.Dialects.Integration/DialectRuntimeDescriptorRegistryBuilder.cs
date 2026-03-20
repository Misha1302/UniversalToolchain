using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

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

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedModulesFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverModules(assemblies))
            RegisterModule(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedModules(params Type[] types)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverModules(types))
            RegisterModule(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedOptimizersFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverOptimizers(assemblies))
            RegisterOptimizer(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedOptimizers(params Type[] types)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverOptimizers(types))
            RegisterOptimizer(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedBackendsFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverBackends(assemblies))
            RegisterBackend(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterAttributedBackends(params Type[] types)
    {
        foreach (var descriptor in DialectRuntimeDescriptorAttributeDiscovery.DiscoverBackends(types))
            RegisterBackend(descriptor);

        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterModule(RuntimeModuleDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        RegisterUniqueDescriptor(descriptor.CanonicalId, descriptor.AllNames, descriptor, _modules, _moduleNameMap, "module", static x => x.MetadataOwnerType);
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterOptimizer(RuntimeOptimizerDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        RegisterUniqueDescriptor(descriptor.CanonicalId, descriptor.AllNames, descriptor, _optimizers, _optimizerNameMap, "optimizer", static x => x.MetadataOwnerType);
        return this;
    }

    public DialectRuntimeDescriptorRegistryBuilder RegisterBackend(RuntimeBackendDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        if (_backends.ContainsKey(descriptor.BackendId))
        {
            var existing = _backends[descriptor.BackendId];
            Thrower.Argument(
                nameof(descriptor),
                $"backend canonical identifier '{descriptor.CanonicalId}' is declared by both '{existing.MetadataOwnerType.FullName}' and '{descriptor.MetadataOwnerType.FullName}'.");
        }

        _backends.Add(descriptor.BackendId, descriptor);
        RegisterNames(descriptor.AllNames, descriptor, _backendNameMap, "backend", static x => x.MetadataOwnerType);
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
        string kind,
        Func<TDescriptor, Type> getMetadataOwnerType)
    {
        if (canonicalMap.ContainsKey(canonicalId))
        {
            var existing = canonicalMap[canonicalId];
            Thrower.Argument(
                nameof(descriptor),
                $"{kind} canonical identifier '{canonicalId}' is declared by both '{getMetadataOwnerType(existing).FullName}' and '{getMetadataOwnerType(descriptor).FullName}'.");
        }

        canonicalMap.Add(canonicalId, descriptor);
        RegisterNames(allNames, descriptor, nameMap, kind, getMetadataOwnerType);
    }

    private static void RegisterNames<TDescriptor>(
        IReadOnlyList<string> names,
        TDescriptor descriptor,
        IDictionary<string, TDescriptor> nameMap,
        string kind,
        Func<TDescriptor, Type> getMetadataOwnerType)
    {
        foreach (var name in names)
        {
            if (nameMap.ContainsKey(name))
            {
                var existing = nameMap[name];
                Thrower.Argument(
                    nameof(descriptor),
                    $"{kind} alias '{name}' is declared by both '{getMetadataOwnerType(existing).FullName}' and '{getMetadataOwnerType(descriptor).FullName}'.");
            }

            nameMap.Add(name, descriptor);
        }
    }
}
