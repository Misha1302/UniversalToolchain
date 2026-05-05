using System.Reflection.Emit;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Testing.Infrastructure;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class PricingRestrictedDialectExecutionTests
{
    private const string PricingFormula = "price * 0.9 + fee";

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
            var interpreter = host.GetArtifactCompiler<IAbstractIR>("interpreter");
            _ = interpreter.Compile(StatementStyleBindingFormula, CreateDeclaredBindings());
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

    private static double ExecuteCompilerFormula(WistDialectExecutionHost host)
    {
        var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");
        var artifact = compiler.Compile(PricingFormula, CreateDeclaredBindings());
        var session = artifact.CreateSession();
        SetPricingArguments(session);

        return BackendParityInfrastructure.AsNumber(session.Run());
    }

    private static double ExecuteInterpreterFormula(WistDialectExecutionHost host)
    {
        var interpreter = host.GetArtifactCompiler<IAbstractIR>("interpreter");
        var artifact = interpreter.Compile(PricingFormula, CreateDeclaredBindings());
        var session = artifact.CreateSession();
        SetPricingArguments(session);

        return BackendParityInfrastructure.AsNumber(session.Run());
    }

    private static void SetPricingArguments(ICompiledArtifactSession session)
    {
        session.SetArgument("price", 100.0d);
        session.SetArgument("fee", 5.0d);
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings() =>
        new()
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

    private static WistDialectExecutionHost CreatePricingHost()
        => DialectTestHostInfrastructure.CreateHostFromDialectText(File.ReadAllText(GetDialectFilePath()));

    private static string GetDialectFilePath()
        => Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Dialects",
            "examples",
            "wist",
            "pricing-restricted",
            "dialect.wistdialect"));
}