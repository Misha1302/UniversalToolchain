using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Dialects.Tests.Wist;

[TestFixture]
public sealed class ShippedDefaultConsumerGuardrailTests
{
    [Test]
    public void DslPricingCalculator_DefaultPreset_IsProvidedByWistShippedDialectPresets()
    {
        var source = ReadRepositoryFile(
            "UniversalToolchain",
            "Example",
            "Scenarios",
            "DslPricingCalculator.cs");

        Assert.Multiple(() =>
        {
            Assert.That(WistShippedDialectPresets.FullDefaultNative.Id, Is.EqualTo("full-default-native"));
            Assert.That(source, Does.Contain("this(WistShippedDialectPresets.FullDefaultNative)"));
            Assert.That(source, Does.Not.Contain("\"full-default-native\""));
        });
    }

    [Test]
    public void BenchmarkEnvironment_UsesPublicWistFacadeInsteadOfHardcodedDialectPaths()
    {
        var source = ReadRepositoryFile(
            "UniversalToolchain",
            "Benchmarks",
            "UniversalToolchain.Benchmarks",
            "FormulaHotPathBenchmarks.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("WistEngine.CreateSafeFormulas()"));
            Assert.That(source, Does.Not.Contain("Path.Combine(AppContext.BaseDirectory"));
            Assert.That(source, Does.Not.Contain("\"full-default-native\""));
        });
    }

    [Test]
    public void DefaultConsumers_ResolveThroughCanonicalWistEntryPoints()
    {
        var calculatorSource = ReadRepositoryFile(
            "UniversalToolchain",
            "Example",
            "Scenarios",
            "DslPricingCalculator.cs");
        var benchmarkSource = ReadRepositoryFile(
            "UniversalToolchain",
            "Benchmarks",
            "UniversalToolchain.Benchmarks",
            "FormulaHotPathBenchmarks.cs");

        Assert.Multiple(() =>
        {
            Assert.That(calculatorSource, Does.Contain("new WistShippedDialectFileResolver().Resolve(dialectPreset)"));
            Assert.That(benchmarkSource, Does.Contain("WistEngine.CreateSafeFormulas()"));
            Assert.That(calculatorSource, Does.Not.Contain("Path.Combine("));
            Assert.That(benchmarkSource, Does.Not.Contain("Path.Combine(AppContext.BaseDirectory"));
        });
    }

    [Test]
    public void ShippedDefaultConsumers_DoNotDefineIndependentDefaultProfileIds()
    {
        var sources = string.Join(
            Environment.NewLine,
            ReadRepositoryFile("UniversalToolchain", "Example", "Scenarios", "DslPricingCalculator.cs"),
            ReadRepositoryFile("UniversalToolchain", "Example", "Scenarios", "PricingDiscountScenario.cs"),
            ReadRepositoryFile(
                "UniversalToolchain",
                "Benchmarks",
                "UniversalToolchain.Benchmarks",
                "FormulaHotPathBenchmarks.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(sources, Does.Not.Contain("\"full-default-native\""));
            Assert.That(sources, Does.Not.Contain("\"pricing-restricted\""));
            Assert.That(sources, Does.Contain("WistShippedDialectPresets.PricingRestricted"));
        });
    }

    private static string ReadRepositoryFile(params string[] parts)
        => File.ReadAllText(Path.Combine([GetRepositoryRoot(), .. parts]));

    private static string GetRepositoryRoot() => UniversalToolchain.Dialects.Tests.TestSourcePaths.RepositoryRoot;
}
