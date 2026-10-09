using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace Tests.Internal;

[TestFixture]
public sealed class DeclarativeBytecodeOperationTests
{
    private sealed record HeldOutOperation(int Value) : IBytecodeOperationData;
    private sealed record UnknownOperation : IBytecodeOperationData;

    private sealed class HeldOutHandler : BytecodeOperationHandler<HeldOutOperation>
    {
        public int EmitCount { get; private set; }

        protected override IAbstractIR Emit(HeldOutOperation operation, IAbstractMethodConvertable.Context context)
        {
            EmitCount++;
            var air = new AbstractIR();
            air.Push(operation.Value);
            return air;
        }
    }

    private sealed class DuplicateHeldOutHandler : BytecodeOperationHandler<HeldOutOperation>
    {
        protected override IAbstractIR Emit(HeldOutOperation operation, IAbstractMethodConvertable.Context context) =>
            new AbstractIR();
    }

    [Test]
    public void HeldOutOperation_ResolvesWithoutCentralDispatcherChange()
    {
        var handler = new HeldOutHandler();
        var registry = new BytecodeOperationHandlerRegistry([handler]);
        var method = new DeclarativeMethodImpl("held-out", new HeldOutOperation(42), registry);

        var first = method.GetAbstractIR(new IAbstractMethodConvertable.Context([]));
        var second = method.GetAbstractIR(new IAbstractMethodConvertable.Context([]));

        Assert.Multiple(() =>
        {
            Assert.That(method.Operation, Is.TypeOf<HeldOutOperation>());
            Assert.That(handler.EmitCount, Is.EqualTo(2));
            Assert.That(first.Instructions, Has.Count.EqualTo(1));
            Assert.That(second.Instructions, Has.Count.EqualTo(1));
            Assert.That(first.Instructions[0].ToString(), Is.EqualTo(second.Instructions[0].ToString()));
        });
    }

    [Test]
    public void UnknownOperation_FailsClosedBeforeExecution()
    {
        var registry = new BytecodeOperationHandlerRegistry([new HeldOutHandler()]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new DeclarativeMethodImpl("unknown", new UnknownOperation(), registry));

        Assert.That(exception!.Message, Does.Contain("No bytecode operation handler"));
    }

    [Test]
    public void DuplicateOperationHandler_FailsClosedIndependentlyOfRegistrationOrder()
    {
        var first = Assert.Throws<InvalidOperationException>(() =>
            new BytecodeOperationHandlerRegistry([new HeldOutHandler(), new DuplicateHeldOutHandler()]));
        var second = Assert.Throws<InvalidOperationException>(() =>
            new BytecodeOperationHandlerRegistry([new DuplicateHeldOutHandler(), new HeldOutHandler()]));

        Assert.Multiple(() =>
        {
            Assert.That(first!.Message, Does.Contain(typeof(HeldOutOperation).FullName!));
            Assert.That(second!.Message, Does.Contain(typeof(HeldOutOperation).FullName!));
        });
    }
}
