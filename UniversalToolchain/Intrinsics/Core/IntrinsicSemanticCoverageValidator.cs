using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicSemanticCoverageValidator
{
    public IReadOnlyList<string> Validate(
        IEnumerable<Type> registeredProviderTypes,
        IEnumerable<(Type ModuleType, Type ProviderType)> coverageRequirements)
    {
        if (registeredProviderTypes == null)
            Thrower.ArgumentNull(nameof(registeredProviderTypes));

        if (coverageRequirements == null)
            Thrower.ArgumentNull(nameof(coverageRequirements));

        var errors = new List<string>();
        var providerTypeSet = registeredProviderTypes
            .Select(x => x.NotNull(nameof(registeredProviderTypes)))
            .ToHashSet();
        var orderedRequirements = coverageRequirements
            .Select(x => (x.ModuleType.NotNull(nameof(coverageRequirements)), x.ProviderType.NotNull(nameof(coverageRequirements))))
            .Distinct()
            .OrderBy(x => x.Item1.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.Item2.FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var requirement in orderedRequirements)
        {
            var moduleType = requirement.Item1;
            var providerType = requirement.Item2;
            var moduleDisplayName = moduleType.FullName ?? moduleType.Name;
            var providerDisplayName = providerType.FullName ?? providerType.Name;

            if (!providerTypeSet.Contains(providerType))
                errors.Add($"Module '{moduleDisplayName}' requires intrinsic descriptor provider '{providerDisplayName}', but it is not registered.");
        }

        return errors;
    }

    public IReadOnlyList<string> Validate(
        IIntrinsicCatalog catalog,
        IEnumerable<IIntrinsicDescriptorProvider> providers,
        IEnumerable<(Type ModuleType, Type ProviderType)> coverageRequirements)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        if (providers == null)
            Thrower.ArgumentNull(nameof(providers));

        if (coverageRequirements == null)
            Thrower.ArgumentNull(nameof(coverageRequirements));

        var errors = new List<string>();
        var providerMap = providers
            .Select(x => x.NotNull(nameof(providers)))
            .GroupBy(static x => x.GetType())
            .ToDictionary(static x => x.Key, static x => x.First());
        var orderedRequirements = coverageRequirements
            .Select(x => (x.ModuleType.NotNull(nameof(coverageRequirements)), x.ProviderType.NotNull(nameof(coverageRequirements))))
            .Distinct()
            .OrderBy(x => x.Item1.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.Item2.FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var requirement in orderedRequirements)
        {
            var moduleType = requirement.Item1;
            var providerType = requirement.Item2;
            var moduleDisplayName = moduleType.FullName ?? moduleType.Name;
            var providerDisplayName = providerType.FullName ?? providerType.Name;

            if (!providerMap.TryGetValue(providerType, out var provider))
            {
                errors.Add($"Module '{moduleDisplayName}' requires intrinsic descriptor provider '{providerDisplayName}', but it is not registered.");
                continue;
            }

            var descriptors = provider.GetDescriptors()
                .Where(static x => x != null)
                .Where(static x => !EqualityComparer<IntrinsicSymbol>.Default.Equals(x.Symbol, default))
                .OrderBy(x => x.Symbol.Namespace, StringComparer.Ordinal)
                .ThenBy(x => x.Symbol.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var descriptor in descriptors)
            {
                if (!catalog.TryResolve(descriptor.Symbol, out _))
                {
                    errors.Add(
                        $"Module '{moduleDisplayName}' expects symbol '{descriptor.Symbol}' from provider '{providerDisplayName}', but the symbol is missing from the intrinsic catalog.");
                }
            }
        }

        return errors;
    }
}
