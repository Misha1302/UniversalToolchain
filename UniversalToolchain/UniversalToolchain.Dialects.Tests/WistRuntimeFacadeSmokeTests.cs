using Tests.Infrastructure;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class WistRuntimeFacadeSmokeTests
{
    private const string PricingFormula = "price * 0.9 + fee";
    private const string InteropCall = "NumbersModule.Core.RealNumberImpl.Add(2, 5)";

    private const string StatementStyleBindingFormula = """
                                                        let discount = 0.9
                                                        price * discount + fee
                                                        """;

    [Test]
    public void Facade_Run_Compiler_ReturnsExpectedResult()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var result = wist.Run(PricingFormula, CreateArguments(), mode: "compiler");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void Facade_Run_Interpreter_ReturnsExpectedResult()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var result = wist.Run(PricingFormula, CreateArguments(), mode: "interpreter");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void Facade_CreateDefault_TryCompile_InteropCall_Fails()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateDefault()
            .Build();

        var attempt = wist.TryCompile(
            InteropCall,
            new Dictionary<string, Type>(),
            mode: "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(attempt.IsSuccess, Is.False);
            Assert.That(attempt.Artifact, Is.Null);
            Assert.That(attempt.Exception, Is.Not.Null);
            Assert.That(attempt.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void Facade_CreateTrustedDefault_TryCompile_InteropCall_Succeeds()
    {
        using var wist = WistRuntimeFacadeBuilder
            .CreateTrustedDefault()
            .Build();

        var attempt = wist.TryCompile(
            InteropCall,
            new Dictionary<string, Type>(),
            mode: "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(attempt.IsSuccess, Is.True);
            Assert.That(attempt.Artifact, Is.Not.Null);
            Assert.That(attempt.Exception, Is.Null);
            Assert.That(attempt.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void Facade_TryCompile_RestrictedPricing_ReturnsFailureForStatementStyleBinding()
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
