using BasicCore.Builtins;
using BasicCore.Contracts;
using ConditionsModule.Optimizers;
using Microsoft.Extensions.DependencyInjection;
using NativeMathModule;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class IntrinsicDescriptorProviderRegistrationTests
{
    [Test]
    public void SelectedOptimizerModules_ShouldRegisterAssociatedIntrinsicProviders()
    {
        using var provider = CreateProvider(
            optimizers:
            [
                typeof(ComparisonIntrinsicOptimizerModule),
                typeof(BooleanOptimizerModule),
                typeof(ArithmeticOptimizerModule)
            ]);

        var providerTypes = provider.GetServices<IIntrinsicDescriptorProvider>()
            .Select(static x => x.GetType())
            .ToArray();

        Assert.That(providerTypes, Is.EqualTo(new[]
        {
            typeof(CoreIntrinsicDescriptorProvider),
            typeof(ArithmeticIntrinsicDescriptorProvider),
            typeof(BooleanIntrinsicDescriptorProvider),
            typeof(ComparisonIntrinsicDescriptorProvider)
        }));
    }

    [Test]
    public void ProviderRegistration_ShouldBeDeterministic()
    {
        using var firstProvider = CreateProvider(
            optimizers:
            [
                typeof(EGraphOptimizerModule),
                typeof(BooleanOptimizerModule),
                typeof(ComparisonIntrinsicOptimizerModule)
            ]);
        using var secondProvider = CreateProvider(
            optimizers:
            [
                typeof(ComparisonIntrinsicOptimizerModule),
                typeof(BooleanOptimizerModule),
                typeof(EGraphOptimizerModule)
            ]);

        var firstProviderTypes = firstProvider.GetServices<IIntrinsicDescriptorProvider>()
            .Select(static x => x.GetType().FullName)
            .ToArray();
        var secondProviderTypes = secondProvider.GetServices<IIntrinsicDescriptorProvider>()
            .Select(static x => x.GetType().FullName)
            .ToArray();

        Assert.That(firstProviderTypes, Is.EqualTo(secondProviderTypes));
    }

    [Test]
    public void DuplicateProviderAttributes_ShouldNotProduceDuplicateRegistrations()
    {
        using var provider = CreateProvider(
            [typeof(DuplicateBooleanFrontendModule)],
            optimizers: [typeof(DuplicateBooleanOptimizerModule)]);

        var providerTypes = provider.GetServices<IIntrinsicDescriptorProvider>()
            .Select(static x => x.GetType())
            .ToArray();

        Assert.That(providerTypes.Count(static x => x == typeof(BooleanIntrinsicDescriptorProvider)), Is.EqualTo(1));
        Assert.That(providerTypes, Is.EqualTo(new[]
        {
            typeof(CoreIntrinsicDescriptorProvider),
            typeof(BooleanIntrinsicDescriptorProvider)
        }));
    }


    private static WistDialectServiceProviderFactory CreateFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) =>
        new(
            new StaticBackendRegistrarResolver(backendRegistrars),
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator(),
            ModuleContractPipelineProfiles.Warn,
            new InMemoryModuleContractDiagnosticSink());

    private static ServiceProvider CreateProvider(
        IReadOnlyList<Type>? frontendModules = null,
        IReadOnlyList<Type>? irModules = null,
        IReadOnlyList<Type>? optimizers = null)
    {
        var factory = CreateFactory([]);
        var configuration = new ToolchainRuntimeConfiguration(
            "Test",
            frontendModules ?? [],
            irModules ?? [],
            optimizers ?? [],
            [],
            []);

        return (ServiceProvider)factory.Create(configuration);
    }

    [IntrinsicDescriptorProvider(typeof(BooleanIntrinsicDescriptorProvider))]
    private sealed class DuplicateBooleanFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(BooleanIntrinsicDescriptorProvider))]
    [IntrinsicDescriptorProvider(typeof(BooleanIntrinsicDescriptorProvider))]
    private sealed class DuplicateBooleanOptimizerModule : IAirOptimizer;

    private sealed class StaticBackendRegistrarResolver(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) : IRuntimeBackendRegistrarResolver
    {
        private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _registrarsById = backendRegistrars.ToDictionary(
            static x => x.BackendId,
            static x => x);

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry) => _registrarsById[new DialectBackendId(backendEntry.CanonicalAlias)];
    }
}
