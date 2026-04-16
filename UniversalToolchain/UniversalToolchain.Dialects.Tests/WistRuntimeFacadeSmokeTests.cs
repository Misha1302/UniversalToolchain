using Tests.Infrastructure;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class WistRuntimeFacadeSmokeTests
{
    private const string PricingFormula = "price * 0.9 + fee";
    private const string InteropFormula = "NumbersModule.Core.RealNumberImpl.Add(2, 5)";

    private const string StatementStyleBindingFormula = """
                                                        let discount = 0.9
                                                        price * discount + fee
                                                        """;

    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void CreateDefault_RunPricingFormula_ReturnsExpectedResult(string mode)
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var result = wist.Run(PricingFormula, CreateArguments(), mode);

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void CreateDefault_RunInteropFormula_ReturnsFailure(string mode)
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var result = BackendParityInfrastructure.ExecuteSafely(() => wist.Run(InteropFormula, new Dictionary<string, object?>(), mode));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Exception!.Message, Does.Contain("interop").Or.Contain("identifier").Or.Contain("token").IgnoreCase);
        });
    }

    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void CreateTrustedDefault_RunPricingFormula_ReturnsExpectedResult(string mode)
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateTrustedDefault()
            .Build();

        var result = wist.Run(PricingFormula, CreateArguments(), mode);

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void CreateTrustedDefault_RunInteropFormula_ReturnsExpectedResult(string mode)
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateTrustedDefault()
            .Build();

        var result = wist.Run(InteropFormula, new Dictionary<string, object?>(), mode);

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(7.0d).Within(1e-9));
    }

    [TestCase("compiler")]
    [TestCase("interpreter")]
    public void CreateDefault_AndPricingRestrictedFile_RunPricingFormula_ReturnMatchingResult(string mode)
    {
        using var builtInSafe = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();
        using var fileBasedRestricted = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithDialectFile(GetDialectFilePath())
            .Build();

        var builtInResult = builtInSafe.Run(PricingFormula, CreateArguments(), mode);
        var fileBasedResult = fileBasedRestricted.Run(PricingFormula, CreateArguments(), mode);

        Assert.That(
            BackendParityInfrastructure.AsNumber(builtInResult),
            Is.EqualTo(BackendParityInfrastructure.AsNumber(fileBasedResult)).Within(1e-9));
    }

    [Test]
    public void WithDialectFile_PricingRestricted_ReturnsFailureForStatementStyleBinding()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithDialectFile(GetDialectFilePath())
            .Build();

        var attempt = wist.TryCompile(StatementStyleBindingFormula, CreateDeclaredBindings(), mode: "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(attempt.IsSuccess, Is.False);
            Assert.That(attempt.Artifact, Is.Null);
            Assert.That(attempt.Exception, Is.Not.Null);
            Assert.That(attempt.ErrorMessage, Is.Not.Empty);
        });
    }

    private static Dictionary<string, object?> CreateArguments()
    {
        return new Dictionary<string, object?>
        {
            ["price"] = 100.0d,
            ["fee"] = 5.0d
        };
    }

    private static Dictionary<string, Type> CreateDeclaredBindings()
    {
        return new Dictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };
    }

    private static string GetDialectFilePath()
        => Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Dialects",
            "examples",
            "wist",
            "pricing-restricted",
            "dialect.wistdialect"));
}
