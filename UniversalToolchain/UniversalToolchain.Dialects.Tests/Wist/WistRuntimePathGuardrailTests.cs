using Microsoft.Extensions.DependencyInjection;
using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Facade;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistRuntimePathGuardrailTests
{
    [Test]
    public void WistRuntimeFacadeBuilder_DefaultPreset_MatchesDirectWorkflowSelection()
    {
        var presetFile = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.Default);
        using var facade = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(presetFile);
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var expectedShape = shapeBuilder.Build(composition.BuildPlan!, selection);

        Assert.Multiple(() =>
        {
            Assert.That(WistDialectTestInfrastructure.BuildConfigurationSignature(facade.Configuration), Is.EqualTo(WistDialectTestInfrastructure.BuildConfigurationSignature(host.Configuration)));
            Assert.That(facade.Configuration.FrontendModules, Is.SupersetOf(expectedShape.FrontendModuleTypes));
            Assert.That(facade.Configuration.IrModules, Is.EqualTo(expectedShape.IRModuleTypes));
            Assert.That(
                facade.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId),
                Is.EqualTo(expectedShape.BackendEntries.Select(static x => x.CanonicalAlias)));
        });
    }

    [Test]
    public void WistRuntimeFacadeBuilder_WithDialectFile_MatchesDirectWorkflowSelection()
    {
        var dialectFile = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.MinimalArithmetic);

        using var facade = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithDialectFile(dialectFile)
            .Build();
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(dialectFile);
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var expectedShape = shapeBuilder.Build(composition.BuildPlan!, selection);

        Assert.Multiple(() =>
        {
            Assert.That(WistDialectTestInfrastructure.BuildConfigurationSignature(facade.Configuration), Is.EqualTo(WistDialectTestInfrastructure.BuildConfigurationSignature(host.Configuration)));
            Assert.That(facade.Configuration.FrontendModules, Is.SupersetOf(expectedShape.FrontendModuleTypes));
            Assert.That(facade.Configuration.IrModules, Is.EqualTo(expectedShape.IRModuleTypes));
            Assert.That(
                facade.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId),
                Is.EqualTo(expectedShape.BackendEntries.Select(static x => x.CanonicalAlias)));
        });
    }

    [Test]
    public void ComposeText_CanonicalRuntimePath_DoesNotCreateBackendRegistrationSideEffects()
    {
        var registrar = new CountingRegistrar("interpreter");
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddSingleton<IDialectBackendRuntimeRegistrar>(registrar);

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeText("dialect ComposeOnly\nuse Arithmetic,Numbers\nbackend interpreter", "compose-only");

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(composition.RuntimeSelection, Is.InstanceOf<SelectedRuntimePlan>());
            Assert.That(registrar.RegisterRuntimeCallCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void ComposeFile_CanonicalRuntimePath_DoesNotCreateBackendRegistrationSideEffects()
    {
        var registrar = new CountingRegistrar("interpreter");
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddSingleton<IDialectBackendRuntimeRegistrar>(registrar);

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialectFile = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.MinimalArithmetic);

        var composition = workflow.ComposeFile(dialectFile);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(composition.RuntimeSelection, Is.InstanceOf<SelectedRuntimePlan>());
            Assert.That(registrar.RegisterRuntimeCallCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_UsesOnlySelectedModulesAndExplicitRequiredInfrastructure()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();
        var requiredInfrastructure = provider.GetRequiredService<IWistRequiredInfrastructureModulesProvider>();
        var composition = workflow.ComposeText("dialect Minimal\nuse Arithmetic\nbackend interpreter", "minimal");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var shape = shapeBuilder.Build(composition.BuildPlan!, selection);
        var requiredFrontendModules = requiredInfrastructure.GetFrontendModuleTypes();
        var selectedModuleTypes = selection.OrderedModules
            .Select(static x => x.CanonicalAlias)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(selectedModuleTypes, Is.EqualTo(new[] { "Arithmetic" }));
            Assert.That(shape.FrontendModuleTypes, Is.SupersetOf(requiredFrontendModules));
            Assert.That(shape.FrontendModuleTypes.Except(requiredFrontendModules).Select(static x => x.Name), Is.EqualTo(new[] { "ArithmeticModuleImpl" }));
            Assert.That(shape.IRModuleTypes, Is.Empty);
            Assert.That(shape.BackendEntries.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    [Test]
    public void WistRequiredInfrastructureModulesProvider_GetModules_RepeatedCallsProduceEquivalentExplicitInfrastructure()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var requiredInfrastructure = provider.GetRequiredService<IWistRequiredInfrastructureModulesProvider>();

        var frontendSignatures = Enumerable.Range(0, 30)
            .Select(_ => string.Join("|", requiredInfrastructure.GetFrontendModuleTypes().Select(static x => x.FullName)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var irSignatures = Enumerable.Range(0, 30)
            .Select(_ => string.Join("|", requiredInfrastructure.GetIRModuleTypes().Select(static x => x.FullName)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(frontendSignatures, Is.EqualTo(new[] { typeof(ProgramStructureFrontendModule).FullName }));
            Assert.That(irSignatures, Is.EqualTo(new[] { string.Empty }));
        });
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_RepeatedCallsProduceEquivalentShapes()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();
        var composition = workflow.ComposeText(
            "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
            "stable");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var signatures = Enumerable.Range(0, 30)
            .Select(_ => BuildShapeSignature(shapeBuilder.Build(composition.BuildPlan!, selection)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.That(signatures, Has.Length.EqualTo(1));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_Build_RepeatedCallsProduceEquivalentConfigurations()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var configurationBuilder = provider.GetRequiredService<WistDialectExecutionConfigurationBuilder>();
        var composition = workflow.ComposeText(
            "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
            "stable");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var signatures = Enumerable.Range(0, 30)
            .Select(_ => WistDialectTestInfrastructure.BuildConfigurationSignature(configurationBuilder.Build(composition.BuildPlan!, selection)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.That(signatures, Has.Length.EqualTo(1));
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_EquivalentPlansDoNotDependOnDialectNames()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        var shippedNamedComposition = workflow.ComposeText(
            "dialect full-default\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
            "full-default");
        var arbitraryNamedComposition = workflow.ComposeText(
            "dialect RuntimePathGuardrail\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
            "runtime-path-guardrail");

        Assert.Multiple(() =>
        {
            Assert.That(shippedNamedComposition.IsSuccess, Is.True, FormatComposition(shippedNamedComposition));
            Assert.That(arbitraryNamedComposition.IsSuccess, Is.True, FormatComposition(arbitraryNamedComposition));
        });

        var shippedShape = BuildNameIndependentShapeSignature(shapeBuilder.Build(
            shippedNamedComposition.BuildPlan!,
            (SelectedRuntimePlan)shippedNamedComposition.RuntimeSelection!));
        var arbitraryShape = BuildNameIndependentShapeSignature(shapeBuilder.Build(
            arbitraryNamedComposition.BuildPlan!,
            (SelectedRuntimePlan)arbitraryNamedComposition.RuntimeSelection!));

        Assert.That(arbitraryShape, Is.EqualTo(shippedShape));
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_PreservesSelectedModuleOrderWithStableDeduplication()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();
        var typeLoader = provider.GetRequiredService<IRuntimeComponentTypeLoader>();
        var requiredInfrastructure = provider.GetRequiredService<IWistRequiredInfrastructureModulesProvider>();
        var composition = workflow.ComposeText(
            "dialect Ordered\nuse Arithmetic,Whitespaces,Numbers\nbackend interpreter",
            "ordered");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var shape = shapeBuilder.Build(composition.BuildPlan!, selection);

        var expectedFrontendTypes = DeduplicateStable(
            requiredInfrastructure.GetFrontendModuleTypes()
                .Concat(selection.OrderedModules
                    .Select(typeLoader.LoadType)
                    .Where(static x => typeof(IFrontendCoreModule).IsAssignableFrom(x))));
        var expectedIrTypes = DeduplicateStable(
            requiredInfrastructure.GetIRModuleTypes()
                .Concat(selection.OrderedModules
                    .Select(typeLoader.LoadType)
                    .Where(static x => typeof(IIRProcessingModule).IsAssignableFrom(x))));

        Assert.Multiple(() =>
        {
            Assert.That(shape.FrontendModuleTypes, Is.EqualTo(expectedFrontendTypes));
            Assert.That(shape.IRModuleTypes, Is.EqualTo(expectedIrTypes));
            Assert.That(shape.BackendEntries.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition)
    {
        return DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));
    }

    private static string BuildShapeSignature(SelectedRuntimeExecutionShape shape)
    {
        return shape.DialectName
               + "::"
               + BuildNameIndependentShapeSignature(shape);
    }

    private static string BuildNameIndependentShapeSignature(SelectedRuntimeExecutionShape shape)
    {
        return string.Join("|", shape.FrontendModuleTypes.Select(static x => x.FullName))
               + "::"
               + string.Join("|", shape.IRModuleTypes.Select(static x => x.FullName))
               + "::"
               + string.Join("|", shape.OptimizerEntries.Select(static x => x.CanonicalAlias))
               + "::"
               + string.Join("|", shape.BackendEntries.Select(static x => x.CanonicalAlias));
    }

    private static IReadOnlyList<Type> DeduplicateStable(IEnumerable<Type> types)
    {
        var snapshot = new List<Type>();
        var seen = new HashSet<Type>();

        foreach (var type in types)
        {
            if (seen.Add(type))
            {
                snapshot.Add(type);
            }
        }

        return snapshot;
    }

    private sealed class CountingRegistrar(string backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new(backendId);

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public int RegisterRuntimeCallCount { get; private set; }

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            RegisterRuntimeCallCount++;
            services.AddSingleton(typeof(object), new object());
        }
    }
}
