using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class ModuleCombinationsTests
{
    private const string DialectText = """
                                       dialect ModuleCombinations
                                       use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                       backend compiler,interpreter
                                       """;

    [Test]
    public void Execute_AllCoreModulesTogether_WorksCorrectly()
    {
        var code = "((10 + 5) * 2 - 3) / 2";

        var result = DialectTestHostInfrastructure.RunInBothBackends(DialectText, code);

        Assert.That(BackendResultAssertions.AsNumber(result), Is.EqualTo(13.5).Within(1e-9));
    }

    [Test]
    public void Execute_MixedOperationsWithDifferentPrecedence_RespectsOrder()
    {
        var code = "(2 + 3 * 4) + ((2 + 3) * 4) * 2";

        var result = DialectTestHostInfrastructure.RunInBothBackends(DialectText, code);

        Assert.That(BackendResultAssertions.AsNumber(result), Is.EqualTo(54).Within(1e-9));
    }
}
