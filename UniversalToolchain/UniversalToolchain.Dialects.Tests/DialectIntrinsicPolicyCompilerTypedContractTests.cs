using BasicCore.Builtins;
using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectIntrinsicPolicyCompilerTypedContractTests
{
    [Test]
    public void Compile_ForbidsTypedIntrinsic()
    {
        var compiler = CreatePolicyCompiler(["boolean_not"]);
        var ir = BuildIr(CreateTypedIntrinsic(BuiltinIntrinsicSymbols.Boolean.Not));
        var input = new CompilationInput { SourceText = string.Empty };

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, input));

        Assert.That(exception!.Message, Does.Contain("forbidden by the selected dialect"));
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