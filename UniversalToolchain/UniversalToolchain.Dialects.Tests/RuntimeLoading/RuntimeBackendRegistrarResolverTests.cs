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

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Entry("backend", null)));

        Assert.That(exception!.Message, Does.Contain("registrarTypeFullName"));
    }

    [Test]
    public void Resolve_WhenRegistrarTypeDoesNotImplementBackendRegistrar_ThrowsClearError()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([(typeof(NotARegistrar).FullName!, typeof(NotARegistrar))]),
            new ServiceCollection().BuildServiceProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Entry("backend", typeof(NotARegistrar).FullName)));

        Assert.That(exception!.Message, Does.Contain(nameof(IDialectBackendRuntimeRegistrar)));
    }

    [Test]
    public void Resolve_WhenRegistrarBackendIdDoesNotMatchManifestAlias_ThrowsClearError()
    {
        var resolver = new DefaultRuntimeBackendRegistrarResolver(
            new StubAssemblyTypeLoader([(typeof(MismatchedRegistrar).FullName!, typeof(MismatchedRegistrar))]),
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
        var typeLoader = new StubAssemblyTypeLoader([(typeof(DependencyBackedRegistrar).FullName!, typeof(DependencyBackedRegistrar))]);
        var resolver = new DefaultRuntimeBackendRegistrarResolver(typeLoader, provider);

        var registrar = resolver.Resolve(Entry("backend", typeof(DependencyBackedRegistrar).FullName));

        Assert.Multiple(() =>
        {
            Assert.That(registrar, Is.InstanceOf<DependencyBackedRegistrar>());
            Assert.That(((DependencyBackedRegistrar)registrar).Dependency.Value, Is.EqualTo("injected"));
            Assert.That(typeLoader.LoadedTypes, Is.EqualTo(new[] { typeof(DependencyBackedRegistrar).FullName }));
        });
    }

    private static RuntimeComponentManifestEntry Entry(string alias, string? registrarTypeFullName)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            "TestAssembly",
            new RuntimeComponentActivationInfo(typeof(object).FullName!, registrarTypeFullName));

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

    private sealed class StubAssemblyTypeLoader(IEnumerable<(string FullName, Type Type)> types) : IRuntimeAssemblyTypeLoader
    {
        private readonly IReadOnlyDictionary<string, Type> _types = types.ToDictionary(
            static x => x.FullName,
            static x => x.Type,
            StringComparer.Ordinal);
        private readonly List<string> _loadedTypes = [];

        public IReadOnlyList<string> LoadedTypes => _loadedTypes;

        public System.Reflection.Assembly LoadAssembly(string assemblySimpleName)
        {
            throw new NotSupportedException();
        }

        public Type LoadType(string assemblySimpleName, string activationTypeFullName)
        {
            Assert.That(assemblySimpleName, Is.EqualTo("TestAssembly"));
            _loadedTypes.Add(activationTypeFullName);
            return _types[activationTypeFullName];
        }
    }
}
