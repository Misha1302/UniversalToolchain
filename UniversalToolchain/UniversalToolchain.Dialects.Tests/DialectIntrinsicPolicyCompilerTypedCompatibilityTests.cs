using BasicCore.Builtins;
using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectIntrinsicPolicyCompilerTypedCompatibilityTests
{
    [Test]
    public void Compile_ForbidsTypedIntrinsic_SameWayAsLegacyStringIntrinsic()
    {
        var compiler = CreatePolicyCompiler(["boolean_not"]);
        var typedIr = BuildIr(CreateTypedIntrinsic(BuiltinIntrinsicSymbols.Boolean.Not));
        var legacyIr = BuildIr(new Instruction(UOpCode.Intrinsic, ["boolean_not"]));
        var input = new CompilationInput { SourceText = string.Empty };

        var typedException = Assert.Throws<InvalidOperationException>(() => compiler.Compile(typedIr, input));
        var legacyException = Assert.Throws<InvalidOperationException>(() => compiler.Compile(legacyIr, input));

        Assert.Multiple(() =>
        {
            Assert.That(typedException!.Message, Does.Contain("forbidden by the selected dialect"));
            Assert.That(legacyException!.Message, Does.Contain("forbidden by the selected dialect"));
        });
    }

    private static DialectIntrinsicPolicyCompiler<IAbstractIR> CreatePolicyCompiler(IReadOnlyList<string> forbiddenIntrinsics) =>
        new(
            new PassthroughCompiler(),
            [],
            forbiddenIntrinsics);

    private static Instruction CreateTypedIntrinsic(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument>? typeArguments = null,
        IReadOnlyList<object?>? dataOperands = null)
    {
        var invocation = new IntrinsicInvocation(
            symbol,
            typeArguments ?? [],
            dataOperands ?? []);

        return new Instruction(UOpCode.Intrinsic, [invocation]);
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var air = new AbstractIR();
        air.AppendInstructions(instructions);
        return air;
    }

    private sealed class PassthroughCompiler : IAbstractIrCompiler<IAbstractIR>
    {
        public IReadOnlyList<string> SupportedIntrinsics => ["boolean_not"];

        public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
    }
}