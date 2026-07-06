using ArithmeticModule;
using ArithmeticModule.Module;
using CSharpInteropModule;
using CSharpInteropModule.Module;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class ModuleOwnedFeatureCatalogTests
{
    [Test]
    public void ModuleOwnedFeatures_ArithmeticProvider_DiscoveredFromArithmeticModule()
    {
        var catalog = new KnownCapabilityCatalogBuilder().Build([typeof(ArithmeticModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Providers.Select(static x => x.ProviderType), Is.EqualTo(new[] { typeof(ArithmeticCapabilityProvider) }));
            Assert.That(catalog.LanguageFeatures.Select(static x => x.FeatureId.Value), Is.EqualTo(new[] { "ArithmeticExpressions" }));
        });
    }

    [Test]
    public void ModuleOwnedFeatures_CSharpInteropProvider_DiscoveredOnlyWhenModuleKnown()
    {
        var arithmeticOnlyCatalog = new KnownCapabilityCatalogBuilder().Build([typeof(ArithmeticModuleImpl)]);
        var withInteropCatalog = new KnownCapabilityCatalogBuilder().Build([typeof(ArithmeticModuleImpl), typeof(CSharpInteropModuleImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(arithmeticOnlyCatalog.LanguageFeatures.Select(static x => x.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
            Assert.That(withInteropCatalog.Providers.Select(static x => x.ProviderType), Does.Contain(typeof(CSharpInteropCapabilityProvider)));
            Assert.That(withInteropCatalog.LanguageFeatures.Select(static x => x.FeatureId.Value), Does.Contain("CSharpInterop"));
        });
    }

    [Test]
    public void FeatureExplanation_MinimalArithmetic_ContainsArithmeticButNotCSharpInterop()
    {
        var explanation = BuildExplanation("minimal-arithmetic");

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value), Does.Contain("ArithmeticExpressions"));
            Assert.That(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
            Assert.That(explanation.UnavailableKnownFeatures.Select(static x => x.Feature.FeatureId.Value), Does.Contain("CSharpInterop"));
        });
    }

    [Test]
    public void FeatureExplanation_RestrictedProfile_DoesNotExposeCSharpInterop()
    {
        var explanation = BuildExplanation("restricted-sandbox");

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value), Does.Not.Contain("CSharpInterop"));
            Assert.That(explanation.UnavailableKnownFeatures.Select(static x => x.Feature.FeatureId.Value), Does.Contain("CSharpInterop"));
        });
    }

    [Test]
    public void FeatureExplanation_DeterministicFormatting()
    {
        var explanation = BuildExplanation("full-default");

        var first = DialectFeatureExplanationFormatter.FormatDeterministic(explanation);
        var second = DialectFeatureExplanationFormatter.FormatDeterministic(explanation);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Architecture_NoCentralWistLanguageFeatureCatalog()
    {
        var repoRoot = GetRepositoryRoot();
        var centralCatalogCandidates = Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path =>
                path.EndsWith("WistLanguageFeatureCatalog.cs", StringComparison.Ordinal) ||
                path.EndsWith("WistLanguageFeatureIds.cs", StringComparison.Ordinal))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.That(centralCatalogCandidates, Is.Empty);
    }

    private static DialectFeatureExplanation BuildExplanation(string dialectName)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var runtimeComponentCatalog = provider.GetRequiredService<IRuntimeComponentCatalog>();
        var typeLoader = provider.GetRequiredService<IRuntimeComponentTypeLoader>();
        var dialectPath = GetDialectPath(dialectName);
        var composition = workflow.ComposeFile(dialectPath);

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        var selectedRuntimePlan = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var knownCatalog = new KnownCapabilityCatalogBuilder(typeLoader).Build(runtimeComponentCatalog);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder(typeLoader).Build(selectedRuntimePlan);

        return DialectFeatureExplanationProjector.Project(
            knownCatalog,
            selectedCatalog,
            selectedRuntimePlan,
            composition.BuildPlan!.Name);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static string GetDialectPath(string dialectName) =>
        UniversalToolchain.Dialects.Tests.TestSourcePaths.WistExampleDialectPath(dialectName);

    private static string GetRepositoryRoot() => UniversalToolchain.Dialects.Tests.TestSourcePaths.RepositoryRoot;
}