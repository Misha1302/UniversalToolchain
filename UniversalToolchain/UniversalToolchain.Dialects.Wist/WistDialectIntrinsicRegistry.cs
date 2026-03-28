using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

[Obsolete("Use RuntimeBackendIntrinsicRegistry from UniversalToolchain.Dialects.Integration.")]
internal static class WistDialectIntrinsicRegistry
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors(IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        return RuntimeBackendIntrinsicRegistry.CreateDescriptors(backendProviders.Cast<IDialectBackendRuntimeRegistrar>());
    }
}
