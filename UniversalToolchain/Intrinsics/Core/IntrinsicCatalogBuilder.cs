namespace BasicCore.Core;

/// <summary>
///     Builds the intrinsic catalog from the descriptor providers selected for a plan.
///     <para>
///         This is the only place a production catalog is built, so it owns every check that makes a catalog
///         trustworthy. The separate <c>IntrinsicDescriptorProviderMetadataValidator</c> and
///         <c>IntrinsicSemanticStartupValidator</c> restate some of these checks, but they are registered in DI
///         and never resolved (measured: zero production call sites), so they cannot be relied on. A check that
///         only exists in an uncalled validator is a check that does not run.
///     </para>
///     <para>
///         Each guard names what it prevents. A descriptor with a <c>default</c> symbol is the interesting one:
///         it is not null, so it survives a null check, and it lands in the map under the all-empty key, where it
///         shadows nothing and can never be resolved by a real invocation.
///     </para>
/// </summary>
public sealed class IntrinsicCatalogBuilder
{
    public IIntrinsicCatalog Build(IEnumerable<IIntrinsicDescriptorProvider> providers)
    {
        providers = providers.ArgNotNull();

        var orderedProviders = new List<IIntrinsicDescriptorProvider>();
        var index = 0;
        foreach (var provider in providers)
        {
            if (provider == null)
                Thrower.InvalidOpEx(
                    $"Intrinsic descriptor provider registration at index {index} is null; " +
                    "a null registration cannot contribute a catalog entry.");

            orderedProviders.Add(provider);
            index++;
        }

        orderedProviders = orderedProviders
            .OrderBy(static x => x.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        var map = new Dictionary<IntrinsicSymbol, IntrinsicSemanticDescriptor>();

        foreach (var provider in orderedProviders)
        {
            var providerDisplayName = provider.GetType().FullName ?? provider.GetType().Name;

            var fetched = provider.GetDescriptors();
            if (fetched == null)
                Thrower.InvalidOpEx(
                    $"Intrinsic descriptor provider '{providerDisplayName}' returned a null descriptor collection.");

            var descriptors = fetched
                .Select(descriptor =>
                {
                    if (descriptor == null)
                        Thrower.InvalidOpEx(
                            $"Intrinsic descriptor provider '{providerDisplayName}' returned a null descriptor; " +
                            "a null descriptor has no symbol and cannot be catalogued.");

                    return descriptor;
                })
                .OrderBy(static x => x.Symbol.Namespace, StringComparer.Ordinal)
                .ThenBy(static x => x.Symbol.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var descriptor in descriptors)
            {
                ValidateDescriptor(descriptor, providerDisplayName);

                if (!map.TryAdd(descriptor.Symbol, descriptor))
                    Thrower.InvalidOpEx($"Duplicate intrinsic semantic descriptor for symbol '{descriptor.Symbol}'.");
            }
        }

        return new IntrinsicCatalog(map);
    }

    private static void ValidateDescriptor(IntrinsicSemanticDescriptor descriptor, string providerDisplayName)
    {
        descriptor = descriptor.ArgNotNull();

        if (EqualityComparer<IntrinsicSymbol>.Default.Equals(descriptor.Symbol, default))
            Thrower.InvalidOpEx(
                $"Intrinsic descriptor provider '{providerDisplayName}' returned a descriptor with the default symbol; " +
                "an all-empty symbol compares equal to every other descriptor that also forgot to set one, " +
                "so the catalog would silently keep whichever arrived first.");

        Thrower.AssertAlways(descriptor.StackRule != null);
        Thrower.AssertAlways(descriptor.ValidationRule != null);
    }
}
