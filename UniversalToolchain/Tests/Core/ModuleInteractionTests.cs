using BasicStdLib;
using Tests.Infrastructure;
using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class ModuleInteractionTests
{
    private const string DialectText = """
                                       dialect ModuleInteraction
                                       use Arithmetic,Numbers,CSharpInterop,Identifier,Scopes,Whitespaces
                                       backend cil,interpreter
                                       security trusted
                                       capability unsafe-interop
                                       """;

    [Test]
    public void Execute_MultipleModuleIntegration_WorksSeamlessly()
    {
        var code = "Main.Round((10 * 2) * 3.141592653589793)";

        var result = RunInBoth(code);

        Assert.That(BackendResultAssertions.AsNumber(result), Is.EqualTo(63).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithAllOperators_OrderOfOperationsCorrect()
    {
        var code = "Main.Pow(2.0, 3.0) + (Main.Sqrt(4.0) * 2.0) - Main.Log(3.0, 2.0) / Main.Abs(2.0 - 3.0)";

        var result = RunInBoth(code);

        Assert.That(BackendResultAssertions.AsNumber(result), Is.EqualTo(10.4150375).Within(1e-7));
    }

    private static object? RunInBoth(string code)
    {
        using var host = new CanonicalWistTestHost(
            DialectText,
            ["cil", "interpreter"],
            [typeof(Main).Assembly]);
        var compilerResult = BackendParityInfrastructure.ExecuteSafely(() => host.Run(code, "cil"));
        var interpreterResult = BackendParityInfrastructure.ExecuteSafely(() => host.Run(code, "interpreter"));
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return compilerResult.IsSuccess ? compilerResult.Value : throw compilerResult.Exception!;
    }
}
