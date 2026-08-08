using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistBackendContractTests
{
    [Test]
    public void Compile_WhenInterpreterRequested_UsesInterpreterMetadata()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(WistLanguageDefinitions.PricingRestrictedId),
            BackendId = "interpreter"
        });
        var program = wist.Compile<Func<int>>("2 + 3");

        Assert.Multiple(() =>
        {
            Assert.That(program.CompiledDelegate(), Is.EqualTo(5));
            Assert.That(program.Metadata.Backend, Is.EqualTo("interpreter"));
            Assert.That(program.Metadata.ReturnType, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void FullDefault_EvaluateAndCompile_HaveExactValueAndTypeParity()
    {
        using var wist = WistEngine.Create(WistEngineOptions.FromPresetId(WistLanguageDefinitions.FullDefaultId));
        var evaluated = wist.Evaluate<int>("2 + 3");
        var program = wist.Compile<Func<int>>("2 + 3");
        var compiled = program.CompiledDelegate();

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.TypeOf<int>());
            Assert.That(compiled, Is.TypeOf<int>());
            Assert.That(evaluated, Is.EqualTo(5));
            Assert.That(compiled, Is.EqualTo(evaluated));
            Assert.That(program.Metadata.Backend, Is.EqualTo("cil"));
        });
    }

    [TestCase(WistLanguageDefinitions.MinimalArithmeticId, "cil")]
    [TestCase(WistLanguageDefinitions.MinimalArithmeticGroupedId, "cil")]
    [TestCase(WistLanguageDefinitions.MinimalArithmeticNativeId, "interpreter")]
    [TestCase(WistLanguageDefinitions.CompositionRestrictedId, "cil")]
    public void Create_WhenPresetDoesNotSupportBackend_FailsBeforeFirstOperation(
        string presetId,
        string backendId)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WistEngine.Create(new WistEngineOptions
            {
                DialectSource = WistDialectSource.FromShippedPreset(presetId),
                BackendId = backendId
            }));
        Assert.That(exception!.Message, Does.Contain($"does not enable backend '{backendId}'"));
    }

    [Test]
    public void EveryShippedPreset_DefaultBackend_CreatesAndExecutes()
    {
        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var options = WistEngineOptions.FromPresetId(presetId);
            using var wist = WistEngine.Create(options);
            var evaluated = wist.Evaluate<int>("2 + 3");
            var program = wist.Compile<Func<int>>("2 + 3");

            Assert.Multiple(() =>
            {
                Assert.That(evaluated, Is.EqualTo(5), presetId);
                Assert.That(program.CompiledDelegate(), Is.EqualTo(5), presetId);
                Assert.That(program.Metadata.Backend, Is.EqualTo(options.BackendId), presetId);
            });
        }
    }

    [Test]
    public void CanonicalFacadeAssemblies_DoNotReferenceLegacyWistRuntimeProject()
    {
        static bool ReferencesLegacyWistRuntime(System.Reflection.Assembly assembly) =>
            assembly.GetReferencedAssemblies().Any(static reference =>
                string.Equals(reference.Name, "UniversalToolchain.Dialects.Wist", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(ReferencesLegacyWistRuntime(typeof(WistEngine).Assembly), Is.False);
            Assert.That(ReferencesLegacyWistRuntime(typeof(WistLanguageFeaturePackage).Assembly), Is.False);
        });
    }
}
