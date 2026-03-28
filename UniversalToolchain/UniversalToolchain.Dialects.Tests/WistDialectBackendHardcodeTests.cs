using System.Reflection.Emit;
using BasicCore.Contracts;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectBackendHardcodeTests
{
    [Test]
    public void ExistingCilBackendStillWorks()
    {
        using var rootProvider = CreateRootProvider();
        var factory = rootProvider.GetRequiredService<WistDialectServiceProviderFactory>();
        var configuration = CreateConfiguration(rootProvider, WistDialectBackendIds.Cil);

        using var runtimeProvider = (ServiceProvider)factory.Create(configuration);
        var runtimes = runtimeProvider.GetServices<WistDialectBackendRuntime>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(runtimeProvider.GetRequiredService<IExecutableGiver<DynamicMethod>>(), Is.Not.Null);
            Assert.That(runtimes.Select(x => x.Descriptor.BackendId), Is.EqualTo(new[] { WistDialectBackendIds.Cil }));
            Assert.That(runtimes[0].Core, Is.AssignableTo<ICoreRunnable>());
        });
    }

    [Test]
    public void ExistingInterpreterBackendStillWorks()
    {
        using var rootProvider = CreateRootProvider();
        var factory = rootProvider.GetRequiredService<WistDialectServiceProviderFactory>();
        var configuration = CreateConfiguration(rootProvider, WistDialectBackendIds.Interpreter);

        using var runtimeProvider = (ServiceProvider)factory.Create(configuration);
        var runtimes = runtimeProvider.GetServices<WistDialectBackendRuntime>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(runtimeProvider.GetRequiredService<IExecutableGiver<IAbstractIR>>(), Is.Not.Null);
            Assert.That(runtimes.Select(x => x.Descriptor.BackendId), Is.EqualTo(new[] { WistDialectBackendIds.Interpreter }));
            Assert.That(runtimes[0].Core, Is.AssignableTo<ICoreRunnable>());
        });
    }

    [Test]
    public void CentralFactorySupportsFakeBackendPluginWithoutCodeChanges()
    {
        var backendId = new DialectBackendId("fake-backend");
        var backendDescriptor = new RuntimeBackendDescriptor(backendId, "fake-runtime");
        var factory = new WistDialectServiceProviderFactory([new FakeBackendServiceProvider(backendId, ["fake_intrinsic"]) ]);
        var configuration = CreateConfiguration(backendDescriptor);

        using var runtimeProvider = (ServiceProvider)factory.Create(configuration);
        var runtime = runtimeProvider.GetServices<WistDialectBackendRuntime>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Descriptor.BackendId, Is.EqualTo(backendId));
            Assert.That(runtime.Core, Is.TypeOf<FakeCoreRunnable>());
            Assert.That(runtimeProvider.GetRequiredService<ICoreRunnable>(), Is.TypeOf<FakeCoreRunnable>());
        });
    }

    [Test]
    public void DuplicateBackendProvidersAreRejectedDeterministically()
    {
        var backendId = new DialectBackendId("duplicate-backend");

        var exception = Assert.Throws<InvalidOperationException>(() => new WistDialectServiceProviderFactory([
            new FakeBackendServiceProvider(backendId, ["first_intrinsic"]),
            new FakeBackendServiceProvider(backendId, ["second_intrinsic"])
        ]));

        Assert.That(exception!.Message, Is.EqualTo("Duplicate backend runtime registrar registration for backend 'duplicate-backend'."));
    }

    [Test]
    public void MissingBackendProviderIsRejectedClearly()
    {
        var requestedBackend = new RuntimeBackendDescriptor(new DialectBackendId("missing-backend"), "missing-runtime");
        var factory = new WistDialectServiceProviderFactory([new FakeBackendServiceProvider(new DialectBackendId("other-backend"), ["other_intrinsic"])]);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Create(CreateConfiguration(requestedBackend)));

        Assert.That(exception!.Message, Is.EqualTo("No backend runtime registrar is registered for backend 'missing-backend'."));
    }

    [Test]
    public void IntrinsicRegistryIsAssembledFromBackendPlugins()
    {
        var backendA = new DialectBackendId("backend-a");
        var backendB = new DialectBackendId("backend-b");

        var descriptors = RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            new FakeBackendServiceProvider(backendA, ["shared_intrinsic", "only_a"]),
            new FakeBackendServiceProvider(backendB, ["shared_intrinsic", "only_b"])
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(descriptors.Select(x => $"{x.CanonicalId}@{DialectBackendSelectorText.ToText(x.Target)}"), Is.EqualTo(new[]
            {
                "only_a@backend-a",
                "only_b@backend-b",
                "shared_intrinsic@any"
            }));
            Assert.That(descriptors.Single(x => x.CanonicalId == "shared_intrinsic").Target, Is.EqualTo(DialectBackendSelector.Any));
        });
    }

    [Test]
    public void DeterministicOrderingDoesNotDependOnBackendProviderRegistrationOrder()
    {
        var backendA = new RuntimeBackendDescriptor(new DialectBackendId("backend-a"), "backend-a-runtime");
        var backendB = new RuntimeBackendDescriptor(new DialectBackendId("backend-b"), "backend-b-runtime");
        var configuration = CreateConfiguration(backendA, backendB);
        var providersInDeclaredOrder = new IDialectBackendRuntimeRegistrar[]
        {
            new FakeBackendServiceProvider(backendA.BackendId, ["shared_intrinsic", "only_a"]),
            new FakeBackendServiceProvider(backendB.BackendId, ["shared_intrinsic", "only_b"])
        };
        var providersInReverseOrder = new IDialectBackendRuntimeRegistrar[]
        {
            new FakeBackendServiceProvider(backendB.BackendId, ["shared_intrinsic", "only_b"]),
            new FakeBackendServiceProvider(backendA.BackendId, ["shared_intrinsic", "only_a"])
        };

        var firstDescriptorSet = RuntimeBackendIntrinsicRegistry.CreateDescriptors(providersInDeclaredOrder)
            .Select(x => $"{x.CanonicalId}@{DialectBackendSelectorText.ToText(x.Target)}")
            .ToList();
        var secondDescriptorSet = RuntimeBackendIntrinsicRegistry.CreateDescriptors(providersInReverseOrder)
            .Select(x => $"{x.CanonicalId}@{DialectBackendSelectorText.ToText(x.Target)}")
            .ToList();

        using var firstRuntimeProvider = (ServiceProvider)new WistDialectServiceProviderFactory(providersInDeclaredOrder).Create(configuration);
        using var secondRuntimeProvider = (ServiceProvider)new WistDialectServiceProviderFactory(providersInReverseOrder).Create(configuration);
        var firstRuntimeOrder = firstRuntimeProvider.GetServices<WistDialectBackendRuntime>().Select(x => x.Descriptor.BackendId.Value).ToList();
        var secondRuntimeOrder = secondRuntimeProvider.GetServices<WistDialectBackendRuntime>().Select(x => x.Descriptor.BackendId.Value).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(firstDescriptorSet, Is.EqualTo(secondDescriptorSet));
            Assert.That(firstDescriptorSet, Is.EqualTo(new[] { "only_a@backend-a", "only_b@backend-b", "shared_intrinsic@any" }));
            Assert.That(firstRuntimeOrder, Is.EqualTo(secondRuntimeOrder));
            Assert.That(firstRuntimeOrder, Is.EqualTo(new[] { "backend-a", "backend-b" }));
        });
    }

    private static ServiceProvider CreateRootProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesLegacy();
        return services.BuildServiceProvider();
    }

    private static WistDialectExecutionConfiguration CreateConfiguration(ServiceProvider provider, DialectBackendId backendId)
    {
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
        if (!registry.TryResolveBackend(backendId, out var backendDescriptor))
            Thrower.InvalidOpEx($"Backend '{backendId.Value}' was not registered.");

        return CreateConfiguration(backendDescriptor!);
    }

    private static WistDialectExecutionConfiguration CreateConfiguration(params RuntimeBackendDescriptor[] backendDescriptors)
    {
        return new WistDialectExecutionConfiguration(
            "test-dialect",
            [],
            [],
            [],
            backendDescriptors.Select(static x => new DialectBackendRuntimeConfiguration(x, [], [], false)),
            backendDescriptors);
    }

    private sealed class FakeBackendServiceProvider(DialectBackendId backendId, IReadOnlyList<string> supportedIntrinsics) : IDialectBackendRuntimeRegistrar
    {
        private readonly IReadOnlyList<string> _supportedIntrinsics = supportedIntrinsics
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        public DialectBackendId BackendId { get; } = backendId;

        public IReadOnlyList<string> SupportedIntrinsics => _supportedIntrinsics;

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            if (services == null)
                Thrower.ArgumentNull(nameof(services));

            if (configuration == null)
                Thrower.ArgumentNull(nameof(configuration));

            services.AddTransient<ICoreRunnable, FakeCoreRunnable>();
            services.AddTransient(provider => new WistDialectBackendRuntime(configuration.BackendDescriptor, provider.GetRequiredService<ICoreRunnable>()));
        }
    }

    private sealed class FakeCoreRunnable : ICoreRunnable
    {
        public object? Run(string code, Dictionary<string, object>? args = null) => code;
    }
}
