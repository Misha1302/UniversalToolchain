using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class CatalogBackedDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly IRuntimeComponentCatalog _catalog;
    private readonly IEnumerable<IDialectBackendRuntimeRegistrar> _backendRegistrars;
    private readonly IRuntimeComponentTypeLoader _typeLoader;

    public CatalogBackedDialectRuntimeDescriptorProvider(
        IRuntimeComponentCatalog catalog,
        IRuntimeComponentTypeLoader typeLoader,
        IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _typeLoader = typeLoader ?? throw new ArgumentNullException(nameof(typeLoader));
        _backendRegistrars = backendRegistrars ?? throw new ArgumentNullException(nameof(backendRegistrars));
    }

    public decimal Order => 100m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        foreach (var module in _catalog.GetModulesInDeterministicOrder())
            RegisterModule(builder, module);

        foreach (var optimizer in _catalog.GetOptimizersInDeterministicOrder())
            RegisterOptimizer(builder, optimizer);

        foreach (var backend in _catalog.GetBackendsInDeterministicOrder())
            RegisterBackend(builder, backend);

        foreach (var intrinsic in RuntimeBackendIntrinsicRegistry.CreateDescriptors(_backendRegistrars))
            builder.RegisterIntrinsic(intrinsic);
    }

    private void RegisterModule(DialectRuntimeDescriptorRegistryBuilder builder, RuntimeComponentManifestEntry entry)
    {
        var implementationType = _typeLoader.LoadType(entry);
        if (!typeof(IFrontendCoreModule).IsAssignableFrom(implementationType) && !typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.InvalidOpEx($"Runtime module '{entry.CanonicalAlias}' must implement IFrontendCoreModule or IIRProcessingModule.");

        builder.RegisterModule(new RuntimeModuleDescriptor(implementationType, entry.Aliases, entry.CanonicalAlias));
    }

    private void RegisterOptimizer(DialectRuntimeDescriptorRegistryBuilder builder, RuntimeComponentManifestEntry entry)
    {
        var implementationType = _typeLoader.LoadType(entry);
        if (!typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.InvalidOpEx($"Runtime optimizer '{entry.CanonicalAlias}' must implement IIRProcessingModule.");

        builder.RegisterOptimizer(new RuntimeOptimizerDescriptor(implementationType, entry.Aliases, entry.CanonicalAlias));
    }

    private void RegisterBackend(DialectRuntimeDescriptorRegistryBuilder builder, RuntimeComponentManifestEntry entry)
    {
        var metadataOwnerType = _typeLoader.LoadType(entry);
        var descriptor = new RuntimeBackendDescriptor(new DialectBackendId(entry.CanonicalAlias), metadataOwnerType, entry.Aliases);
        builder.RegisterBackend(descriptor);
    }
}
