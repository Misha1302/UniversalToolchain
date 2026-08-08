using System.Diagnostics.CodeAnalysis;
using ExceptionsManager;
using UniversalToolchain.Wist;

namespace Tests.Core;

[TestFixture]
public sealed class CSharpInteropResolutionAndNegativeContractsTests
{
    private const string DialectText = """
        dialect InteropContracts
        use BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,NativeTypes,Scopes,SemicolonAsNewLine,Variables,Whitespaces
        backend cil,interpreter
        security trusted
        capability unsafe-interop
        """;

    [Test]
    public void ExecuteCode_ShouldResolveExactOverload()
    {
        Assert.That(ExecuteCode<string>($"{typeof(InteropContractsHost).FullName}.Pick(1, 2)"), Is.EqualTo("int-long"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectAmbiguousOverload()
    {
        Assert.Catch(() => ExecuteCode<string>($"{typeof(InteropContractsHost).FullName}.Ambiguous(1, 2)"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectNonPublicMethod()
    {
        var exception = Assert.Throws<ImportException>(() => ExecuteCode<int>($"{typeof(InteropContractsHost).FullName}.Hidden()"));
        Assert.That(exception!.Message, Does.Contain("not found").IgnoreCase);
    }

    [Test]
    public void ExecuteCode_ShouldRejectUnsupportedRefOutCallShape()
    {
        Assert.Catch(() => ExecuteCode<int>("System.Int32.TryParse(7)"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectNullCallShape_WhenCastContractCannotBeSatisfied()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ExecuteCode<int>("System.String.IsNullOrEmpty(null)"));
        Assert.That(exception!.Message, Does.Contain("Unknown identifier 'null'"));
    }

    private static T ExecuteCode<T>(string code)
    {
        var options = WistEngineOptions.FromDialectText(DialectText, "interop-contracts");
        options.BackendId = "cil";
        options.AllowedAssemblies = [typeof(string).Assembly, typeof(InteropContractsHost).Assembly];
        using var engine = WistEngine.Create(options);
        return engine.Evaluate<T>(code);
    }
}

internal static class InteropContractsHost
{
    public static string Pick(int left, long right) => "int-long";
    public static string Pick(long left, int right) => "long-int";
    public static string Ambiguous(IComparable left, object right) => "comparable-object";
    public static string Ambiguous(object left, IComparable right) => "object-comparable";

    [SuppressMessage("Performance", "CA1822", Justification = "The method is used by name in a negative C# interop resolution contract test.")]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "The method is intentionally non-public and resolved by name in a negative interop test.")]
    private static string Hidden() => "hidden";
}
