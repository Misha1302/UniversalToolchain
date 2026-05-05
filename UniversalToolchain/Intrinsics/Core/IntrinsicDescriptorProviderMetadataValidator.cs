using ObjectExtensions;

namespace BasicCore.Core;

public sealed class IntrinsicDescriptorProviderMetadataValidator
{
    public IReadOnlyList<string> Validate(IServiceCollection services)
    {
        services = services.ArgNotNull();

        var descriptors = services
            .Select(static (descriptor, index) => new RegistrationEntry(index, descriptor))
            .Where(static x => x.Descriptor.ServiceType == typeof(IIntrinsicDescriptorProvider))
            .OrderBy(static x => GetRegistrationProviderType(x.Descriptor)?.FullName, StringComparer.Ordinal)
            .ThenBy(static x => x.Index)
            .ToList();

        var errors = new List<string>();
        var providerRegistrationCounts = new Dictionary<Type, int>();

        foreach (var entry in descriptors)
        {
            var descriptor = entry.Descriptor;
            var providerType = GetRegistrationProviderType(descriptor);

            if (providerType != null)
            {
                if (!typeof(IIntrinsicDescriptorProvider).IsAssignableFrom(providerType))
                {
                    var providerDisplayName = providerType.FullName ?? providerType.Name;
                    errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' does not implement IIntrinsicDescriptorProvider.");
                    continue;
                }

                providerRegistrationCounts.TryGetValue(providerType, out var currentCount);
                providerRegistrationCounts[providerType] = currentCount + 1;
            }
        }

        foreach (var duplicateProviderRegistration in providerRegistrationCounts
                     .Where(static x => x.Value > 1)
                     .OrderBy(static x => x.Key.FullName, StringComparer.Ordinal))
        {
            var providerDisplayName = duplicateProviderRegistration.Key.FullName ?? duplicateProviderRegistration.Key.Name;
            errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' is registered {duplicateProviderRegistration.Value} times.");
        }

        return errors;
    }

    public IReadOnlyList<string> Validate(IEnumerable<IIntrinsicDescriptorProvider> providers)
    {
        providers = providers.ArgNotNull();

        var errors = new List<string>();
        var providerEntries = providers
            .Select(static (provider, index) => new ProviderEntry(index, provider))
            .OrderBy(static x => x.Provider?.GetType().FullName, StringComparer.Ordinal)
            .ThenBy(static x => x.Index)
            .ToList();
        var providerRegistrationCounts = new Dictionary<Type, int>();
        var symbolOwners = new Dictionary<IntrinsicSymbol, string>();

        foreach (var entry in providerEntries)
        {
            if (entry.Provider == null)
            {
                errors.Add($"Intrinsic descriptor provider registration at index {entry.Index} is null.");
                continue;
            }

            var providerType = entry.Provider.GetType();
            var providerDisplayName = providerType.FullName ?? providerType.Name;

            if (!typeof(IIntrinsicDescriptorProvider).IsAssignableFrom(providerType))
            {
                errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' does not implement IIntrinsicDescriptorProvider.");
                continue;
            }

            providerRegistrationCounts.TryGetValue(providerType, out var currentCount);
            providerRegistrationCounts[providerType] = currentCount + 1;

            IReadOnlyList<IntrinsicSemanticDescriptor> descriptors;
            try
            {
                descriptors = entry.Provider.GetDescriptors();
            }
            catch (Exception ex)
            {
                errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' failed to enumerate descriptors: {ex.Message}");
                continue;
            }

            if (descriptors.MakeNullable() == null)
            {
                errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' returned a null descriptor collection.");
                continue;
            }

            for (var descriptorIndex = 0; descriptorIndex < descriptors.Count; descriptorIndex++)
            {
                var descriptor = descriptors[descriptorIndex];
                if (descriptor.MakeNullable() == null)
                {
                    errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' returned a null descriptor at index {descriptorIndex}.");
                    continue;
                }

                if (EqualityComparer<IntrinsicSymbol>.Default.Equals(descriptor.Symbol, default))
                {
                    errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' returned a descriptor with the default symbol.");
                    continue;
                }

                if (descriptor.StackRule.MakeNullable() == null)
                    errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' returned descriptor '{descriptor.Symbol}' with a null StackRule.");

                if (descriptor.ValidationRule.MakeNullable() == null)
                    errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' returned descriptor '{descriptor.Symbol}' with a null ValidationRule.");

                if (!symbolOwners.TryAdd(descriptor.Symbol, providerDisplayName))
                {
                    var firstOwner = symbolOwners[descriptor.Symbol];
                    errors.Add(
                        $"Intrinsic symbol '{descriptor.Symbol}' is duplicated by provider '{providerDisplayName}'. The symbol is already exported by provider '{firstOwner}'.");
                }
            }
        }

        foreach (var duplicateProviderRegistration in providerRegistrationCounts
                     .Where(static x => x.Value > 1)
                     .OrderBy(static x => x.Key.FullName, StringComparer.Ordinal))
        {
            var providerDisplayName = duplicateProviderRegistration.Key.FullName ?? duplicateProviderRegistration.Key.Name;
            errors.Add($"Intrinsic descriptor provider '{providerDisplayName}' is registered {duplicateProviderRegistration.Value} times.");
        }

        return errors;
    }

    private static Type? GetRegistrationProviderType(ServiceDescriptor descriptor) => descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();

    private sealed record RegistrationEntry(int Index, ServiceDescriptor Descriptor);

    private sealed record ProviderEntry(int Index, IIntrinsicDescriptorProvider? Provider);
}