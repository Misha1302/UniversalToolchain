using Tests.Infrastructure;
using UniversalToolchain.Intrinsics.Legacy;

namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterBackendOptimizerIntrinsicSurfaceTests
{
    private static readonly HashSet<string> SupportedInterpreterIntrinsics =
    [
        "call C#",
        "call C# ctor",
        "load_external",
        "store_external"
    ];

    [Test]
    public void InterpreterBackend_WithOptimizersEnabled_DoesNotContainBackendSpecificIntrinsics()
    {
        var dialect = """
                      dialect Tiny
                      use NativeTypes, BooleanConditions, ComparisonConditions, Conditions, Numbers, Scopes, Variables, Whitespaces
                      backend interpreter
                      enable ArithmeticOptimization
                      enable BooleanOptimization
                      enable ComparisonIntrinsicOptimization
                      enable NativeCilOptimization
                      enable LocalVariablesOptimization
                      enable EGraphOptimization
                      """;

        using var host = DialectTestHostInfrastructure.CreateInterpreterHost(dialect);
        var compiler = host.GetArtifactCompiler<IAbstractIR>("interpreter");
        var artifact = compiler.Compile("(1 + 2) > 0 && true");

        var intrinsicNames = CollectIntrinsicNames(artifact.CompilationOutput).ToArray();

        Assert.That(intrinsicNames, Is.Not.Empty);
        Assert.That(intrinsicNames.All(SupportedInterpreterIntrinsics.Contains), Is.True,
            $"Interpreter IR contains unsupported intrinsic names: {string.Join(", ", intrinsicNames.Where(x => !SupportedInterpreterIntrinsics.Contains(x)).Distinct(StringComparer.Ordinal))}");
    }

    private static IEnumerable<string> CollectIntrinsicNames(IAbstractIR air)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;

            if (!IntrinsicInstructionLegacyProjector.TryProject(instruction, out var projectedInstruction))
                Assert.Fail($"Failed to project intrinsic instruction to legacy form: {instruction}");

            yield return (string)projectedInstruction.Operands[0];
        }
    }
}
