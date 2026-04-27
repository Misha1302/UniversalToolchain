using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Capabilities.Core;

public sealed class SelectedCapabilityCatalogBuilder
{
    private readonly CapabilityProviderFactory _providerFactory;
    private readonly CapabilityProviderTypeResolver _providerTypeResolver;
    private readonly IRuntimeComponentTypeLoader? _runtimeComponentTypeLoader;

    public SelectedCapabilityCatalogBuilder(
        IRuntimeComponentTypeLoader? runtimeComponentTypeLoader = null,
        CapabilityProviderTypeResolver? providerTypeResolver = null,
        CapabilityProviderFactory? providerFactory = null)
    {
        _runtimeComponentTypeLoader = runtimeComponentTypeLoader;
        _providerTypeResolver = providerTypeResolver ?? new CapabilityProviderTypeResolver();
        _providerFactory = providerFactory ?? new CapabilityProviderFactory();
    }

    public CapabilityCatalog Build(IEnumerable<Type> runtimeComponentImplementationTypes)
    {
        return CapabilityCatalog.Build(runtimeComponentImplementationTypes, _providerTypeResolver, _providerFactory);
    }

    public CapabilityCatalog Build(SelectedRuntimePlan selectedRuntimePlan)
    {
        ArgumentNullException.ThrowIfNull(selectedRuntimePlan);

        EnsureTypeLoaderConfigured();

        return Build(selectedRuntimePlan.OrderedModules
            .Concat(selectedRuntimePlan.EnabledOptimizers)
            .Concat(selectedRuntimePlan.EnabledBackends)
            .Select(x => _runtimeComponentTypeLoader!.LoadType(x)));
    }

    private void EnsureTypeLoaderConfigured()
    {
        if (_runtimeComponentTypeLoader == null)
            throw new InvalidOperationException("Runtime component type loader must be configured to build a catalog from a selected runtime plan.");
    }
}
