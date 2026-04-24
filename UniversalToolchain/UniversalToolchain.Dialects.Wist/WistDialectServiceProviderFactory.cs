using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Dialects.Wist.Functions;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Functions.Abstractions;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds a real Wist runtime service provider from resolved dialect execution configuration.
/// </summary>
public sealed class WistDialectServiceProviderFactory
{
    private readonly IRuntimeBackendRegistrarResolver _backendRegistrarResolver;
    private readonly IntrinsicSemanticBootstrapPlanBuilder _intrinsicBootstrapPlanBuilder;
    private readonly IntrinsicSemanticBootstrapPreProviderValidator _intrinsicBootstrapPreProviderValidator;
    private readonly IntrinsicSemanticBootstrapRuntimeValidator _intrinsicBootstrapRuntimeValidator;

    public WistDialectServiceProviderFactory(
        IRuntimeBackendRegistrarResolver backendRegistrarResolver,
        IntrinsicSemanticBootstrapPlanBuilder intrinsicBootstrapPlanBuilder,
        IntrinsicSemanticBootstrapPreProviderValidator intrinsicBootstrapPreProviderValidator,
        IntrinsicSemanticBootstrapRuntimeValidator intrinsicBootstrapRuntimeValidator)
    {
        _backendRegistrarResolver = backendRegistrarResolver.ArgNotNull();
        _intrinsicBootstrapPlanBuilder = intrinsicBootstrapPlanBuilder.ArgNotNull();
        _intrinsicBootstrapPreProviderValidator = intrinsicBootstrapPreProviderValidator.ArgNotNull();
        _intrinsicBootstrapRuntimeValidator = intrinsicBootstrapRuntimeValidator.ArgNotNull();
    }

    public IServiceProvider Create(WistDialectExecutionConfiguration configuration)
    {
        configuration = configuration.ArgNotNull();

        var services = new ServiceCollection();
        services.AddNeutralRuntimeInfrastructure();
        services.AddBasicFrontendPipelineDefaults();
        services.AddSingleton(configuration);
        services.AddSingleton<ILanguageFeatureCatalog, WistLanguageFeatureCatalog>();
        services.AddSingleton<IBuiltinFunctionCatalog, WistBuiltinFunctionCatalog>();
        services.AddSingleton<WistBuiltinFunctionBindingResolver>();

        RegisterModules(services, configuration.FrontendModules, typeof(IFrontendCoreModule), ServiceLifetime.Singleton);
        RegisterModules(services, configuration.IrModules, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterModules(services, configuration.Optimizers, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterIntrinsicDescriptorProviders(services, configuration);
        RegisterBackendRuntimes(services, configuration);

        var bootstrapPlan = _intrinsicBootstrapPlanBuilder.Build(services);
        _intrinsicBootstrapPreProviderValidator.Validate(bootstrapPlan, services);

        var provider = services.BuildServiceProvider();
        _intrinsicBootstrapRuntimeValidator.Validate(provider, bootstrapPlan);
        return provider;
    }

    private static void RegisterModules(IServiceCollection services, IEnumerable<Type> types, Type serviceType, ServiceLifetime lifetime)
    {
        foreach (var type in types.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            services.Add(new ServiceDescriptor(serviceType, type, lifetime));

            if (!services.Any(x => x.ServiceType == type && x.ImplementationType == type))
            {
                services.Add(new ServiceDescriptor(type, type, lifetime));
            }
        }
    }

    private void RegisterBackendRuntimes(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        foreach (var backend in configuration.BackendConfigurations.OrderBy(x => x.BackendDescriptor.BackendId))
        {
            var manifestEntry = backend.BackendManifestEntry
                                ?? Thrower.InvalidOpEx<RuntimeComponentManifestEntry>(
                                    $"Backend runtime configuration for backend '{backend.BackendDescriptor.CanonicalId}' does not include the selected backend manifest entry.");
            var backendRegistrar = _backendRegistrarResolver.Resolve(manifestEntry);
            backendRegistrar.RegisterRuntime(services, backend);
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
            {
                services.AddSingleton(typeof(IIntrinsicDescriptorProvider), providerType);
            }
        }
    }

    private sealed class TypeFullNameComparer : IComparer<Type>
    {
        public static TypeFullNameComparer Instance { get; } = new();

        public int Compare(Type? x, Type? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return StringComparer.Ordinal.Compare(x.FullName, y.FullName);
        }
    }
}
