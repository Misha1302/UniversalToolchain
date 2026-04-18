using Tests.Infrastructure;
using UniversalToolchain.Intrinsics.Core;

namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterBackendOptimizerIntrinsicSurfaceTests
{
    [Test]
    public void InterpreterBackend_WithOptimizersEnabled_ContainsOnlyInterpreterSupportedIntrinsics()
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
        var artifact = compiler.Compile("(1 + 2) > 0 and true");

        var intrinsicNames = CollectIntrinsicNames(artifact.CompilationOutput).ToArray();

        Assert.That(intrinsicNames, Is.Not.Empty);
        Assert.That(intrinsicNames.All(IsSupportedInterpreterIntrinsic), Is.True,
            $"Interpreter IR contains unsupported intrinsic names: {string.Join(", ", intrinsicNames.Where(x => !IsSupportedInterpreterIntrinsic(x)).Distinct(StringComparer.Ordinal))}");
    }

    private static bool IsSupportedInterpreterIntrinsic(string intrinsicName)
    {
        if (intrinsicName == "call C#"
            || intrinsicName == "call C# ctor"
            || intrinsicName == "load_external"
            || intrinsicName == "store_external"
            || intrinsicName == "store_local"
            || intrinsicName == "load_local"
            || intrinsicName == "load_local_ref"
            || intrinsicName == "load_bool"
            || intrinsicName == "boolean_and"
            || intrinsicName == "boolean_or"
            || intrinsicName == "boolean_not")
            return true;

        return intrinsicName.StartsWith("load_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("add_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("sub_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("mul_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("div_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("cmp_", StringComparison.Ordinal);
    }

    private static IEnumerable<string> CollectIntrinsicNames(IAbstractIR air)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;

            if (!IntrinsicInstructionNormalizer.TryNormalize(instruction, out var normalizedInstruction))
                Assert.Fail($"Failed to normalize intrinsic instruction: {instruction}");

            yield return (string)normalizedInstruction.Operands[0];
        }
    }
}