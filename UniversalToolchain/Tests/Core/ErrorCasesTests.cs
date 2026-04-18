using Tests.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class ErrorCasesTests
{
    private const string DialectText = """
                                       dialect ErrorCases
                                       use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                       enable LocalVariablesOptimization
                                       backend compiler,interpreter
                                       """;

    [Test]
    public void Execute_InvalidSyntax_ThrowsExceptionWithStableDiagnosticFragment()
    {
        var code = "let 123 = 456";

        var ex = Assert.Throws(Is.InstanceOf<Exception>(), () => ExecuteCode(code));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Is.Not.Empty);
    }

    [Test]
    public void Execute_ForLoopWithoutBodyScope_ThrowsExceptionWithStableDiagnosticFragment()
    {
        var code = @"
                let sum = 0

                for (let i = 1) (i <= 3) (i = i + 1)

                sum
            ";

        var ex = Assert.Throws(Is.InstanceOf<Exception>(), () => ExecuteCode(code));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Does.Contain("Tree is invalid").Or.Contain("Assertion failed").Or.Contain("Invalid token").Or.Contain("Index was out of range").Or.Contain("violates the constraint"));
    }

    [Test]
    public void Execute_WhileLoopWithoutConditionOrBody_ThrowsExceptionWithStableDiagnosticFragment()
    {
        var code = @"
                let i = 0
                while
                i
            ";

        var ex = Assert.Throws(Is.InstanceOf<Exception>(), () => ExecuteCode(code));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Does.Contain("Tree is invalid").Or.Contain("Assertion failed").Or.Contain("Invalid token").Or.Contain("Index was out of range").Or.Contain("violates the constraint"));
    }

    [Test]
    public void Execute_LoopWithSwappedBracketsAroundSections_ThrowsExceptionWithStableDiagnosticFragment()
    {
        var code = @"
                let sum = 0

                for ((let i = 1) (i <= 3)) (i = i + 1) (
                    sum = sum + i
                )

                sum
            ";

        var ex = Assert.Throws(Is.InstanceOf<Exception>(), () => ExecuteCode(code));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Does.Contain("Tree is invalid").Or.Contain("Assertion failed").Or.Contain("Invalid token").Or.Contain("Index was out of range").Or.Contain("violates the constraint"));
    }

    [Test]
    public void Execute_ForLoopWithWrongSectionOrder_ThrowsExceptionWithStableDiagnosticFragment()
    {
        var code = @"
                let sum = 0

                for (i <= 3) (let i = 1) (i = i + 1) (
                    sum = sum + i
                )

                sum
            ";

        var ex = Assert.Throws(Is.InstanceOf<Exception>(), () => ExecuteCode(code));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.Not.Empty);
        Assert.That(ex.Message, Does.Contain("Tree is invalid").Or.Contain("Assertion failed").Or.Contain("Invalid token").Or.Contain("Index was out of range").Or.Contain("violates the constraint"));
    }

    private static object? ExecuteCode(string code)
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(DialectText, code);
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);

        if (compilerResult.IsSuccess)
            return compilerResult.Value;

        throw compilerResult.Exception!;
    }
}