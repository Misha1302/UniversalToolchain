using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.Dialects.Tests;

public class DialectIntrinsicPolicyCompilerTypedCompatibilityTests
{
    [Test]
    public void Compile_ForbidsTypedIntrinsic_SameWayAsLegacyStringIntrinsic()
    {
        var compiler = CreatePolicyCompiler(forbiddenIntrinsics: ["boolean_not"]);
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

    private static DialectIntrinsicPolicyCompiler<IAbstractIR> CreatePolicyCompiler(IReadOnlyList<string> forbiddenIntrinsics)
    {
        return new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            new PassthroughCompiler(),
            allowedIntrinsics: [],
            forbiddenIntrinsics: forbiddenIntrinsics,
            hasExplicitAllowList: false);
    }

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
