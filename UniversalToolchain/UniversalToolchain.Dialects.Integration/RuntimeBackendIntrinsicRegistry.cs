using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public static class RuntimeBackendIntrinsicRegistry
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        var backendIntrinsics = CreateBackendIntrinsicMap(backendRegistrars);
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

    private static IReadOnlyDictionary<DialectBackendId, SortedSet<string>> CreateBackendIntrinsicMap(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        backendRegistrars = backendRegistrars.ArgNotNull();

        var map = new SortedDictionary<DialectBackendId, SortedSet<string>>();
        foreach (var backendRegistrar in backendRegistrars
                     .Select(x => x.NotNull(nameof(backendRegistrars)))
                     .OrderBy(x => x.BackendId))
        {
            var intrinsics = SnapshotSupportedIntrinsics(backendRegistrar.BackendId, backendRegistrar.SupportedIntrinsics);
            if (!map.TryAdd(backendRegistrar.BackendId, intrinsics))
                Thrower.InvalidOpEx($"Duplicate backend provider registration for backend '{backendRegistrar.BackendId.Value}'.");
        }

        return map;
    }

    private static SortedSet<string> SnapshotSupportedIntrinsics(DialectBackendId backendId, IEnumerable<string?>? supportedIntrinsics)
    {
        var source = supportedIntrinsics
                     ?? Thrower.InvalidOpEx<IEnumerable<string?>>($"Backend '{backendId.Value}' returned null supported intrinsics.");

        var snapshot = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var intrinsic in source)
        {
            var normalized = intrinsic?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                Thrower.InvalidOpEx($"Backend '{backendId.Value}' returned an empty supported intrinsic id.");

            snapshot.Add(normalized.NotNull(nameof(supportedIntrinsics)));
        }

        return snapshot;
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