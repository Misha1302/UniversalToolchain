using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Adapts runtime manifest catalog entries into legacy runtime descriptor registry registrations.
/// </summary>
public sealed class CatalogBackedDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly IRuntimeComponentCatalog _catalog;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public CatalogBackedDialectRuntimeDescriptorProvider(
        IRuntimeComponentCatalog catalog,
        IRuntimeComponentTypeLoader typeLoader)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _typeLoader = typeLoader ?? throw new ArgumentNullException(nameof(typeLoader));
    }

    public decimal Order => 100m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        foreach (var moduleEntry in _catalog.GetModulesInDeterministicOrder())
            builder.RegisterModule(new RuntimeModuleDescriptor(moduleEntry.CanonicalAlias, _typeLoader.LoadType(moduleEntry), moduleEntry.Aliases));

        foreach (var optimizerEntry in _catalog.GetOptimizersInDeterministicOrder())
            builder.RegisterOptimizer(new RuntimeOptimizerDescriptor(optimizerEntry.CanonicalAlias, _typeLoader.LoadType(optimizerEntry), optimizerEntry.Aliases));

        foreach (var backendEntry in _catalog.GetBackendsInDeterministicOrder())
            builder.RegisterBackend(new RuntimeBackendDescriptor(new DialectBackendId(backendEntry.CanonicalAlias), typeof(CatalogBackedDialectRuntimeDescriptorProvider), backendEntry.Aliases));
    }
}
