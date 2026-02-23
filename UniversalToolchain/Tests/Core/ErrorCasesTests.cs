namespace Tests;

[TestFixture]
public class ErrorCasesTests : TestBase
{
    [Test]
    public void Execute_InvalidSyntax_ThrowsException()
    {
        var code = "let 123 = 456";

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Not.Empty);
    }

    [Test]
    public void Execute_ForLoopWithoutBodyScope_ThrowsException()
    {
        var code = @"
                let sum = 0

                for (let i = 1) (i <= 3) (i = i + 1)

                sum
            ";

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Not.Empty);
    }

    [Test]
    public void Execute_WhileLoopWithoutConditionOrBody_ThrowsException()
    {
        var code = @"
                let i = 0
                while
                i
            ";

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Not.Empty);
    }

    [Test]
    public void Execute_LoopWithSwappedBracketsAroundSections_ThrowsException()
    {
        var code = @"
                let sum = 0

                for ((let i = 1) (i <= 3)) (i = i + 1) (
                    sum = sum + i
                )

                sum
            ";

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Not.Empty);
    }

    [Test]
    public void Execute_ForLoopWithWrongSectionOrder_ThrowsException()
    {
        var code = @"
                let sum = 0

                for (i <= 3) (let i = 1) (i = i + 1) (
                    sum = sum + i
                )

                sum
            ";

        Assert.That(() => ExecuteCode(code), Throws.Exception.With.Message.Not.Empty);
    }
}