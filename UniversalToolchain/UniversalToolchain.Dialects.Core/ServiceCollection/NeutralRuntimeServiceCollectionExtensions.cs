using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers runtime services that are independent of concrete frontend and backend implementations.
/// </summary>
public static class NeutralRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddNeutralRuntimeInfrastructure(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddSingleton<IntrinsicDescriptorProviderMetadataValidator>();
        services.AddSingleton<IntrinsicSemanticCoverageValidator>();
        services.AddSingleton<IntrinsicSemanticStartupValidator>();
        services.AddSingleton<IIntrinsicTypeResolutionContext, IntrinsicTypeResolutionContext>();
        services.AddSingleton<MethodCallTypeSemanticsResolver>();
        services.AddTransient<IIntrinsicDescriptorProvider, CoreIntrinsicDescriptorProvider>();
        services.AddSingleton<IIntrinsicCatalog>(sp =>
        {
            var providers = sp.GetServices<IIntrinsicDescriptorProvider>();
            return new IntrinsicCatalogBuilder().Build(providers);
        });
        services.AddSingleton<ILegacyIntrinsicDecoder, LegacyIntrinsicDecoder>();
        services.AddSingleton<IInstructionIntrinsicReader, InstructionIntrinsicReader>();
        services.AddSingleton<IIntrinsicTypeStackProcessor, IntrinsicTypeStackProcessor>();
        services.AddSingleton<IIntrinsicCapabilitySetFactory, CompilerIntrinsicCapabilitySetFactory>();

        return services;
    }
}
