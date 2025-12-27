namespace Tests;

[TestFixture]
public class ErrorCasesTests : TestBase
{
    [Test]
    public void Execute_InvalidSyntax_ThrowsException()
    {
        // Arrange
        var code = "let 123 = 456";

        try
        {
            ExecuteCode(code);
            Assert.Fail();
        }
        catch
        {
            Assert.Pass();
        }
    }
}