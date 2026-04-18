using UniversalToolchain.Modules.Tests;

namespace Tests.Core;

[TestFixture]
public class ExecutionRepeatabilitySmokeTests
{
    private const string DialectText = """
                                       dialect TestUniversal
                                       use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
                                       enable LocalVariablesOptimization
                                       backend compiler,interpreter
                                       """;

    [Test]
    public void Should_BeStableAcrossRepeatedRuns_When_ExecutingSameProgramMultipleTimes()
    {
        const string code = @"
            let x = 40
            let y = 2
            x + y
        ";

        var first = RunInBoth(code);
        var second = RunInBoth(code);

        Assert.That(second, Is.EqualTo(first));
    }

    private static object? RunInBoth(string code)
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(DialectText, code);
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return compilerResult.IsSuccess ? compilerResult.Value : throw compilerResult.Exception!;
    }
}