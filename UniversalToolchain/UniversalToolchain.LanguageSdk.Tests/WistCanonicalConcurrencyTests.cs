using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCanonicalConcurrencyTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public async Task ParallelDslPlanning_DoesNotMixSelectedFeaturesOrBackends()
    {
        var cases = new[]
        {
            new DialectCase(
                "dialect ArithmeticOnly\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted",
                new[] { WistFeatureIds.Arithmetic, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces },
                new[] { "interpreter" }),
            new DialectCase(
                "dialect Variables\nuse Arithmetic,Numbers,Scopes,Variables,Whitespaces\nbackend interpreter,cil\nsecurity restricted",
                new[] { WistFeatureIds.Arithmetic, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Variables, WistFeatureIds.Whitespaces },
                new[] { "cil", "interpreter" }),
            new DialectCase(
                "dialect Conditions\nuse Arithmetic,Conditions,Numbers,Scopes,Whitespaces\nbackend cil\nsecurity restricted",
                new[] { WistFeatureIds.Arithmetic, WistFeatureIds.Conditions, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces },
                new[] { "cil" })
        };

        var results = await Task.WhenAll(Enumerable.Range(0, 48).Select(index => Task.Run(() =>
        {
            var testCase = cases[index % cases.Length];
            var plan = Compile(testCase.Source, $"parallel-{index}.wistdialect");
            return new
            {
                Case = testCase,
                Features = plan.Definition.SelectedFeatures.OrderBy(static x => x.Value, StringComparer.Ordinal).ToArray(),
                Backends = plan.Definition.Backends.Select(static x => x.Value).OrderBy(static x => x, StringComparer.Ordinal).ToArray()
            };
        })));

        Assert.Multiple(() =>
        {
            foreach (var result in results)
            {
                Assert.That(
                    result.Features,
                    Is.EqualTo(result.Case.Features.OrderBy(static x => x.Value, StringComparer.Ordinal)),
                    result.Case.Source);
                Assert.That(
                    result.Backends,
                    Is.EqualTo(result.Case.Backends.OrderBy(static x => x, StringComparer.Ordinal)),
                    result.Case.Source);
            }
        });
    }

    [Test]
    public void RepeatedDslPlanning_ProducesOneStablePlanIdentity()
    {
        const string source = "dialect Stable\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";

        var hashes = Enumerable.Range(0, 64)
            .Select(index => Compile(source, $"stable-{index}.wistdialect").PlanHash)
            .ToArray();

        Assert.That(hashes.Distinct(StringComparer.Ordinal), Has.Count.EqualTo(1));
    }

    [Test]
    public void FailedDslPlanning_DoesNotPoisonNextSuccessfulPlan()
    {
        const string invalid = "dialect Broken\nuse MissingModule\nbackend interpreter\nsecurity restricted";
        const string valid = "dialect Good\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";

        Assert.Throws<Exception>(() => Compile(invalid, "broken.wistdialect"));
        var first = Compile(valid, "good-1.wistdialect");
        var second = Compile(valid, "good-2.wistdialect");

        Assert.Multiple(() =>
        {
            Assert.That(first.PlanHash, Is.EqualTo(second.PlanHash));
            Assert.That(first.Definition.SelectedFeatures, Is.EqualTo(second.Definition.SelectedFeatures));
            Assert.That(first.Definition.Backends, Is.EqualTo(second.Definition.Backends));
        });
    }

    [Test]
    public async Task ParallelExactRuntimeSessions_RemainIndependentAndDeterministic()
    {
        const string source = "dialect RuntimeStable\nuse Arithmetic,Numbers,Scopes,Whitespaces\nbackend interpreter\nsecurity restricted";
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            source,
            "runtime-stable.wistdialect",
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();

        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            using var runtime = LanguageRuntime.Create(
                plan,
                new ILanguageRouteComponentSource[] { package });
            return runtime.Run(new LanguageExecutionRequest("2 + 3", Interpreter)).Value?.ToString();
        })));

        Assert.That(results, Is.All.EqualTo("5"));
    }

    private static LanguagePlan Compile(string source, string sourceName)
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            source,
            sourceName,
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);
        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
    }

    private sealed record DialectCase(
        string Source,
        IReadOnlyList<LanguageFeatureId> Features,
        IReadOnlyList<string> Backends);
}
