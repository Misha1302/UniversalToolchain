using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectIntrinsicRegistry
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors(IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        var backendIntrinsics = CreateBackendIntrinsicMap(backendProviders);
        var commonIntrinsics = GetCommonIntrinsics(backendIntrinsics);
        var descriptors = new List<RuntimeIntrinsicDescriptor>();

        foreach (var intrinsic in commonIntrinsics)
            descriptors.Add(new RuntimeIntrinsicDescriptor(intrinsic, DialectBackendSelector.Any));

        foreach (var backend in backendIntrinsics)
        {
            foreach (var intrinsic in backend.Value.Except(commonIntrinsics, StringComparer.Ordinal))
                descriptors.Add(new RuntimeIntrinsicDescriptor(intrinsic, DialectBackendSelector.For(backend.Key)));
        }

        return descriptors
            .OrderBy(x => x.CanonicalId, StringComparer.Ordinal)
            .ThenBy(x => x.Target)
            .ToList();
    }

    private static IReadOnlyDictionary<DialectBackendId, SortedSet<string>> CreateBackendIntrinsicMap(IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        var map = new SortedDictionary<DialectBackendId, SortedSet<string>>();
        foreach (var backendProvider in backendProviders
                     .Select(x => x.NotNull(nameof(backendProviders)))
                     .OrderBy(x => x.BackendId))
        {
            var intrinsics = new SortedSet<string>(backendProvider.SupportedIntrinsics ?? Thrower.InvalidOpEx<IReadOnlyList<string>>($"Backend '{backendProvider.BackendId.Value}' returned null supported intrinsics."), StringComparer.Ordinal);
            if (!map.TryAdd(backendProvider.BackendId, intrinsics))
                Thrower.InvalidOpEx($"Duplicate Wist backend service provider registration for backend '{backendProvider.BackendId.Value}'.");
        }

        return map;
    }

    private static SortedSet<string> GetCommonIntrinsics(IReadOnlyDictionary<DialectBackendId, SortedSet<string>> backendIntrinsics)
    {
        var commonIntrinsics = new SortedSet<string>(StringComparer.Ordinal);
        if (backendIntrinsics.Count == 0)
            return commonIntrinsics;

        commonIntrinsics.UnionWith(backendIntrinsics.First().Value);
        foreach (var backend in backendIntrinsics.Skip(1))
            commonIntrinsics.IntersectWith(backend.Value);

        return commonIntrinsics;
    }
}
