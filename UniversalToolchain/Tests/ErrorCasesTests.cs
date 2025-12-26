namespace Tests;

[TestFixture]
public class ErrorCasesTests : TestBase
{
    [Test]
    public void Execute_InvalidSyntax_ThrowsException()
    {
        // Arrange
        var code = "let 123 = 456";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        try
        {
            ExecuteCode(code, modules);
            Assert.Fail();
        }
        catch
        {
            Assert.Pass();
        }
    }
}