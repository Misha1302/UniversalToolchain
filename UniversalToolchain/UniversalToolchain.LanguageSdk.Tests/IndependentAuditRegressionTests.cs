using UniversalToolchain.Dialects.Wist.Facade;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class IndependentAuditRegressionTests
{
    [Test]
    public void WistProvider_RejectsCSharpInteropWhenRuntimePolicyForbidsIt()
    {
        var definition = LanguageDefinitionBuilder.Create("Wist.Restricted.Interop", "1")
            .UseFeature(WistFeatureIds.CSharpInterop)
            .EnableBackend("interpreter")
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .WithRuntimePolicy(new LanguageRuntimePolicy(AllowHostInterop: false))
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage()))
            .Compile(definition)
            .GetRequiredPlan();

        var exception = Assert.Throws<InvalidOperationException>(() => LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider())));

        Assert.That(exception!.Message, Does.Contain("host interop"));

        var provider = new WistLanguageRuntimeProvider();
        var directException = Assert.Throws<InvalidOperationException>(() =>
            provider.CreateSession(plan, new LanguageRuntimeOptions()));
        Assert.That(directException!.Message, Does.Contain("host interop"));
    }

    [Test]
    public void ShippedInteropPresets_DeclareTypedHostInteropPolicyWithoutBehavioralMetadata()
    {
        foreach (var presetId in new[]
                 {
                     WistLanguageDefinitions.FullDefaultId,
                     WistLanguageDefinitions.FullDefaultNativeId
                 })
        {
            var definition = WistLanguageDefinitions.Create(presetId);

            Assert.Multiple(() =>
            {
                Assert.That(definition.RuntimePolicy.AllowHostInterop, Is.True, presetId);
                Assert.That(definition.SelectedFeatures, Does.Contain(WistFeatureIds.CSharpInterop), presetId);
                Assert.That(definition.Metadata.Keys, Does.Not.Contain("wist.capability.unsafe-interop"), presetId);
            });
        }
    }

    [Test]
    public void PresetLookup_CanonicalizesDefinitionIdentityAndMetadata()
    {
        var definition = WistLanguageDefinitions.Create("FULL-DEFAULT");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Id.Value, Is.EqualTo("wist.full-default"));
            Assert.That(definition.Metadata["wist.preset"], Is.EqualTo("full-default"));
        });
    }
    [Test]
    public void ShippedPresets_ExecuteRepresentativeParityCases()
    {
        var cases = new (string PresetId, string Source, IReadOnlyDictionary<string, object?> Arguments)[]
        {
            (WistLanguageDefinitions.FullDefaultId, "2 + 3 * 4", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.FullDefaultNativeId, "2 + 3 * 4", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.FunctionCallsSafeMathId, "min(10.0, 3.0)", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.MinimalArithmeticId, "2 + 3 * 4", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.MinimalArithmeticGroupedId, "2 + 3 * 4", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.MinimalArithmeticNativeId, "2 + 3 * 4", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.PricingRestrictedId, "price * 0.9 + fee",
                new Dictionary<string, object?> { ["price"] = 100.0, ["fee"] = 5.0 }),
            (WistLanguageDefinitions.SsaId, "1", new Dictionary<string, object?>()),
            (WistLanguageDefinitions.CompositionRestrictedId, "if 2 == 2 (1) else (2)",
                new Dictionary<string, object?>())
        };
        var packageRegistry = new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage());
        var providerRegistry = new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider());

        foreach (var testCase in cases)
        {
            var plan = new LanguageCompiler(packageRegistry)
                .Compile(WistLanguageDefinitions.Create(testCase.PresetId))
                .GetRequiredPlan();
            using var typedRuntime = LanguageRuntime.Create(plan, providerRegistry);
            using var shippedRuntime = WistRuntimeFacadeBuilder.CreateDefault()
                .WithShippedDialectPreset(WistShippedDialectPresets.GetRequired(testCase.PresetId))
                .Build();

            foreach (var backend in plan.Definition.Backends)
            {
                var typed = typedRuntime.Run(new LanguageExecutionRequest(
                    testCase.Source,
                    backend,
                    testCase.Arguments)).Value;
                var shipped = shippedRuntime.Run(new WistRunRequest(
                    testCase.Source,
                    testCase.Arguments,
                    backend.Value));

                Assert.Multiple(() =>
                {
                    Assert.That(typed?.GetType(), Is.EqualTo(shipped?.GetType()),
                        $"{testCase.PresetId}/{backend.Value}: runtime value type differs.");
                    Assert.That(typed, Is.EqualTo(shipped),
                        $"{testCase.PresetId}/{backend.Value}: runtime value differs.");
                });
            }
        }
    }

}
