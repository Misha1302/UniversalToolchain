using System.Diagnostics.CodeAnalysis;
using BasicCore.Compilation;
using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

[TestFixture]
public sealed class LazyBackendRuntimeActivationTests
{
    [Test]
    public void Host_ActivatesOnlyRequestedBackend_AndCachesItsRuntime()
    {
        var firstDescriptor = new RuntimeBackendDescriptor(new DialectBackendId("first"), ["one"]);
        var secondDescriptor = new RuntimeBackendDescriptor(new DialectBackendId("second"), ["two"]);
        var firstFactoryCalls = 0;
        var secondFactoryCalls = 0;
        var services = new ServiceCollection();
        services.AddSingleton(new ToolchainBackendRuntimeRegistration(
            firstDescriptor,
            _ =>
            {
                firstFactoryCalls++;
                return new ToolchainBackendRuntime(firstDescriptor, new StubCore("first"));
            }));
        services.AddSingleton(new ToolchainBackendRuntimeRegistration(
            secondDescriptor,
            _ =>
            {
                secondFactoryCalls++;
                return new ToolchainBackendRuntime(secondDescriptor, new StubCore("second"));
            }));
        using var provider = services.BuildServiceProvider();
        using var host = new ToolchainRuntimeHost(
            provider,
            new TestRuntimeConfiguration([firstDescriptor, secondDescriptor]));

        var firstCore = host.GetCore("one");
        var firstCoreAgain = host.GetCore("first");

        Assert.Multiple(() =>
        {
            Assert.That(firstCore.Run(string.Empty), Is.EqualTo("first"));
            Assert.That(firstCoreAgain, Is.SameAs(firstCore));
            Assert.That(firstFactoryCalls, Is.EqualTo(1));
            Assert.That(secondFactoryCalls, Is.Zero);
        });

        var secondCore = host.GetCore("two");

        Assert.Multiple(() =>
        {
            Assert.That(secondCore.Run(string.Empty), Is.EqualTo("second"));
            Assert.That(firstFactoryCalls, Is.EqualTo(1));
            Assert.That(secondFactoryCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Registration_MismatchedFactoryResult_IsNeverPublished()
    {
        var expected = new RuntimeBackendDescriptor(new DialectBackendId("expected"));
        var unexpected = new RuntimeBackendDescriptor(new DialectBackendId("unexpected"));
        var factoryCalls = 0;
        var registration = new ToolchainBackendRuntimeRegistration(
            expected,
            _ =>
            {
                factoryCalls++;
                return new ToolchainBackendRuntime(unexpected, new StubCore("unexpected"));
            });
        using var provider = new ServiceCollection().BuildServiceProvider();

        var first = Assert.Throws<InvalidOperationException>(() => registration.Resolve(provider));
        var second = Assert.Throws<InvalidOperationException>(() => registration.Resolve(provider));

        Assert.Multiple(() =>
        {
            Assert.That(first!.Message, Does.Contain("returned runtime"));
            Assert.That(second!.Message, Does.Contain("returned runtime"));
            Assert.That(factoryCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void Registration_ReusedAcrossContainers_CachesRuntimePerProvider()
    {
        var descriptor = new RuntimeBackendDescriptor(new DialectBackendId("scoped"));
        var calls = 0;
        var registration = new ToolchainBackendRuntimeRegistration(
            descriptor,
            provider => new ToolchainBackendRuntime(descriptor, new StubCore($"core-{++calls}-{provider.GetHashCode()}")));
        using var firstProvider = new ServiceCollection().BuildServiceProvider();
        using var secondProvider = new ServiceCollection().BuildServiceProvider();

        var first = registration.Resolve(firstProvider);
        var firstAgain = registration.Resolve(firstProvider);
        var second = registration.Resolve(secondProvider);

        Assert.Multiple(() =>
        {
            Assert.That(firstAgain, Is.SameAs(first));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void Host_DuplicateRuntimeRegistration_RejectsCompositionBeforeActivation()
    {
        var descriptor = new RuntimeBackendDescriptor(new DialectBackendId("duplicate"));
        var services = new ServiceCollection();
        services.AddSingleton(new ToolchainBackendRuntimeRegistration(
            descriptor,
            _ => new ToolchainBackendRuntime(descriptor, new StubCore("first"))));
        services.AddSingleton(new ToolchainBackendRuntimeRegistration(
            descriptor,
            _ => new ToolchainBackendRuntime(descriptor, new StubCore("second"))));
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ToolchainRuntimeHost(provider, new TestRuntimeConfiguration([descriptor])));

        Assert.That(exception!.Message, Does.Contain("Multiple runtime registrations"));
    }

    private sealed class StubCore(string value) : ICoreRunnable, IArtifactCompiler
    {
        public object? Run(string code, Dictionary<string, object>? args = null) => value;

        public ICompiledArtifact Compile(string code, OrderedDictionary<string, Type>? parameters = null) =>
            throw new NotSupportedException();

        public ICompiledArtifact Compile(CompilationInput input) => throw new NotSupportedException();
    }

    private sealed class TestRuntimeConfiguration : IToolchainRuntimeConfiguration
    {
        private readonly IReadOnlyDictionary<DialectBackendId, DialectBackendRuntimeConfiguration> _configurations;
        private readonly IReadOnlyDictionary<string, DialectBackendId> _idsByName;

        public TestRuntimeConfiguration(IReadOnlyList<RuntimeBackendDescriptor> descriptors)
        {
            EnabledBackends = descriptors;
            _configurations = descriptors.ToDictionary(
                static descriptor => descriptor.BackendId,
                static descriptor => new DialectBackendRuntimeConfiguration(descriptor, [], [], [], false));
            _idsByName = descriptors
                .SelectMany(static descriptor => descriptor.AllNames.Select(name => (name, descriptor.BackendId)))
                .ToDictionary(static pair => pair.name, static pair => pair.BackendId, StringComparer.Ordinal);
        }

        public string DialectName => "lazy-test";

        public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends { get; }

        public bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId) =>
            _idsByName.TryGetValue(nameOrAlias, out backendId);

        public bool TryGetEnabledBackend(
            DialectBackendId backendId,
            [MaybeNullWhen(false)] out DialectBackendRuntimeConfiguration backendConfiguration) =>
            _configurations.TryGetValue(backendId, out backendConfiguration);
    }
}
