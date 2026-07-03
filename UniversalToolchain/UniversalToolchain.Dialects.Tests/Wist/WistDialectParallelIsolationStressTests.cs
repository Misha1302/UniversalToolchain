using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

public class WistDialectParallelIsolationStressTests
{
    private const int ParallelCount = 32;
    private const int RepeatCount = 100;

    [Test]
    public async Task ComposeText_ParallelCalls_ShouldNotMixRuntimeSelections()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialects = new[]
        {
            "dialect A\nuse Arithmetic,Numbers\n\nbackend interpreter",
            "dialect B\nuse Arithmetic,Scopes,Variables\nbackend interpreter,compiler",
            "dialect C\nuse Arithmetic,Conditions,ComparisonConditions\nbackend compiler"
        };

        var results = await Task.WhenAll(Enumerable.Range(0, ParallelCount)
            .Select(i => Task.Run(() => workflow.ComposeText(dialects[i % dialects.Length], $"parallel-{i}"))));

        Assert.That(results.All(static x => x.IsSuccess), Is.True, string.Join(Environment.NewLine, results.Select(static x => FormatComposition(x))));

        var signatures = results.Select(WistDialectTestInfrastructure.BuildSelectionSignature).Distinct(StringComparer.Ordinal).ToArray();
        Assert.That(signatures.Length, Is.EqualTo(dialects.Length));
    }

    [Test]
    public async Task CreateHost_ParallelCalls_ShouldNotMixBackendConfigurations()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers\n\nbackend compiler,interpreter", "stable");
        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var signatures = await Task.WhenAll(Enumerable.Range(0, ParallelCount).Select(_ => Task.Run(() =>
        {
            using var host = workflow.CreateHost(composition);
            return WistDialectTestInfrastructure.BuildHostSignature(host);
        })));

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void RepeatedCompose_ShouldNotAccumulateRuntimeMetadata()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var signatures = new List<string>(RepeatCount);

        for (var i = 0; i < RepeatCount; i++)
        {
            var result = workflow.ComposeText("dialect Repeat\nuse Arithmetic,Numbers,Whitespaces\n\nbackend interpreter", $"repeat-{i}");
            Assert.That(result.IsSuccess, Is.True, FormatComposition(result));
            signatures.Add(WistDialectTestInfrastructure.BuildSelectionSignature(result));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void RepeatedCreateHost_ShouldNotAccumulateRegistrations()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect RepeatHost\nuse Arithmetic,Numbers\nbackend interpreter,compiler", "repeat-host");
        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            using var host = workflow.CreateHost(composition);
            signatures.Add(WistDialectTestInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ParallelComposeAndCreateHost_ShouldRemainDeterministic()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var signatures = await Task.WhenAll(Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            var composition = workflow.ComposeText("dialect Mixed\nuse Arithmetic,Numbers,Variables\n\nbackend interpreter,compiler", $"mixed-{i}");
            if (!composition.IsSuccess)
                return "compose-failed:" + FormatComposition(composition);

            using var host = workflow.CreateHost(composition);
            return WistDialectTestInfrastructure.BuildSelectionSignature(composition) + "##" + WistDialectTestInfrastructure.BuildHostSignature(host);
        })));

        Assert.That(signatures.All(static x => !x.StartsWith("compose-failed:", StringComparison.Ordinal)), Is.True, string.Join(Environment.NewLine, signatures));
        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void FailedComposition_ShouldNotPoisonNextSuccessfulComposition()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var failed = workflow.ComposeText("dialect Broken\nuse MissingModule\nbackend interpreter", "broken");
        var first = workflow.ComposeText("dialect Good\nuse Arithmetic,Numbers\nbackend interpreter", "good-1");
        var second = workflow.ComposeText("dialect Good\nuse Arithmetic,Numbers\nbackend interpreter", "good-2");

        Assert.Multiple(() =>
        {
            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(first.IsSuccess, Is.True, FormatComposition(first));
            Assert.That(second.IsSuccess, Is.True, FormatComposition(second));
            Assert.That(WistDialectTestInfrastructure.BuildSelectionSignature(first), Is.EqualTo(WistDialectTestInfrastructure.BuildSelectionSignature(second)));
        });
    }

    [Test]
    public void RepeatedKnownBackendResolution_ShouldRemainStable()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Backends\nuse Arithmetic\nbackend compiler,interpreter", "backends");
        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            using var host = workflow.CreateHost(composition);
            signatures.Add(string.Join("|", host.Configuration.EnabledBackends.Select(static x => x.CanonicalId)));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ParallelTypeLoading_ShouldRemainDeterministic()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var loader = provider.GetRequiredService<IRuntimeComponentTypeLoader>();

        var composition = workflow.ComposeText("dialect TypeLoad\nuse Arithmetic,Numbers\n\nbackend compiler,interpreter", "typeload");
        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var selection = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var entries = selection.OrderedModules.Concat(selection.EnabledOptimizers).Concat(selection.EnabledBackends).ToArray();

        var signatures = await Task.WhenAll(Enumerable.Range(0, ParallelCount).Select(_ => Task.Run(() => string.Join("|", entries.Select(loader.LoadType).Select(static x => x.FullName)))));
        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) => DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));
}
