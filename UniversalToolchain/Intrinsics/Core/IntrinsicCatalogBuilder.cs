using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicCatalogBuilder
{
    public IIntrinsicCatalog Build(IEnumerable<IIntrinsicDescriptorProvider> providers)
    {
        if (providers == null)
            Thrower.ArgumentNull(nameof(providers));

        var orderedProviders = providers
            .Select(x => x.NotNull(nameof(providers)))
            .OrderBy(x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        var map = new Dictionary<IntrinsicSymbol, IntrinsicSemanticDescriptor>();

        foreach (var provider in orderedProviders)
        {
            var descriptors = provider.GetDescriptors()
                .Select(x => x.NotNull(nameof(providers)))
                .OrderBy(x => x.Symbol.Namespace, StringComparer.Ordinal)
                .ThenBy(x => x.Symbol.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var descriptor in descriptors)
            {
                ValidateDescriptor(descriptor);

                if (!map.TryAdd(descriptor.Symbol, descriptor))
                    Thrower.InvalidOpEx($"Duplicate intrinsic semantic descriptor for symbol '{descriptor.Symbol}'.");
            }
        }

        return new IntrinsicCatalog(map);
    }

    private static void ValidateDescriptor(IntrinsicSemanticDescriptor descriptor)
    {
        if (descriptor == null)
            Thrower.ArgumentNull(nameof(descriptor));

        Thrower.AssertAlways(descriptor.StackRule != null);
        Thrower.AssertAlways(descriptor.ValidationRule != null);
    }
}
