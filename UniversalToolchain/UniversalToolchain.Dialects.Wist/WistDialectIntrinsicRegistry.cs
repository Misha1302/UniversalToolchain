using BytecodeDynamicMethodsCompiler.Compilers;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using AbstractIrConverters;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectIntrinsicRegistry
{
    public static IReadOnlyList<RuntimeIntrinsicDescriptor> CreateDescriptors()
    {
        var backendIntrinsics = new Dictionary<DialectBackendId, SortedSet<string>>
        {
            [WistDialectBackendIds.Cil] = new(new AbstractMethodsCompilerImpl().SupportedIntrinsics, StringComparer.Ordinal),
            [WistDialectBackendIds.Interpreter] = new(new AbstractIrToAbstractIrStub().SupportedIntrinsics, StringComparer.Ordinal)
        };

        var commonIntrinsics = backendIntrinsics.Values
            .Skip(1)
            .Aggregate(new SortedSet<string>(backendIntrinsics.Values.First(), StringComparer.Ordinal), (current, next) =>
            {
                current.IntersectWith(next);
                return current;
            });

        var descriptors = new List<RuntimeIntrinsicDescriptor>();
        foreach (var intrinsic in commonIntrinsics)
            descriptors.Add(new RuntimeIntrinsicDescriptor(intrinsic, DialectBackendSelector.Any));

        foreach (var backend in backendIntrinsics.OrderBy(x => x.Key))
        {
            foreach (var intrinsic in backend.Value.Except(commonIntrinsics, StringComparer.Ordinal))
                descriptors.Add(new RuntimeIntrinsicDescriptor(intrinsic, DialectBackendSelector.For(backend.Key)));
        }

        return descriptors
            .OrderBy(x => x.CanonicalId, StringComparer.Ordinal)
            .ThenBy(x => x.Target)
            .ToList();
    }
}
