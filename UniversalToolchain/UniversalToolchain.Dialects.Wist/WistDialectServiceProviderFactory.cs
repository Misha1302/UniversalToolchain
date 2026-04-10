using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds a real Wist runtime service provider from resolved dialect execution configuration.
/// </summary>
public sealed class WistDialectServiceProviderFactory
{
    private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _backendProviders;

    public WistDialectServiceProviderFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendProviders)
    {
        _backendProviders = CreateBackendProviderMap(backendProviders);
    }

    public IServiceProvider Create(WistDialectExecutionConfiguration configuration)
    {
        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();

        RegisterModules(services, configuration.FrontendModules, typeof(IFrontendCoreModule), ServiceLifetime.Singleton);
        RegisterModules(services, configuration.IrModules, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterModules(services, configuration.Optimizers, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterIntrinsicDescriptorProviders(services, configuration);
        RegisterBackendRuntimes(services, configuration);
        var coverageRequirements = ValidateIntrinsicSemantics(services);

        var provider = services.BuildServiceProvider();
        ValidateIntrinsicSemantics(provider, coverageRequirements);
        return provider;
    }

    private static void RegisterModules(IServiceCollection services, IEnumerable<Type> types, Type serviceType, ServiceLifetime lifetime)
    {
        foreach (var type in types.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            services.Add(new ServiceDescriptor(serviceType, type, lifetime));

            if (!services.Any(x => x.ServiceType == type && x.ImplementationType == type))
                services.Add(new ServiceDescriptor(type, type, lifetime));
        }
    }

    private void RegisterBackendRuntimes(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        foreach (var backend in configuration.BackendConfigurations.OrderBy(x => x.BackendDescriptor.BackendId))
        {
            if (!_backendProviders.TryGetValue(backend.BackendDescriptor.BackendId, out var backendProvider))
                Thrower.InvalidOpEx($"No backend runtime registrar is registered for backend '{backend.BackendDescriptor.CanonicalId}'.");

            backendProvider.RegisterRuntime(services, backend);
        }
    }

    private static void RegisterIntrinsicDescriptorProviders(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        var providerTypes = new SortedSet<Type>(TypeFullNameComparer.Instance);

        foreach (var moduleType in configuration.IrModules
                     .Concat(configuration.Optimizers)
                     .Concat(configuration.FrontendModules))
        {
            foreach (var attribute in moduleType.GetCustomAttributes(typeof(IntrinsicDescriptorProviderAttribute), false)
                         .Cast<IntrinsicDescriptorProviderAttribute>())
            {
                if (!typeof(IIntrinsicDescriptorProvider).IsAssignableFrom(attribute.ProviderType))
                {
                    var moduleDisplayName = moduleType.FullName ?? moduleType.Name;
                    var providerDisplayName = attribute.ProviderType.FullName ?? attribute.ProviderType.Name;
                    Thrower.InvalidOpEx(
                        $"Module '{moduleDisplayName}' declares intrinsic descriptor provider '{providerDisplayName}', but the provider type does not implement IIntrinsicDescriptorProvider.");
                }

                providerTypes.Add(attribute.ProviderType);
            }
        }

        foreach (var providerType in providerTypes)
        {
            if (!services.Any(x => x.ServiceType == typeof(IIntrinsicDescriptorProvider) && x.ImplementationType == providerType))
                services.AddSingleton(typeof(IIntrinsicDescriptorProvider), providerType);
        }
    }

    private static IReadOnlyList<(Type ModuleType, Type ProviderType)> ValidateIntrinsicSemantics(IServiceCollection services)
    {
        var coverageRequirements = GetIntrinsicCoverageRequirements(services);
        var metadataValidator = new IntrinsicDescriptorProviderMetadataValidator();
        var coverageValidator = new IntrinsicSemanticCoverageValidator();
        var errors = new List<string>();

        errors.AddRange(metadataValidator.Validate(services));
        errors.AddRange(coverageValidator.Validate(GetRegisteredProviderTypes(services), coverageRequirements));

        if (errors.Count > 0)
            Thrower.InvalidOpEx("Intrinsic semantic startup validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));

        return coverageRequirements;
    }

    private static void ValidateIntrinsicSemantics(
        IServiceProvider provider,
        IReadOnlyList<(Type ModuleType, Type ProviderType)> coverageRequirements)
    {
        var validator = provider.GetRequiredService<IntrinsicSemanticStartupValidator>();
        var providers = provider.GetServices<IIntrinsicDescriptorProvider>();
        validator.Validate(providers, coverageRequirements);
    }

    private static IReadOnlyList<Type> GetRegisteredProviderTypes(IServiceCollection services)
    {
        return services
            .Where(static x => x.ServiceType == typeof(IIntrinsicDescriptorProvider))
            .Select(static x => x.ImplementationType ?? x.ImplementationInstance?.GetType())
            .Where(static x => x != null)
            .Cast<Type>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<Type> GetRegisteredImplementationTypes(IServiceCollection services, Type serviceType)
    {
        return services
            .Where(x => x.ServiceType == serviceType)
            .Select(x => x.ImplementationType ?? x.ImplementationInstance?.GetType())
            .Where(static x => x != null)
            .Cast<Type>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<(Type ModuleType, Type ProviderType)> GetIntrinsicCoverageRequirements(IServiceCollection services)
    {
        var moduleTypes = GetRegisteredImplementationTypes(services, typeof(IFrontendCoreModule))
            .Concat(GetRegisteredImplementationTypes(services, typeof(IIRProcessingModule)))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal);

        return moduleTypes
            .SelectMany(static moduleType =>
                moduleType.GetCustomAttributes(typeof(IntrinsicDescriptorProviderAttribute), false)
                    .Cast<IntrinsicDescriptorProviderAttribute>()
                    .Select(attribute => (ModuleType: moduleType, ProviderType: attribute.ProviderType)))
            .OrderBy(x => x.ModuleType.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.ProviderType.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> CreateBackendProviderMap(IEnumerable<IDialectBackendRuntimeRegistrar> backendProviders)
    {
        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        var map = new SortedDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar>();
        foreach (var backendProvider in backendProviders
                     .Select(x => x.NotNull(nameof(backendProviders)))
                     .OrderBy(x => x.BackendId))
        {
            if (!map.TryAdd(backendProvider.BackendId, backendProvider))
                Thrower.InvalidOpEx($"Duplicate backend runtime registrar registration for backend '{backendProvider.BackendId.Value}'.");
        }

        return map;
    }

    private sealed class TypeFullNameComparer : IComparer<Type>
    {
        public static TypeFullNameComparer Instance { get; } = new();

        public int Compare(Type? x, Type? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;

            return StringComparer.Ordinal.Compare(x.FullName, y.FullName);
        }
    }
}
