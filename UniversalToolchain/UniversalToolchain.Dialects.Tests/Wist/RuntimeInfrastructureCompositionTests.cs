using System.Reflection.Emit;
using System.Reflection;
using AbstractIrConverters;
using BasicCilCompiler.Contracts;
using BasicCilCompiler.Execution;
using BasicCore.Capabilities;
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
using UniversalToolchain.Dialects.Frontend.Registration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.ModuleContracts;

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
            Assert.That(provider.GetService<Func<IExecutor<CilCompilationOutput>>>(), Is.Null);
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
            Assert.That(provider.GetRequiredService<Func<IExecutor<CilCompilationOutput>>>()(), Is.Not.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Null);
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
            Assert.That(provider.GetService<Func<IExecutor<CilCompilationOutput>>>(), Is.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Null);
        });
    }

    [Test]
    public void WistDialectServiceProviderFactory_ShouldUseNeutralInfrastructurePlusFrontendDefaults()
    {
        var descriptor = new RuntimeBackendDescriptor(new DialectBackendId("noop"), typeof(NoopRegistrar), ["noop"]);
        var backendEntry = BackendEntry("noop", typeof(NoopRegistrar));
        var factory = CreateFactory([new NoopRegistrar(descriptor.BackendId)]);
        var configuration = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(backendEntry, descriptor, [], [], [], false)],
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


    private static WistDialectServiceProviderFactory CreateFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) =>
        new(
            new StaticBackendRegistrarResolver(backendRegistrars),
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator(),
            ModuleContractPipelineProfiles.Warn,
            new InMemoryModuleContractDiagnosticSink());

    private static RuntimeComponentManifestEntry BackendEntry(string alias, Type registrarType)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            registrarType.Assembly.GetName().Name!,
            new RuntimeComponentActivationInfo(
                new RuntimeTypeReference(registrarType.Assembly.GetName().Name!, typeof(object).FullName!),
                new RuntimeTypeReference(registrarType.Assembly.GetName().Name!, registrarType.FullName!)));

    [Test]
    public void CilBackendRegistrar_ShouldRegisterCompilerDefaultsThroughSharedBase()
    {
        var services = CreateBackendServiceCollection();
        var registrar = new WistCilDialectBackendServiceProvider();

        registrar.RegisterRuntime(services, CreateBackendConfiguration(WistDialectBackendIds.Cil, typeof(WistCilDialectBackendServiceProvider), "cil"));

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Not.Null);
            Assert.That(provider.GetService<Func<IExecutor<CilCompilationOutput>>>(), Is.Not.Null);
            Assert.That(provider.GetService<Func<IExecutor<DynamicMethod>>>(), Is.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
            Assert.That(provider.GetServices<WistDialectBackendRuntime>().Single().Descriptor.BackendId, Is.EqualTo(WistDialectBackendIds.Cil));
            Assert.That(provider.GetServices<ToolchainBackendRuntime>().Single().Descriptor.BackendId, Is.EqualTo(WistDialectBackendIds.Cil));
            Assert.That(
                ReadBackendContractModuleIds(provider.GetServices<WistDialectBackendRuntime>().Single().Core),
                Is.EqualTo(new[] { CilBackendContractDescriptorProvider.Module }));
            Assert.That(
                ReadBackendContractModuleIds(provider.GetServices<ToolchainBackendRuntime>().Single().Core),
                Is.EqualTo(new[] { CilBackendContractDescriptorProvider.Module }));
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
            Assert.That(provider.GetServices<ToolchainBackendRuntime>().Single().Descriptor.BackendId, Is.EqualTo(WistDialectBackendIds.Interpreter));
            Assert.That(
                ReadBackendContractModuleIds(provider.GetServices<WistDialectBackendRuntime>().Single().Core),
                Is.EqualTo(new[] { BasicInterpreter.Contracts.InterpreterBackendContractDescriptorProvider.Module }));
            Assert.That(
                ReadBackendContractModuleIds(provider.GetServices<ToolchainBackendRuntime>().Single().Core),
                Is.EqualTo(new[] { BasicInterpreter.Contracts.InterpreterBackendContractDescriptorProvider.Module }));
        });
    }

    private static IReadOnlyList<ModuleId> ReadBackendContractModuleIds(ICoreRunnable core)
    {
        var builderField = core.GetType().GetField("_preparedExecutionBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(builderField, Is.Not.Null);

        var builder = builderField!.GetValue(core);
        Assert.That(builder, Is.Not.Null);

        var componentsField = builder!.GetType().GetField("_backendComponents", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(componentsField, Is.Not.Null);

        var components = componentsField!.GetValue(builder) as IEnumerable<IBackendPipelineComponent>;
        Assert.That(components, Is.Not.Null);

        return components!
            .OfType<IModuleContractBackendPipelineComponent>()
            .SelectMany(static component => component.DescriptorProviders)
            .SelectMany(static provider => provider.GetFacets())
            .Select(static facet => facet.ModuleId)
            .Distinct()
            .OrderBy(static id => id.Value, StringComparer.Ordinal)
            .ToArray();
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

    private sealed class StaticBackendRegistrarResolver(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) : IRuntimeBackendRegistrarResolver
    {
        private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _registrarsById = backendRegistrars.ToDictionary(
            static x => x.BackendId,
            static x => x);

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry)
        {
            if (_registrarsById.TryGetValue(new DialectBackendId(backendEntry.CanonicalAlias), out var registrar))
                return registrar;

            throw new InvalidOperationException($"No test backend runtime registrar is registered for backend '{backendEntry.CanonicalAlias}'.");
        }
    }
}
