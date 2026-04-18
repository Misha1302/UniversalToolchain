using BasicCore.Contracts;
using ConditionsModule.Optimizers;
using LocalVariablesOptimizerModule;
using Microsoft.Extensions.DependencyInjection;
using NativeMathModule;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class IntrinsicDescriptorProviderRegistrationTests
{
    [Test]
    public void SelectedOptimizerModules_ShouldRegisterAssociatedIntrinsicProviders()
    {
        using var provider = CreateProvider(
            optimizers:
            [
                typeof(LocalVariablesOptimizer),
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
            typeof(ComparisonIntrinsicDescriptorProvider),
            typeof(StorageIntrinsicDescriptorProvider)
        }));
    }

    [Test]
    public void ProviderRegistration_ShouldBeDeterministic()
    {
        using var firstProvider = CreateProvider(
            optimizers:
            [
                typeof(LocalVariablesOptimizer),
                typeof(EGraphOptimizerModule),
                typeof(BooleanOptimizerModule),
                typeof(ComparisonIntrinsicOptimizerModule)
            ]);
        using var secondProvider = CreateProvider(
            optimizers:
            [
                typeof(ComparisonIntrinsicOptimizerModule),
                typeof(BooleanOptimizerModule),
                typeof(LocalVariablesOptimizer),
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

    private static ServiceProvider CreateProvider(
        IReadOnlyList<Type>? frontendModules = null,
        IReadOnlyList<Type>? irModules = null,
        IReadOnlyList<Type>? optimizers = null)
    {
        var factory = new WistDialectServiceProviderFactory([]);
        var configuration = new WistDialectExecutionConfiguration(
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
    private sealed class DuplicateBooleanOptimizerModule : IIRProcessingModule;
}