using BasicCore.Capabilities;
using NativeMathModule;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class NativeCilOptimizerCapabilityContractTests
{
    [Test]
    public void ProcessIr_WhenBackendDoesNotSupportNativeLoadIntrinsics_ShouldReturnOriginalIr()
    {
        var optimizer = new NativeCilOptimizerModule();
        optimizer.InitIntrinsicCapabilityContext(new EmptyCapabilityContext());
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Push, [123])
        ]);
        var compiler = new UnsupportedNativeLoadCompiler();

        var result = optimizer.ProcessIr(ir, compiler);

        Assert.That(result, Is.SameAs(ir));
        Assert.That(result.Instructions, Is.EqualTo(ir.Instructions));
    }

    [Test]
    public void ProcessIr_WhenCapabilityContextWasNotInitialized_ShouldFailClearly()
    {
        var optimizer = new NativeCilOptimizerModule();
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Push, [123])
        ]);
        var compiler = new UnsupportedNativeLoadCompiler();

        var exception = Assert.Throws<NullReferenceException>(() => optimizer.ProcessIr(ir, compiler));

        Assert.That(exception!.Message, Does.Contain("capability context initialization"));
    }

    private sealed class EmptyCapabilityContext : IOptimizerIntrinsicCapabilityContext
    {
        public bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments) => false;

        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments) => false;
    }

    private sealed class UnsupportedNativeLoadCompiler : IAbstractIrCompiler<IAbstractIR>
    {
        public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
    }
}
