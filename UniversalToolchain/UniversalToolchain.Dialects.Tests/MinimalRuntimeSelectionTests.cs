using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class MinimalRuntimeSelectionTests
{
    [Test]
    public void SelectionResolver_SameDialect_RepeatedRuns_ProduceSameSelectionOrder()
    {
        using var provider = CreateMinimalProvider();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var builder = provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>();
        var resolver = provider.GetRequiredService<SelectedRuntimePlanResolver>();
        const string source = """
            dialect Deterministic
            use Arithmetic,Numbers,Variables
            backend interpreter,compiler
            enable LocalVariablesOptimization
            """;

        string? signature = null;
        for (var i = 0; i < 100; i++)
        {
            var plan = builder.Build(compiler.Compile(source));
            var selection = resolver.Resolve(plan);
            var current = string.Join("|", selection.OrderedModules.Select(x => x.CanonicalAlias)) + "::" + string.Join("|", selection.EnabledBackends.Select(x => x.CanonicalAlias)) + "::" + string.Join("|", selection.EnabledOptimizers.Select(x => x.CanonicalAlias));
            signature ??= current;
            Assert.That(current, Is.EqualTo(signature));
        }
    }

    [Test]
    public void SelectionResolver_MissingModule_AddsR001()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var plan = BuildPlan(modules: ["NoSuchModule"], backends: [WistDialectBackendIds.Interpreter], optimizers: []);
        var selection = resolver.Resolve(plan);
        Assert.That(selection.Diagnostics.Any(x => x.Code == "R001"), Is.True);
    }

    [Test]
    public void SelectionResolver_MissingBackend_AddsR002()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var plan = BuildPlan(modules: ["Arithmetic"], backends: [new DialectBackendId("missing")], optimizers: []);
        var selection = resolver.Resolve(plan);
        Assert.That(selection.Diagnostics.Any(x => x.Code == "R002"), Is.True);
    }

    [Test]
    public void SelectionResolver_MissingOptimizer_AddsR003()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var plan = BuildPlan(modules: ["Arithmetic"], backends: [WistDialectBackendIds.Interpreter], optimizers: [new OptimizerBuildDirective("missing-opt", true, DialectBackendSelector.Any)]);
        var selection = resolver.Resolve(plan);
        Assert.That(selection.Diagnostics.Any(x => x.Code == "R003"), Is.True);
    }


    [Test]
    public void SelectionResolver_DropsDuplicateBackendSelections_Deterministically()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var plan = BuildPlan(modules: ["Arithmetic"], backends: [WistDialectBackendIds.Interpreter, WistDialectBackendIds.Interpreter, WistDialectBackendIds.Cil], optimizers: []);
        var selection = resolver.Resolve(plan);

        Assert.That(selection.EnabledBackends.Select(x => x.CanonicalAlias), Is.EqualTo(new[] { "cil", "interpreter" }));
    }

    [Test]
    public void SelectionResolver_DropsDuplicateOptimizers_Deterministically()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var plan = BuildPlan(
            modules: ["Arithmetic"],
            backends: [WistDialectBackendIds.Interpreter],
            optimizers:
            [
                new OptimizerBuildDirective("LocalVariablesOptimization", true, DialectBackendSelector.Any),
                new OptimizerBuildDirective("LocalVariablesOptimization", true, DialectBackendSelector.Any)
            ]);

        var selection = resolver.Resolve(plan);
        var optimizerAliases = selection.EnabledOptimizers.Select(x => x.CanonicalAlias).ToList();
        Assert.That(optimizerAliases.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(optimizerAliases.Count));
    }

    [Test]
    public void SelectionResolver_DoesNotLoadFeatureAssemblies()
    {
        var resolver = new SelectedRuntimePlanResolver(new FileBasedRuntimeComponentCatalog(new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()), new RuntimeManifestJsonSerializer()));
        var before = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).ToHashSet(StringComparer.Ordinal);
        var plan = BuildPlan(modules: ["Arithmetic"], backends: [WistDialectBackendIds.Interpreter], optimizers: []);
        _ = resolver.Resolve(plan);
        var after = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(after.Contains("ArithmeticModule"), Is.EqualTo(before.Contains("ArithmeticModule")));
            Assert.That(after.Contains("NativeMathModule"), Is.EqualTo(before.Contains("NativeMathModule")));
            Assert.That(after.Contains("ConditionsModule"), Is.EqualTo(before.Contains("ConditionsModule")));
        });
    }

    private static DialectBuildPlan BuildPlan(IReadOnlyList<string> modules, IReadOnlyList<DialectBackendId> backends, IReadOnlyList<OptimizerBuildDirective> optimizers)
    {
        return new DialectBuildPlan("Demo", null, modules, backends, [], [], optimizers, null, [], new DialectValidationResult([]));
    }

    private static ServiceProvider CreateMinimalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }
}
