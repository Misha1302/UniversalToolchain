using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;
using Wistc;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistCliCustomizedDialectBuilderTests
{
    [Test]
    public void BuildFromPreset_PreservesBackendDirectives_FromBasePreset()
    {
        var dialectText = BuildFromDefaultPreset();

        Assert.That(dialectText, Does.Contain("backend cil,interpreter"));
    }

    [Test]
    public void BuildFromPreset_PreservesEnableDirectives_FromBasePreset()
    {
        var dialectText = BuildFromDefaultPreset();

        Assert.Multiple(() =>
        {
            Assert.That(dialectText, Does.Contain("enable BooleanOptimization"));
            Assert.That(dialectText, Does.Contain("enable ComparisonIntrinsicOptimization"));
            Assert.That(dialectText, Does.Not.Contain("LocalVariablesOptimization"));
        });
    }

    [Test]
    public void BuildFromPreset_PreservesSecurityAndCapabilityDirectives_FromBasePreset()
    {
        var dialectText = BuildFromDefaultPreset();

        Assert.Multiple(() =>
        {
            Assert.That(dialectText, Does.Contain("security trusted"));
            Assert.That(dialectText, Does.Contain("capability unsafe-interop"));
        });
    }

    [Test]
    public void BuildFromPreset_AddsIncludedModules_Once()
    {
        var dialectText = new WistCliCustomizedDialectBuilder().BuildFromPreset(
            WistShippedDialectPresets.Default,
            new WistCliCustomizationRequest(false, ["ExtraModule", "ExtraModule"], []));

        Assert.That(CountModuleOccurrences(dialectText, "ExtraModule"), Is.EqualTo(1));
    }

    [Test]
    public void BuildFromPreset_RemovesExcludedModules()
    {
        var dialectText = new WistCliCustomizedDialectBuilder().BuildFromPreset(
            WistShippedDialectPresets.Default,
            new WistCliCustomizationRequest(false, [], ["CSharpInterop"]));

        Assert.That(GetUseModules(dialectText), Does.Not.Contain("CSharpInterop"));
    }

    [Test]
    public void BuildFromPreset_Result_ComposesSuccessfully()
    {
        var dialectText = new WistCliCustomizedDialectBuilder().BuildFromPreset(
            WistShippedDialectPresets.Default,
            new WistCliCustomizationRequest(false, [], ["CSharpInterop"]));

        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "cli-customized-test");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(
            DialectCompositionExplanationProjector.Project(composition)));
    }

    private static string BuildFromDefaultPreset()
        => new WistCliCustomizedDialectBuilder().BuildFromPreset(
            WistShippedDialectPresets.Default,
            new WistCliCustomizationRequest(false, [], []));

    private static IReadOnlyList<string> GetUseModules(string dialectText)
    {
        var useLine = dialectText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static x => x.Trim())
            .First(static x => x.StartsWith("use ", StringComparison.Ordinal));

        return useLine[4..].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(static x => x.Trim())
            .ToList();
    }

    private static int CountModuleOccurrences(string dialectText, string module)
        => GetUseModules(dialectText).Count(x => string.Equals(x, module, StringComparison.OrdinalIgnoreCase));

}
