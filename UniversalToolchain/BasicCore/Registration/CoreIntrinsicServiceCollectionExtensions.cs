using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Contracts;
using BasicCore.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BasicCore.Registration;

/// <summary>
///     Registers the backend-neutral intrinsic model owned by BasicCore.
/// </summary>
public static class CoreIntrinsicServiceCollectionExtensions
{
    public static IServiceCollection AddCoreIntrinsicServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddSingleton<IntrinsicDescriptorProviderMetadataValidator>();
        services.TryAddSingleton<IntrinsicSemanticCoverageValidator>();
        services.TryAddSingleton<IntrinsicSemanticStartupValidator>();
        services.TryAddSingleton<IIntrinsicTypeResolutionContext, IntrinsicTypeResolutionContext>();
        services.TryAddSingleton<MethodCallTypeSemanticsResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IIntrinsicDescriptorProvider, CoreIntrinsicDescriptorProvider>());
        services.TryAddSingleton<IIntrinsicCatalog>(sp =>
        {
            var providers = sp.GetServices<IIntrinsicDescriptorProvider>();
            return new IntrinsicCatalogBuilder().Build(providers);
        });
        services.TryAddSingleton<IInstructionIntrinsicReader, InstructionIntrinsicReader>();
        services.TryAddSingleton<IIntrinsicTypeStackProcessor, IntrinsicTypeStackProcessor>();
        services.TryAddSingleton<IIntrinsicCapabilitySetFactory, CompilerIntrinsicCapabilitySetFactory>();

        return services;
    }
}
