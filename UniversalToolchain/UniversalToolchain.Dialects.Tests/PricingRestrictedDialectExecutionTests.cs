using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Testing.Infrastructure;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class PricingRestrictedDialectExecutionTests
{
    private const string PricingFormula = "price * 0.9 + fee";

    private const string MixedParameterLiteralFormula = "x * 2 + y";

    private const string StatementStyleBindingFormula = """
                                                        let discount = 0.9
                                                        price * discount + fee
                                                        """;

    [Test]
    public void PricingRestricted_Dialect_Compiler_Executes_PricingFormula()
    {
        using var host = CreatePricingHost();

        var result = ExecuteCompilerFormula(host);

        Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void PricingRestricted_Dialect_Interpreter_Executes_PricingFormula()
    {
        using var host = CreatePricingHost();

        var result = ExecuteInterpreterFormula(host);

        Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void PricingRestricted_Dialect_Rejects_UnsupportedStatementStyleBindingFormula()
    {
        var result = BackendParityInfrastructure.ExecuteSafely(() =>
        {
            using var host = CreatePricingHost();
            _ = host.Compile(StatementStyleBindingFormula, CreateDeclaredBindings(), "interpreter");
            return null;
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False, "Pricing-restricted must reject unsupported statement-style binding formulas.");
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Exception!.Message, Is.Not.Empty);
            Assert.That(
                result.Exception.Message,
                Does.Contain("variable")
                    .Or.Contain("restriction")
                    .Or.Contain("compile")
                    .Or.Contain("token")
                    .IgnoreCase);
        });
    }

    [Test]
    public void PricingRestricted_Dialect_Compiler_And_Interpreter_Agree_On_Result()
    {
        using var host = CreatePricingHost();

        var compilerResult = ExecuteCompilerFormula(host);
        var interpreterResult = ExecuteInterpreterFormula(host);

        Assert.Multiple(() =>
        {
            Assert.That(compilerResult, Is.EqualTo(interpreterResult).Within(1e-9));
            Assert.That(compilerResult, Is.EqualTo(95.0d).Within(1e-9));
        });
    }

    [Test]
    public void PricingRestricted_Dialect_MixedDoubleParametersAndIntegerLiteral_HasBackendParity()
    {
        using var host = CreatePricingHost();
        var bindings = new OrderedDictionary<string, Type>
        {
            ["x"] = typeof(double),
            ["y"] = typeof(double)
        };
        var arguments = new Dictionary<string, object?>
        {
            ["x"] = 5.0d,
            ["y"] = 3.0d
        };

        var compilerArtifact = host.Compile(MixedParameterLiteralFormula, bindings, "cil");
        var interpreterArtifact = host.Compile(MixedParameterLiteralFormula, bindings, "interpreter");
        var compilerResult = BackendParityInfrastructure.AsNumber(host.Run(compilerArtifact, arguments));
        var interpreterResult = BackendParityInfrastructure.AsNumber(host.Run(interpreterArtifact, arguments));

        Assert.Multiple(() =>
        {
            Assert.That(compilerResult, Is.EqualTo(13.0d).Within(1e-9));
            Assert.That(interpreterResult, Is.EqualTo(13.0d).Within(1e-9));
            Assert.That(compilerResult, Is.EqualTo(interpreterResult).Within(1e-9));
        });
    }

    private static double ExecuteCompilerFormula(WistDialectExecutionHost host)
    {
        var artifact = host.Compile(PricingFormula, CreateDeclaredBindings(), "cil");
        return BackendParityInfrastructure.AsNumber(host.Run(artifact, CreatePricingArguments()));
    }

    private static double ExecuteInterpreterFormula(WistDialectExecutionHost host)
    {
        var artifact = host.Compile(PricingFormula, CreateDeclaredBindings(), "interpreter");
        return BackendParityInfrastructure.AsNumber(host.Run(artifact, CreatePricingArguments()));
    }

    private static IReadOnlyDictionary<string, object?> CreatePricingArguments() =>
        new Dictionary<string, object?>
        {
            ["price"] = 100.0d,
            ["fee"] = 5.0d
        };

    private static OrderedDictionary<string, Type> CreateDeclaredBindings() =>
        new()
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

    private static WistDialectExecutionHost CreatePricingHost()
        => DialectTestHostInfrastructure.CreateHostFromDialectText(File.ReadAllText(GetDialectFilePath()));

    private static string GetDialectFilePath() => TestSourcePaths.WistExampleDialectPath("pricing-restricted");
}
