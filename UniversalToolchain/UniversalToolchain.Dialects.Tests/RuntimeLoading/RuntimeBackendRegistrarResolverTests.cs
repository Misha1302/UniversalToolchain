using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class RuntimeBackendRegistrarResolverTests
{
    [Test]
    public void Resolve_WhenRegistrarTypeFullNameIsMissing_ThrowsClearError()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([]),
            new ServiceCollection().BuildServiceProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Entry("backend", (string?)null)));

        Assert.That(exception!.Message, Does.Contain("registrarTypeFullName"));
    }

    [Test]
    public void Resolve_WhenRegistrarTypeDoesNotImplementBackendRegistrar_ThrowsClearError()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([("TestAssembly", typeof(NotARegistrar).FullName!, typeof(NotARegistrar))]),
            new ServiceCollection().BuildServiceProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Entry("backend", typeof(NotARegistrar).FullName)));

        Assert.That(exception!.Message, Does.Contain(nameof(IDialectBackendRuntimeRegistrar)));
    }

    [Test]
    public void Resolve_WhenRegistrarBackendIdDoesNotMatchManifestAlias_ThrowsClearError()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([("TestAssembly", typeof(MismatchedRegistrar).FullName!, typeof(MismatchedRegistrar))]),
            new ServiceCollection().BuildServiceProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Entry("selected", typeof(MismatchedRegistrar).FullName)));

        Assert.That(exception!.Message, Does.Contain("declares backend id 'different'").And.Contain("selected"));
    }

    [Test]
    public void Resolve_WhenRegistrarIsValid_LoadsExactTypeAndUsesDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new RegistrarDependency("injected"));
        using var provider = services.BuildServiceProvider();
        var typeLoader = new StubAssemblyTypeLoader([("TestAssembly", typeof(DependencyBackedRegistrar).FullName!, typeof(DependencyBackedRegistrar))]);
        var resolver = new DefaultRuntimeBackendRegistrarResolver(typeLoader, provider);

        var registrar = resolver.Resolve(Entry("backend", typeof(DependencyBackedRegistrar).FullName));

        Assert.Multiple(() =>
        {
            Assert.That(registrar, Is.InstanceOf<DependencyBackedRegistrar>());
            Assert.That(((DependencyBackedRegistrar)registrar).Dependency.Value, Is.EqualTo("injected"));
            Assert.That(typeLoader.LoadedTypes, Is.EqualTo(new[] { "TestAssembly::" + typeof(DependencyBackedRegistrar).FullName }));
        });
    }

    [Test]
    public void Resolve_WhenRegistrarAssemblyIsExplicit_LoadsFromRegistrarAssembly()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([("RegistrarAssembly", typeof(SimpleRegistrar).FullName!, typeof(SimpleRegistrar))]),
            new ServiceCollection().BuildServiceProvider());

        var entry = Entry(
            "backend",
            new RuntimeTypeReference("RegistrarAssembly", typeof(SimpleRegistrar).FullName!));

        var registrar = resolver.Resolve(entry);

        Assert.That(registrar, Is.InstanceOf<SimpleRegistrar>());
    }

    [Test]
    public void Resolve_WhenLegacyRegistrarAssemblyIsMissing_UsesBackendAssemblyFallback()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([("TestAssembly", typeof(SimpleRegistrar).FullName!, typeof(SimpleRegistrar))]),
            new ServiceCollection().BuildServiceProvider());

        var entry = Entry("backend", typeof(SimpleRegistrar).FullName);
        var registrar = resolver.Resolve(entry);

        Assert.That(registrar, Is.InstanceOf<SimpleRegistrar>());
    }

    private static RuntimeComponentManifestEntry Entry(string alias, string? registrarTypeFullName)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            "TestAssembly",
            new RuntimeComponentActivationInfo(typeof(object).FullName!, registrarTypeFullName));

    private static RuntimeComponentManifestEntry Entry(string alias, RuntimeTypeReference registrarTypeReference)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            "TestAssembly",
            new RuntimeComponentActivationInfo(
                new RuntimeTypeReference("TestAssembly", typeof(object).FullName!),
                registrarTypeReference));

    private sealed class NotARegistrar;

    private sealed class MismatchedRegistrar : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new("different");
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
    }

    private sealed class DependencyBackedRegistrar(RegistrarDependency dependency) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new("backend");
        public IReadOnlyList<string> SupportedIntrinsics => [];
        public RegistrarDependency Dependency { get; } = dependency;

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
    }

    private sealed record RegistrarDependency(string Value);

    private sealed class SimpleRegistrar : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new("backend");
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
    }

    private sealed class StubAssemblyTypeLoader(IEnumerable<(string AssemblySimpleName, string FullName, Type Type)> types) : IRuntimeAssemblyTypeLoader
    {
        private readonly IReadOnlyDictionary<(string AssemblySimpleName, string FullName), Type> _types = types.ToDictionary(
            static x => (x.AssemblySimpleName, x.FullName),
            static x => x.Type);
        private readonly List<string> _loadedTypes = [];

        public IReadOnlyList<string> LoadedTypes => _loadedTypes;

        public System.Reflection.Assembly LoadAssembly(string assemblySimpleName)
        {
            throw new NotSupportedException();
        }

        public Type LoadType(string assemblySimpleName, string activationTypeFullName)
        {
            _loadedTypes.Add(assemblySimpleName + "::" + activationTypeFullName);
            return _types[(assemblySimpleName, activationTypeFullName)];
        }
    }
}
