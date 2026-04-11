using System.Reflection.Emit;
using AbstractIrConverters;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Intrinsics.Capabilities;

namespace UniversalToolchain.Dialects.Tests.Wist;

public class RuntimeInfrastructureCompositionTests
{
    [Test]
    public void AddNeutralRuntimeInfrastructure_ShouldNotRegisterConcreteFrontendOrBackendDefaults()
    {
        var services = new ServiceCollection();
        services.AddNeutralRuntimeInfrastructure();

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<IIntrinsicCapabilitySetFactory>(), Is.Not.Null);
            Assert.That(provider.GetService<Func<ILexer>>(), Is.Null);
            Assert.That(provider.GetService<Func<IParser>>(), Is.Null);
            Assert.That(provider.GetService<Func<IAstToBytecodeTranslator>>(), Is.Null);
            Assert.That(provider.GetService<Func<IAbstractMethodsTranslator>>(), Is.Null);
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Null);
            Assert.That(provider.GetService<Func<IExecutor<IAbstractIR>>>(), Is.Null);
        });
    }

    [Test]
    public void AddBasicFrontendPipelineDefaults_ShouldRegisterLexerParserTranslatorFactories()
    {
        var services = new ServiceCollection();
        services.AddNeutralRuntimeInfrastructure();
        services.AddBasicFrontendPipelineDefaults();

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<Func<ILexer>>()(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<Func<IParser>>()(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<Func<IAstToBytecodeTranslator>>()(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<Func<IAbstractMethodsTranslator>>()(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
        });
    }

    [Test]
    public void AddCompilerBackendDefaults_ShouldRegisterCompilerRuntimeDefaults()
    {
        var services = new ServiceCollection();
        services.AddCompilerBackendDefaults();

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>()(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
            Assert.That(provider.GetService<Func<IExecutor<IAbstractIR>>>(), Is.Null);
        });
    }

    [Test]
    public void AddInterpreterBackendDefaults_ShouldRegisterInterpreterRuntimeDefaults()
    {
        var services = new ServiceCollection();
        services.AddInterpreterBackendDefaults();

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>()(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Null);
        });
    }

    [Test]
    public void AddCoreRuntimeInfrastructure_ShouldRemainEquivalentToCombinedDefaultWrappers()
    {
        var wrapperServices = new ServiceCollection();
        wrapperServices.AddCoreRuntimeInfrastructure();

        var combinedServices = new ServiceCollection();
        combinedServices
            .AddNeutralRuntimeInfrastructure()
            .AddBasicFrontendPipelineDefaults()
            .AddCompilerBackendDefaults()
            .AddInterpreterBackendDefaults();

        Assert.That(BuildServiceSignature(wrapperServices), Is.EqualTo(BuildServiceSignature(combinedServices)));
    }

    [Test]
    public void WistDialectServiceProviderFactory_ShouldUseNeutralInfrastructurePlusFrontendDefaults()
    {
        var descriptor = new RuntimeBackendDescriptor(new DialectBackendId("noop"), typeof(NoopRegistrar), ["noop"]);
        var factory = new WistDialectServiceProviderFactory([new NoopRegistrar(descriptor.BackendId)]);
        var configuration = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(descriptor, [], [], [], false)],
            [descriptor]);

        var provider = factory.Create(configuration);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(provider.GetService<IIntrinsicCapabilitySetFactory>(), Is.Not.Null);
                Assert.That(provider.GetService<Func<ILexer>>(), Is.Not.Null);
                Assert.That(provider.GetService<Func<IParser>>(), Is.Not.Null);
                Assert.That(provider.GetService<Func<IAstToBytecodeTranslator>>(), Is.Not.Null);
                Assert.That(provider.GetService<Func<IAbstractMethodsTranslator>>(), Is.Not.Null);
                Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
                Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
            });
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    [Test]
    public void CilBackendRegistrar_ShouldRegisterCompilerDefaultsThroughSharedBase()
    {
        var services = CreateBackendServiceCollection();
        var registrar = new WistCilDialectBackendServiceProvider();

        registrar.RegisterRuntime(services, CreateBackendConfiguration(WistDialectBackendIds.Cil, typeof(WistCilDialectBackendServiceProvider), "compiler"));

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Not.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
            Assert.That(provider.GetServices<WistDialectBackendRuntime>().Single().Descriptor.BackendId, Is.EqualTo(WistDialectBackendIds.Cil));
        });
    }

    [Test]
    public void InterpreterBackendRegistrar_ShouldRegisterInterpreterDefaultsThroughSharedBase()
    {
        var services = CreateBackendServiceCollection();
        var registrar = new WistInterpreterDialectBackendServiceProvider();

        registrar.RegisterRuntime(services, CreateBackendConfiguration(WistDialectBackendIds.Interpreter, typeof(WistInterpreterDialectBackendServiceProvider), "interpreter"));

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Not.Null);
            Assert.That(provider.GetService<Func<IExecutor<IAbstractIR>>>(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
            Assert.That(provider.GetServices<WistDialectBackendRuntime>().Single().Descriptor.BackendId, Is.EqualTo(WistDialectBackendIds.Interpreter));
        });
    }

    private static IServiceCollection CreateBackendServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddNeutralRuntimeInfrastructure();
        services.AddBasicFrontendPipelineDefaults();
        return services;
    }

    private static DialectBackendRuntimeConfiguration CreateBackendConfiguration(DialectBackendId backendId, Type registrarType, string alias)
    {
        var descriptor = new RuntimeBackendDescriptor(backendId, registrarType, [alias]);
        return new DialectBackendRuntimeConfiguration(descriptor, [], [], [], false);
    }

    private static IReadOnlyList<string> BuildServiceSignature(IServiceCollection services)
        => services
            .Select(static descriptor =>
                string.Join(
                    "|",
                    descriptor.ServiceType.FullName,
                    descriptor.Lifetime,
                    descriptor.ImplementationType?.FullName,
                    descriptor.ImplementationInstance?.GetType().FullName,
                    descriptor.ImplementationFactory == null ? string.Empty : "factory"))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

    private sealed class NoopRegistrar(DialectBackendId backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = backendId;

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
    }
}
