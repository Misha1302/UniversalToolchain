using System.Reflection;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistEngineCanonicalRuntimeCutoverTests
{
    [Test]
    public void WistEngine_OwnsExactlyOneCanonicalPlanAndRuntime()
    {
        var fields = typeof(WistEngine)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(fields.Count(static field => field.FieldType == typeof(LanguageRuntime)), Is.EqualTo(1));
            Assert.That(fields.Count(static field => field.FieldType == typeof(LanguagePlan)), Is.EqualTo(1));
            Assert.That(
                fields.Select(static field => field.FieldType.FullName),
                Has.None.Contains("WistDialectExecutionHost"));
            Assert.That(
                fields.Select(static field => field.FieldType.FullName),
                Has.None.Contains("WistDialectExecutionWorkflow"));
        });
    }

    [Test]
    public void CompiledInterpreterDelegate_RemainsUsableAfterOriginatingEngineIsDisposed()
    {
        var engine = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            BackendId = "interpreter"
        });
        var program = engine.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");

        engine.Dispose();

        Assert.That(program.CompiledDelegate(100.0d, 5.0d), Is.EqualTo(95.0d).Within(1e-9));
    }
}
