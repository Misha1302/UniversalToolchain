using BasicCilCompiler.Execution;
using BasicCore.Execution;
using BasicCore.Builtins;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using BytecodeDynamicMethodsCompiler.Compilers;
using BytecodeDynamicMethodsCompiler.Core;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.LanguageSdk.Generic.Tests;

/// <summary>
/// End-to-end proof that a registry descriptor is enough to make a non-core intrinsic <b>work</b> on the
/// CIL backend: parse the AIR, simulate the type stack, emit IL and execute.
/// </summary>
/// <remarks>
/// <para>
/// The increment this pins is narrow on purpose. Earlier, a descriptor's <c>ProcessTypes</c> was recorded
/// and never consulted, so an external intrinsic could emit and then be rejected during type simulation.
/// Combined tests catch a handler being called; this test is what proves the intrinsic actually produces
/// the right answer, because a handler can be called and still be wrong.
/// </para>
/// <para>
/// The intrinsic deliberately has semantics no core family has: it takes two <see cref="int"/> and returns
/// a <see cref="string"/>. A core family would have hidden a mistake in the type-stack handler, because
/// arithmetic returns its own operand type.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ExternalCilIntrinsicEndToEndTests
{
    private const string FormatIntrinsic = "acme.describe_pair_i32";

    /// <summary>
    /// A package-style helper standing in for what an external package would compile to: a real .NET method
    /// the intrinsic can call. The point is that nothing about it is privileged - the descriptor names it.
    /// </summary>
    private static string DescribePair(int left, int right) => $"{left}+{right}={left + right}";

    private static CilIntrinsicRegistry RegistryWithFormatIntrinsic() =>
        new(
        [
            new CilIntrinsicDescriptor(
                FormatIntrinsic,
                static (context, _, stack) =>
                {
                    // Replace the two operands with the call to the helper, leaving its result on the stack.
                    AbstractMethodsIntrinsicCompiler.CompileCallCSharp(
                        context,
                        IntrinsicInstructionFactory.CreateForCapability(
                            "call C#",
                            typeof(ExternalCilIntrinsicEndToEndTests)
                                .GetMethod(nameof(DescribePair), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!),
                        stack);
                },
                static (_, stack) =>
                {
                    // Two ints in, one string out. Nothing in the core families does this, so a wrong
                    // handler cannot accidentally agree with a built-in one.
                    stack.RemoveRange(stack.Count - 2, 2);
                    stack.Add(typeof(string));
                })
        ]);

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new UniversalIntermediateRepresentation.AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object Execute(IAbstractIR ir, CilIntrinsicRegistry registry)
    {
        var compiler = new AbstractMethodsCompilerImpl(registry);
        var output = compiler.Compile(ir, new CompilationInput { SourceText = string.Empty });
        return new DynamicMethodExecutor().Execute(output, new ExecutionEnvironment([]));
    }

    [Test]
    public void ExternalIntrinsic_CompilesAndExecutes_OnCil()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            IntrinsicInstructionFactory.CreateForCapability(FormatIntrinsic));

        var result = Execute(ir, RegistryWithFormatIntrinsic());

        Assert.That(result, Is.EqualTo("2+3=5"),
            "a descriptor-only intrinsic must compile and execute, not merely pass type simulation");
    }

    [Test]
    public void ExternalIntrinsic_RegistersItsName_SoTheSurfaceIsDiscoverable()
    {
        // The supported-intrinsic surface is what a contract descriptor publishes to the rest of the
        // toolchain. If registration does not surface the name, an external intrinsic exists only for the
        // code that deliberately constructs this registry.
        var registry = new CilIntrinsicRegistry(
        [
            new CilIntrinsicDescriptor(FormatIntrinsic, static (_, _, _) => { }, static (_, _) => { })
        ]);

        Assert.That(registry.SupportedIntrinsics, Does.Contain(FormatIntrinsic));
    }

    [Test]
    public void ExternalIntrinsic_TypeStackMismatch_FailsBeforeEmittingIl()
    {
        // Type simulation exists to reject a malformed AIR before IL is emitted. A handler that pops two
        // values must not silently accept a one-value stack: doing so would defer the failure to the CLR,
        // where the message names a stack imbalance instead of the instruction that caused it.
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2]),
            IntrinsicInstructionFactory.CreateForCapability(FormatIntrinsic));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Execute(ir, RegistryWithFormatIntrinsic()));

        Assert.That(exception, Is.Not.Null,
            "the intrinsic must be rejected during type simulation, not during CLR execution");
    }

    [Test]
    public void DefaultRegistry_DoesNotContainTheExternalIntrinsic()
    {
        // The negative half: the proof above must come from the descriptor supplied in the test, not from a
        // name that leaked into the default set.
        Assert.That(new CilIntrinsicRegistry().SupportedIntrinsics, Does.Not.Contain(FormatIntrinsic));
    }
}
