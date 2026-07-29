using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistBackendContractTests
{
    [Test]
    public void Compile_WhenInterpreterRequested_UsesInterpreterArtifactAndMetadata()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
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
        using var wist = WistEngine.Create(WistEngineOptions.FromPresetId("full-default"));

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

    [TestCase("minimal-arithmetic", "cil")]
    [TestCase("minimal-arithmetic-grouped", "cil")]
    [TestCase("minimal-arithmetic-native", "interpreter")]
    [TestCase("composition-restricted", "cil")]
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
        foreach (var preset in WistShippedDialectPresets.All)
        {
            using var wist = WistEngine.Create(WistEngineOptions.FromPresetId(preset.Id));
            var evaluated = wist.Evaluate<int>("2 + 3");
            var program = wist.Compile<Func<int>>("2 + 3");

            Assert.Multiple(() =>
            {
                Assert.That(evaluated, Is.EqualTo(5), preset.Id);
                Assert.That(program.CompiledDelegate(), Is.EqualTo(5), preset.Id);
                Assert.That(program.Metadata.Backend, Is.EqualTo(preset.DefaultBackend), preset.Id);
            });
        }
    }

    [Test]
    public void LowLevelWistExecutionTypes_AreNotExportedPolicyBypasses()
    {
        var wistRuntimeAssembly = typeof(WistShippedDialectPresets).Assembly;
        var integrationAssembly = typeof(IRuntimeAssemblyLoadStrategy).Assembly;
        string[] wistRuntimeTypes =
        [
            "UniversalToolchain.Dialects.Wist.WistDialectExecutionConfiguration",
            "UniversalToolchain.Dialects.Wist.WistDialectExecutionHost",
            "UniversalToolchain.Dialects.Wist.WistDialectExecutionWorkflow",
            "UniversalToolchain.Dialects.Wist.Facade.WistRuntimeFacade",
            "UniversalToolchain.Dialects.Wist.Facade.WistRuntimeFacadeBuilder",
            "UniversalToolchain.Dialects.Wist.Facade.WistRunRequest",
            "UniversalToolchain.Dialects.Wist.Facade.WistTryCompileResult",
            "UniversalToolchain.Dialects.Wist.WistCilBackendServiceCollectionExtensions",
            "UniversalToolchain.Dialects.Wist.WistInterpreterBackendServiceCollectionExtensions"
        ];

        Assert.Multiple(() =>
        {
            foreach (var typeName in wistRuntimeTypes)
            {
                var type = wistRuntimeAssembly.GetType(typeName, throwOnError: false);
                if (type is not null)
                {
                    Assert.That(type.IsPublic || type.IsNestedPublic, Is.False, typeName);
                }
            }

            var runtimeHost = integrationAssembly.GetType(
                "UniversalToolchain.Dialects.Integration.ToolchainRuntimeHost",
                throwOnError: true)!;
            Assert.That(runtimeHost.IsPublic || runtimeHost.IsNestedPublic, Is.False, runtimeHost.FullName);
        });
    }
}
