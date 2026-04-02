using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeBackendIntrinsicRegistryTests
{
    [Test]
    public void CreateDescriptors_NoBackends_ReturnsEmptyList()
    {
        var descriptors = RuntimeBackendIntrinsicRegistry.CreateDescriptors([]);

        Assert.That(descriptors, Is.Empty);
    }

    [Test]
    public void CreateDescriptors_CommonIntrinsics_AreMappedToAnySelector()
    {
        var descriptors = RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.Compiler(["common", "compiler-only"]),
            FakeRegistrar.Interpreter(["common", "interpreter-only"])
        ]);

        var common = descriptors.Single(static x => x.CanonicalId == "common");

        Assert.That(common.Target, Is.EqualTo(DialectBackendSelector.Any));
    }

    [Test]
    public void CreateDescriptors_BackendSpecificIntrinsics_RemainScopedToBackend()
    {
        var descriptors = RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.Compiler(["common", "compiler-only"]),
            FakeRegistrar.Interpreter(["common", "interpreter-only"])
        ]);

        Assert.Multiple(() =>
        {
            var compilerOnly = descriptors.Single(static x => x.CanonicalId == "compiler-only");
            var interpreterOnly = descriptors.Single(static x => x.CanonicalId == "interpreter-only");

            Assert.That(compilerOnly.Target, Is.EqualTo(DialectBackendSelector.For(FakeRegistrar.CompilerBackendId)));
            Assert.That(interpreterOnly.Target, Is.EqualTo(DialectBackendSelector.For(FakeRegistrar.InterpreterBackendId)));
        });
    }

    [Test]
    public void CreateDescriptors_DescriptorsAreSortedDeterministically()
    {
        var descriptors = RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.Interpreter(["common", "beta"]),
            FakeRegistrar.Compiler(["common", "alpha"])
        ]);

        var ordered = descriptors
            .Select(static x => $"{x.CanonicalId}@{x.Target}")
            .ToArray();

        Assert.That(ordered, Is.EqualTo(new[]
        {
            "alpha@compiler",
            "beta@interpreter",
            "common@any"
        }));
    }

    [Test]
    public void CreateDescriptors_DuplicateBackendProviders_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.Compiler(["common"]),
            FakeRegistrar.Compiler(["other"])
        ]));

        Assert.That(ex!.Message, Does.Contain("Duplicate backend provider registration for backend 'compiler'"));
    }

    [Test]
    public void CreateDescriptors_NullSupportedIntrinsics_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.CompilerWithNullIntrinsics()
        ]));

        Assert.That(ex!.Message, Does.Contain("returned null supported intrinsics"));
    }

    [Test]
    public void CreateDescriptors_NullRegistrarEntry_Throws()
    {
        IDialectBackendRuntimeRegistrar? nullRegistrar = null;

        var ex = Assert.Throws<NullReferenceException>(() => RuntimeBackendIntrinsicRegistry.CreateDescriptors([
            FakeRegistrar.Compiler(["common"]),
            nullRegistrar!
        ]));

        Assert.That(ex!.Message, Does.Contain("backendRegistrars"));
    }

    private sealed class FakeRegistrar(DialectBackendId backendId, IReadOnlyList<string>? supportedIntrinsics) : IDialectBackendRuntimeRegistrar
    {
        public static DialectBackendId CompilerBackendId { get; } = new("compiler");
        public static DialectBackendId InterpreterBackendId { get; } = new("interpreter");

        public DialectBackendId BackendId { get; } = backendId;

        public IReadOnlyList<string> SupportedIntrinsics { get; } = supportedIntrinsics!;

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }

        public static FakeRegistrar Compiler(IReadOnlyList<string> intrinsics) => new(CompilerBackendId, intrinsics);

        public static FakeRegistrar Interpreter(IReadOnlyList<string> intrinsics) => new(InterpreterBackendId, intrinsics);

        public static FakeRegistrar CompilerWithNullIntrinsics() => new(CompilerBackendId, null);
    }
}
