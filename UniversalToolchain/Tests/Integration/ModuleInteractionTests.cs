using NumbersModule.Core;

namespace Tests.Integration;

[TestFixture]
public class ModuleInteractionTests
{
    private const string DialectText = """
                                       dialect ModuleInteraction
                                       use Arithmetic,Numbers,CSharpInterop
                                       backend compiler,interpreter
                                       """;

    [Test]
    public void Execute_MultipleModuleIntegration_WorksSeamlessly()
    {
        var code = "Main.Round((10 * 2) * 3.141592653589793)";

        var result = Tests.Infrastructure.DialectTestHostInfrastructure.RunInBothBackends(DialectText, code);

        var numberResult = (RealNumberImpl)result!;
        Assert.That(numberResult.GetValue(), Is.EqualTo(63).Within(1e-9));
    }

    [Test]
    public void Execute_ComplexExpressionWithAllOperators_OrderOfOperationsCorrect()
    {
        var code = "Main.Pow(2.0, 3.0) + (Main.Sqrt(4.0) * 2.0) - Main.Log(3.0, 2.0) / Main.Abs(2.0 - 3.0)";

        var result = Tests.Infrastructure.DialectTestHostInfrastructure.RunInBothBackends(DialectText, code);

        var numberResult = (RealNumberImpl)result!;
        Assert.That(numberResult.GetValue(), Is.EqualTo(10.4150375).Within(1e-7));
    }
}
