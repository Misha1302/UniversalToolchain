using AbstractIrConverters;
using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterBackendOptimizerIntrinsicSurfaceTests
{
    [Test]
    public void InterpreterBackendStub_SupportedIntrinsics_ShouldContainOnlyUniversalCallIntrinsics()
    {
        var supported = AbstractIrToAbstractIrStub.SupportedIntrinsicIds;

        Assert.That(supported, Does.Contain("call C#"));
        Assert.That(supported, Does.Contain("call C# ctor"));
        Assert.That(supported, Does.Not.Contain("load_external"));
        Assert.That(supported, Does.Not.Contain("store_external"));
    }

    [Test]
    public void InterpreterBackend_WithOptimizersEnabled_ContainsOnlyInterpreterSupportedIntrinsics()
    {
        var dialect = """
                      dialect Tiny
                      use NativeTypes, BooleanConditions, ComparisonConditions, Conditions, Identifier, Numbers, Scopes, Variables, Whitespaces
                      backend interpreter
                      enable ArithmeticOptimization
                      enable BooleanOptimization
                      enable ComparisonIntrinsicOptimization
                      enable NativeCilOptimization

                      enable EGraphOptimization
                      """;

        using var host = DialectTestHostInfrastructure.CreateInterpreterHost(dialect);
        var compiler = host.GetBackendSpecificArtifactCompiler<IAbstractIR>("interpreter");
        var artifact = compiler.Compile("(1 + 2) > 0 and true");

        var intrinsicNames = CollectIntrinsicNames(artifact.CompilationOutput).ToArray();

        Assert.That(intrinsicNames, Is.Not.Empty);
        Assert.That(intrinsicNames.All(IsSupportedInterpreterIntrinsic), Is.True,
            $"Interpreter IR contains unsupported intrinsic names: {string.Join(", ", intrinsicNames.Where(x => !IsSupportedInterpreterIntrinsic(x)).Distinct(StringComparer.Ordinal))}");
    }

    private static bool IsSupportedInterpreterIntrinsic(string intrinsicName) => intrinsicName == "call C#" || intrinsicName == "call C# ctor";

    private static IEnumerable<string> CollectIntrinsicNames(IAbstractIR air)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;

            if (!IntrinsicInstructionView.TryRead(instruction, out var intrinsic))
                Assert.Fail($"Failed to read typed intrinsic instruction: {instruction}");

            yield return intrinsic.CapabilityId;
        }
    }
}
