using AssemblyFinder;
using System.Diagnostics.CodeAnalysis;
using Tests.Infrastructure;
using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Core;

[TestFixture]
public class CSharpInteropResolutionAndNegativeContractsTests
{
    private const string DialectText = """
                                       dialect NativeInterop
                                       use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop

                                       backend compiler,interpreter
                                       """;

    [Test]
    public void ExecuteCode_ShouldResolveSameUnambiguousCallShape_AcrossRepeatedRuns()
    {
        const string code = "System.Math.Sqrt(16.0)";
        var first = BackendParityInfrastructure.ExecuteSafely(() => ExecuteCode<object>(code));

        for (var i = 0; i < 20; i++)
        {
            var current = BackendParityInfrastructure.ExecuteSafely(() => ExecuteCode<object>(code));
            Assert.That(current.IsSuccess, Is.EqualTo(first.IsSuccess));

            if (first.IsSuccess)
            {
                Assert.That(current.Value, Is.EqualTo(first.Value));
                continue;
            }

            Assert.That(current.Exception!.GetType(), Is.EqualTo(first.Exception!.GetType()));
            Assert.That(current.Exception!.Message, Is.EqualTo(first.Exception!.Message));
        }
    }

    [Test]
    public void MethodsFinder_ShouldFailPredictably_ForAmbiguousOverloadSignature()
    {
        var exception = Assert.Throws<AmbiguousMatchException>(() =>
            MethodsFinder.GetMethod($"{typeof(InteropContractsHost).FullName}.Pick", [typeof(int), typeof(int)]));

        Assert.That(exception!.Message, Does.Contain("Ambiguous match"));
    }

    [Test]
    public void ExecuteCode_ShouldRejectNonPublicInteropTarget()
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

        Assert.That(exception!.Message, Does.Contain("Cannot cast").Or.Contain("Storage type for variable 'null' is not fixed before read"));
    }

    private static T ExecuteCode<T>(string code)
    {
        var compilerResult = BackendParityInfrastructure.ExecuteSafely(() =>
        {
            using var compilerHost = DialectTestHostInfrastructure.CreateCompilerHost(DialectText);
            return compilerHost.Run(code, "compiler");
        });

        if (!compilerResult.IsSuccess)
            throw compilerResult.Exception!;

        return BackendValueNormalizer.ConvertTo<T>(compilerResult.Value);
    }
}

internal static class InteropContractsHost
{
    public static string Pick(int left, long right) => "int-long";
    public static string Pick(long left, int right) => "long-int";

    [SuppressMessage("Performance", "CA1822", Justification = "The method is used by name in a negative C# interop resolution contract test.")]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "The method is intentionally non-public and resolved by name in a negative interop test.")]
    private static string Hidden() => "hidden";
}