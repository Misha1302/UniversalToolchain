using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DialectRuntimeCatalogBuilder
{
    private readonly List<DialectRuntimeBackendDescriptor> _backends = [];
    private readonly List<DialectRuntimeModuleDescriptor> _modules = [];
    private readonly List<DialectRuntimeOptimizerDescriptor> _optimizers = [];

    public DialectRuntimeCatalogBuilder RegisterModule(DialectRuntimeModuleDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        _modules.Add(descriptor);
        return this;
    }

    public DialectRuntimeCatalogBuilder RegisterOptimizer(DialectRuntimeOptimizerDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        _optimizers.Add(descriptor);
        return this;
    }

    public DialectRuntimeCatalogBuilder RegisterBackend(DialectRuntimeBackendDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        _backends.Add(descriptor);
        return this;
    }

    public DialectRuntimeCatalog Build()
    {
        var moduleMap = BuildMap(_modules.SelectMany(x => x.AllAliases.Select(a => (Alias: a, Descriptor: x))));
        var optimizerMap = BuildMap(_optimizers.SelectMany(x => x.AllAliases.Select(a => (Alias: a, Descriptor: x))));
        var backendMap = BuildMap(_backends.SelectMany(x => x.AllAliases.Select(a => (Alias: a, Descriptor: x))));
        return new DialectRuntimeCatalog(moduleMap, optimizerMap, backendMap, _modules.AsReadOnly(), _optimizers.AsReadOnly(), _backends.AsReadOnly());
    }

    private static IReadOnlyDictionary<string, TDescriptor> BuildMap<TDescriptor>(IEnumerable<(string Alias, TDescriptor Descriptor)> pairs)
    {
        var map = new Dictionary<string, TDescriptor>(StringComparer.Ordinal);
        foreach (var (alias, descriptor) in pairs.OrderBy(x => x.Alias, StringComparer.Ordinal))
        {
            if (map.ContainsKey(alias))
                Thrower.Argument(nameof(pairs), $"Alias '{alias}' is duplicated in runtime catalog.");

            map.Add(alias, descriptor);
        }

        return map;
    }
}
