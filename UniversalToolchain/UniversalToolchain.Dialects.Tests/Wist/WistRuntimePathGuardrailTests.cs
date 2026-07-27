using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Frontend;
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
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(presetFile);
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var expectedShape = shapeBuilder.Build(composition.BuildPlan!, selection);

        Assert.Multiple(() =>
        {
            Assert.That(
                WistDialectTestInfrastructure.BuildSelectionSignature(facade.Composition),
                Is.EqualTo(WistDialectTestInfrastructure.BuildSelectionSignature(composition)));
            Assert.That(
                WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(facade.Composition),
                Is.EqualTo(WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(composition)));
            Assert.That(WistDialectTestInfrastructure.BuildConfigurationSignature(facade.Configuration), Is.EqualTo(WistDialectTestInfrastructure.BuildConfigurationSignature(host.Configuration)));
            Assert.That(
                facade.Configuration.FrontendModules.Select(static type => type.FullName),
                Is.SupersetOf(expectedShape.FrontendModuleTypes.Select(static type => type.FullName)));
            Assert.That(
                facade.Configuration.IrModules.Select(static type => type.FullName),
                Is.EqualTo(expectedShape.IrModuleTypes.Select(static type => type.FullName)));
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
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(dialectFile);
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var expectedShape = shapeBuilder.Build(composition.BuildPlan!, selection);

        Assert.Multiple(() =>
        {
            Assert.That(
                WistDialectTestInfrastructure.BuildSelectionSignature(facade.Composition),
                Is.EqualTo(WistDialectTestInfrastructure.BuildSelectionSignature(composition)));
            Assert.That(
                WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(facade.Composition),
                Is.EqualTo(WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(composition)));
            Assert.That(WistDialectTestInfrastructure.BuildConfigurationSignature(facade.Configuration), Is.EqualTo(WistDialectTestInfrastructure.BuildConfigurationSignature(host.Configuration)));
            Assert.That(
                facade.Configuration.FrontendModules.Select(static type => type.FullName),
                Is.SupersetOf(expectedShape.FrontendModuleTypes.Select(static type => type.FullName)));
            Assert.That(
                facade.Configuration.IrModules.Select(static type => type.FullName),
                Is.EqualTo(expectedShape.IrModuleTypes.Select(static type => type.FullName)));
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
    public void CreateRuntimeHost_CanonicalPath_RunsThroughNeutralRuntimeHost()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            "dialect NeutralHost\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter",
            "neutral-host");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var runtimeHost = workflow.CreateRuntimeHost(composition);
        var result = runtimeHost.Run("2 + 3 * 4", "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(runtimeHost, Is.InstanceOf<ToolchainRuntimeHost>());
            Assert.That(runtimeHost.Configuration.DialectName, Is.EqualTo("NeutralHost"));
            Assert.That(result?.ToString(), Is.EqualTo("14"));
        });
    }

    [Test]
    public void CreateRuntimeHost_RequestEnvelope_RunsThroughNeutralRuntimeHost()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            "dialect NeutralRequest\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter",
            "neutral-request");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var runtimeHost = workflow.CreateRuntimeHost(composition);
        var result = runtimeHost.Run(new ToolchainRuntimeRunRequest("2 + 5", "interpreter"));

        Assert.Multiple(() =>
        {
            Assert.That(result.DialectName, Is.EqualTo("NeutralRequest"));
            Assert.That(result.Backend, Is.EqualTo("interpreter"));
            Assert.That(result.Value?.ToString(), Is.EqualTo("7"));
        });
    }

    [Test]
    public void ComposeText_WithTypedRuntimeProfile_InvokesDialectParserExactlyOnce()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var countingFactory = new CountingCompilerFactory(provider.GetRequiredService<IDialectDslCompilerFactory>());
        var neutralWorkflow = new ToolchainCompositionWorkflow(
            countingFactory,
            provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>(),
            provider.GetRequiredService<SelectedRuntimePlanResolver>());
        var workflow = new WistDialectExecutionWorkflow(
            neutralWorkflow,
            provider.GetRequiredService<WistDialectExecutionConfigurationBuilder>(),
            provider.GetRequiredService<WistDialectServiceProviderFactory>());
        var profile = new RuntimeProfileDefinition(
            "typed-overlay",
            defaultModules: ["Arithmetic", "Numbers", "Whitespaces"],
            defaultBackends: [new DialectBackendId("interpreter")]);

        var composition = workflow.ComposeText("dialect TypedOverlay", "typed-overlay", profile);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(countingFactory.CreateCount, Is.EqualTo(1),
                "A typed runtime profile must not be rendered to DSL and parsed a second time.");
            Assert.That(composition.CompiledDialect, Is.Not.Null);
            Assert.That(composition.BuildPlan!.OrderedModules, Is.EqualTo(new[] { "Arithmetic", "Numbers", "Whitespaces" }));
        });
    }

    [Test]
    public void ComposeText_WithRuntimeProfile_AppliesProfileDefaultsBeforeSelection()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var profile = new RuntimeProfileDefinition(
            "minimal-interpreter",
            defaultModules: ["Arithmetic", "Numbers", "Whitespaces"],
            defaultBackends: [new DialectBackendId("interpreter")]);

        var composition = workflow.ComposeText("dialect ProfiledRuntime", "profiled-runtime", profile);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(composition.BuildPlan!.OrderedModules, Is.SupersetOf(new[] { "Arithmetic", "Numbers", "Whitespaces" }));
            Assert.That(
                ((SelectedRuntimePlan)composition.RuntimeSelection!).EnabledBackends.Select(static x => x.CanonicalAlias),
                Is.EqualTo(new[] { "interpreter" }));
        });
    }

    [Test]
    public void ComposeText_WithRuntimeProfile_AppliesDefaultsToCompactWistSyntax()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var profile = RuntimeProfileDefinitionBuilder
            .Create("ssa")
            .EnableOptimizer("Ssa")
            .Build();

        var composition = workflow.ComposeText(
            "dialect Compact\nuse Identifier,NativeTypes,Scopes,Variables,Whitespaces\nbackend cil,interpreter\nsecurity restricted",
            "compact-profile",
            profile,
            RuntimeProfileOverridePolicy.StrictNoConflicts);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(composition.BuildPlan!.OptimizerDirectives.Any(static x => x.Name == "Ssa" && x.Enabled), Is.True);
        });
    }

    [Test]
    public void ComposeText_WithRuntimeProfile_StrictModeRejectsDisabledOptimizerConflict()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var profile = RuntimeProfileDefinitionBuilder
            .Create("ssa")
            .EnableOptimizer("Ssa")
            .Build();

        var composition = workflow.ComposeText(
            "dialect Conflict\ndisable Ssa\nbackend interpreter",
            "compact-conflict",
            profile,
            RuntimeProfileOverridePolicy.StrictNoConflicts);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.False);
            Assert.That(composition.SemanticDiagnostics.Any(static x => x.Code == "R301"), Is.True);
        });
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_UsesOnlySelectedModulesAndExplicitRequiredInfrastructure()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
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
            Assert.That(shape.IrModuleTypes, Is.Empty);
            Assert.That(shape.BackendEntries.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    [Test]
    public void WistRequiredInfrastructureModulesProvider_GetModules_RepeatedCallsProduceEquivalentExplicitInfrastructure()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var requiredInfrastructure = provider.GetRequiredService<IWistRequiredInfrastructureModulesProvider>();

        var frontendSignatures = Enumerable.Range(0, 30)
            .Select(_ => string.Join("|", requiredInfrastructure.GetFrontendModuleTypes().Select(static x => x.FullName)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var irSignatures = Enumerable.Range(0, 30)
            .Select(_ => string.Join("|", requiredInfrastructure.GetIrModuleTypes().Select(static x => x.FullName)))
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
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();
        var composition = workflow.ComposeText(
            "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\n\nbackend interpreter,cil",
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
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var configurationBuilder = provider.GetRequiredService<WistDialectExecutionConfigurationBuilder>();
        var composition = workflow.ComposeText(
            "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\n\nbackend interpreter,cil",
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
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var shapeBuilder = provider.GetRequiredService<SelectedRuntimeExecutionShapeBuilder>();

        var shippedNamedComposition = workflow.ComposeText(
            "dialect full-default\nuse Arithmetic,Numbers,Whitespaces\n\nbackend interpreter,cil",
            "full-default");
        var arbitraryNamedComposition = workflow.ComposeText(
            "dialect RuntimePathGuardrail\nuse Arithmetic,Numbers,Whitespaces\n\nbackend interpreter,cil",
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
    public void WistDialectExecutionConfigurationBuilder_Build_DuplicateSelectedModulesDoNotDriftFromCanonicalConfiguration()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var resolver = provider.GetRequiredService<SelectedRuntimePlanResolver>();
        var configurationBuilder = provider.GetRequiredService<WistDialectExecutionConfigurationBuilder>();

        var canonicalPlan = new DialectBuildPlan(
            "Stable",
            null,
            ["Arithmetic", "Numbers"],
            [WistDialectBackendIds.Interpreter],
            [],
            [],
            [],
            null,
            [],
            new DialectValidationResult([]));
        var duplicatePlan = new DialectBuildPlan(
            "Stable",
            null,
            ["Arithmetic", "Numbers", "Arithmetic", "Numbers"],
            [WistDialectBackendIds.Interpreter],
            [],
            [],
            [],
            null,
            [],
            new DialectValidationResult([]));

        var canonicalSelection = resolver.Resolve(canonicalPlan);
        var duplicateSelection = resolver.Resolve(duplicatePlan);
        var canonicalConfiguration = configurationBuilder.Build(canonicalPlan, canonicalSelection);
        var duplicateConfiguration = configurationBuilder.Build(duplicatePlan, duplicateSelection);

        Assert.Multiple(() =>
        {
            Assert.That(canonicalSelection.IsResolved, Is.True);
            Assert.That(duplicateSelection.IsResolved, Is.True);
            Assert.That(duplicateSelection.OrderedModules.Select(static x => x.CanonicalAlias), Is.EqualTo(canonicalSelection.OrderedModules.Select(static x => x.CanonicalAlias)));
            Assert.That(
                WistDialectTestInfrastructure.BuildConfigurationSignature(duplicateConfiguration),
                Is.EqualTo(WistDialectTestInfrastructure.BuildConfigurationSignature(canonicalConfiguration)));
        });
    }

    [Test]
    public void SelectedRuntimeExecutionShapeBuilder_Build_PreservesSelectedModuleOrderWithStableDeduplication()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
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
            requiredInfrastructure.GetIrModuleTypes()
                .Concat(selection.OrderedModules
                    .Select(typeLoader.LoadType)
                    .Where(static x => typeof(IAirOptimizer).IsAssignableFrom(x))));

        Assert.Multiple(() =>
        {
            Assert.That(shape.FrontendModuleTypes, Is.EqualTo(expectedFrontendTypes));
            Assert.That(shape.IrModuleTypes, Is.EqualTo(expectedIrTypes));
            Assert.That(shape.BackendEntries.Select(static x => x.CanonicalAlias), Is.EqualTo(new[] { "interpreter" }));
        });
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) => DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));

    private static string BuildShapeSignature(SelectedRuntimeExecutionShape shape) =>
        shape.DialectName
        + "::"
        + BuildNameIndependentShapeSignature(shape);

    private static string BuildNameIndependentShapeSignature(SelectedRuntimeExecutionShape shape)
    {
        return string.Join("|", shape.FrontendModuleTypes.Select(static x => x.FullName))
               + "::"
               + string.Join("|", shape.IrModuleTypes.Select(static x => x.FullName))
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
                snapshot.Add(type);
        }

        return snapshot;
    }

    private sealed class CountingCompilerFactory(IDialectDslCompilerFactory inner) : IDialectDslCompilerFactory
    {
        public int CreateCount { get; private set; }

        public DialectDslCompiler Create()
        {
            CreateCount++;
            return inner.Create();
        }
    }

    private sealed class CountingRegistrar(string backendId) : IDialectBackendRuntimeRegistrar
    {
        public int RegisterRuntimeCallCount { get; private set; }
        public DialectBackendId BackendId { get; } = new(backendId);

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            RegisterRuntimeCallCount++;
            services.AddSingleton(typeof(object), new object());
        }
    }
}
