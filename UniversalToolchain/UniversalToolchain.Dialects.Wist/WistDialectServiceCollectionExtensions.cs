using System.Runtime.Loader;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.ExpressionTyping.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectServiceCollectionExtensions
{
    /// <summary>
    ///     Adds canonical Wist dialect composition services with manifest-driven runtime catalog and exact activation.
    /// </summary>
    internal static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        EnsureCapabilityContractAssembliesUseHostIdentity();

        return services
            .AddWistDialectCoreServices()
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();
    }

    private static void EnsureCapabilityContractAssembliesUseHostIdentity()
    {
        // Runtime implementations are loaded through an isolated context. Capability attributes
        // and provider interfaces are host contracts, so their exact assemblies must already be
        // rooted in the default context before any selected implementation type is resolved.
        // Otherwise the first runtime component can load a second contract identity and make a
        // valid provider invisible to the host-side capability resolver.
        var contractAssemblies = new[]
        {
            typeof(DialectCapabilityProviderAttribute).Assembly,
            typeof(IBuiltinFunctionDescriptorProvider).Assembly,
            typeof(IBuiltinFunctionRuntimeBindingProvider).Assembly,
            typeof(IExpressionTypeRuleProvider).Assembly
        };

        foreach (var assembly in contractAssemblies.Distinct())
        {
            if (!ReferenceEquals(AssemblyLoadContext.GetLoadContext(assembly), AssemblyLoadContext.Default))
            {
                throw new InvalidOperationException(
                    $"Wist host capability contract assembly '{assembly.FullName}' is not loaded in the default assembly context.");
            }
        }
    }
}
