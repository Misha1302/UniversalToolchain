namespace UniversalToolchain.Capabilities.Core;

/// <summary>
/// Materializes runtime capabilities from the exact implementation types already selected by LanguagePlan.
/// This type performs no feature, package, backend or route selection.
/// </summary>
public sealed class SelectedCapabilityCatalogBuilder
{
    private readonly CapabilityProviderFactory _providerFactory;
    private readonly CapabilityProviderTypeResolver _providerTypeResolver;

    public SelectedCapabilityCatalogBuilder(
        CapabilityProviderTypeResolver? providerTypeResolver = null,
        CapabilityProviderFactory? providerFactory = null)
    {
        _providerTypeResolver = providerTypeResolver ?? new CapabilityProviderTypeResolver();
        _providerFactory = providerFactory ?? new CapabilityProviderFactory();
    }

    public CapabilityCatalog Build(IEnumerable<Type> runtimeComponentImplementationTypes) =>
        CapabilityCatalog.Build(runtimeComponentImplementationTypes, _providerTypeResolver, _providerFactory);
}
