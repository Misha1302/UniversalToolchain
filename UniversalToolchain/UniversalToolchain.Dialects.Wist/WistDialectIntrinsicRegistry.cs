using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectIntrinsicRegistry
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) =>
        RuntimeBackendIntrinsicRegistry.CreateDescriptors(backendRegistrars);

    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors(IEnumerable<IWistDialectBackendServiceProvider> backendProviders) =>
        RuntimeBackendIntrinsicRegistry.CreateDescriptors(backendProviders);
}
