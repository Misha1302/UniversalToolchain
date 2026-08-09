using System.Globalization;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class FunctionCallsSourceExecutionTests
{
    [TestCase("min(10.0, 3.0)", "3")]
    [TestCase("max(10.0, 3.0)", "10")]
    [TestCase("abs(0.0 - 5.0)", "5")]
    [TestCase("clamp(120.0, 0.0, 100.0)", "100")]
    [TestCase("let x = 120.0\nclamp(x, 0.0, 100.0)", "100")]
    [TestCase("let base = 300.0\nlet discount = 0.15\nlet maxDiscount = 50.0\nclamp(base * discount, 0.0, maxDiscount)", "45")]
    [TestCase("round(2.6)", "3")]
    public void SafeMathFunctionCalls_FromWistSource_ShouldHaveInterpreterAndCompilerParity(
        string source,
        string expectedValueText)
    {
        var interpreter = Evaluate(source, "interpreter");
        var compiler = Evaluate(source, "cil");

        Assert.Multiple(() =>
        {
            Assert.That(interpreter, Is.EqualTo(expectedValueText));
            Assert.That(compiler, Is.EqualTo(expectedValueText));
            Assert.That(compiler, Is.EqualTo(interpreter));
        });
    }

    [Test]
    public void FinalPriceExpression_FromWistSource_ShouldHaveInterpreterAndCompilerParity()
    {
        const string source = """
            let base = 100.0 * 3.0
            let discountValue = clamp(base * 0.15, 0.0, 50.0)
            let result = base - discountValue
            if result < 0.0 then 0.0 else result
            """;

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(source, "interpreter"), Is.EqualTo("255"));
            Assert.That(Evaluate(source, "cil"), Is.EqualTo("255"));
        });
    }

    private static string Evaluate(string source, string backend)
    {
        var options = WistEngineOptions.FromPresetId(WistLanguageDefinitions.FunctionCallsSafeMathId);
        options.BackendId = backend;
        using var engine = WistEngine.Create(options);
        var value = engine.Evaluate<double>(source);
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
