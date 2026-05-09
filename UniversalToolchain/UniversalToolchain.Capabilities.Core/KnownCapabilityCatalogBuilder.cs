using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Capabilities.Core;

public sealed class KnownCapabilityCatalogBuilder
{
    private readonly CapabilityProviderFactory _providerFactory;
    private readonly CapabilityProviderTypeResolver _providerTypeResolver;
    private readonly IRuntimeComponentTypeLoader? _runtimeComponentTypeLoader;

    public KnownCapabilityCatalogBuilder(
        IRuntimeComponentTypeLoader? runtimeComponentTypeLoader = null,
        CapabilityProviderTypeResolver? providerTypeResolver = null,
        CapabilityProviderFactory? providerFactory = null)
    {
        _runtimeComponentTypeLoader = runtimeComponentTypeLoader;
        _providerTypeResolver = providerTypeResolver ?? new CapabilityProviderTypeResolver();
        _providerFactory = providerFactory ?? new CapabilityProviderFactory();
    }

    public CapabilityCatalog Build(IEnumerable<Type> runtimeComponentImplementationTypes) => CapabilityCatalog.Build(runtimeComponentImplementationTypes, _providerTypeResolver, _providerFactory);

    public CapabilityCatalog Build(IRuntimeComponentCatalog runtimeComponentCatalog)
    {
        runtimeComponentCatalog = runtimeComponentCatalog.ArgNotNull();

        EnsureTypeLoaderConfigured();

        return Build(GetRuntimeComponentImplementationTypes(
            runtimeComponentCatalog.GetModulesInDeterministicOrder(),
            runtimeComponentCatalog.GetOptimizersInDeterministicOrder(),
            runtimeComponentCatalog.GetBackendsInDeterministicOrder()));
    }

    private IEnumerable<Type> GetRuntimeComponentImplementationTypes(params IEnumerable<RuntimeComponentManifestEntry>[] groups)
    {
        foreach (var entry in groups.SelectMany(static x => x))
            yield return _runtimeComponentTypeLoader!.LoadType(entry);
    }

    private void EnsureTypeLoaderConfigured()
    {
        if (_runtimeComponentTypeLoader == null)
            throw new InvalidOperationException("Runtime component type loader must be configured to build a catalog from runtime component manifests.");
    }
}