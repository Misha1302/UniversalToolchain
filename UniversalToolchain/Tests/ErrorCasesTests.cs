// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using ArithmeticModule;
using BasicCore;
using EqualityModule;
using IdentifierModule;
using NumbersModule;
using ScopesModule;
using VariablesModule;
using WhitespacesModule;

namespace Tests;

[TestFixture]
public class ErrorCasesTests : TestBase
{
    [Test]
    public void Execute_UndefinedVariable_ThrowsException()
    {
        // Arrange
        var code = "x + 5";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ExecuteCode(code, modules));
    }

    [Test]
    public void Execute_InvalidSyntax_ThrowsException()
    {
        // Arrange
        var code = "let 123 = 456";
        var modules = new ICoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl()
        };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ExecuteCode(code, modules));
    }
}