using UniversalToolchain.Testing.Infrastructure;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class TextualAdditionModuleTests
{
    private const string TextualAdditionDialect = """
                                                  dialect TextualAdditionDemo
                                                  use Whitespaces,Numbers,Scopes,Arithmetic,TextualAddition
                                                  backend compiler,interpreter
                                                  """;

    private const string ArithmeticOnlyDialect = """
                                                 dialect ArithmeticOnlyDemo
                                                 use Whitespaces,Numbers,Scopes,Arithmetic
                                                 backend compiler,interpreter
                                                 """;

    [Test]
    public void TextualAddition_Module_ExecutesPlusKeyword()
    {
        var result = DialectTestHostInfrastructure.RunInBothBackends(TextualAdditionDialect, "2 plus 3");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(5.0d).Within(1e-9));
    }

    [Test]
    public void TextualAddition_Module_UsesAdditionPrecedence()
    {
        var result = DialectTestHostInfrastructure.RunInBothBackends(TextualAdditionDialect, "2 plus 3 * 4");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(14.0d).Within(1e-9));
    }

    [Test]
    public void TextualAddition_Syntax_IsUnavailable_WhenModuleIsNotSelected()
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(ArithmeticOnlyDialect, "2 plus 3");

        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.False, "Compiler path must reject syntax owned by an unselected module.");
            Assert.That(interpreterResult.IsSuccess, Is.False, "Interpreter path must reject syntax owned by an unselected module.");
            Assert.That(compilerResult.Exception, Is.Not.Null);
            Assert.That(interpreterResult.Exception, Is.Not.Null);
        });
    }
}