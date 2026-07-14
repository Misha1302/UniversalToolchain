namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterStackSafetyTests
{
    [TestCase(UOpCode.Drop)]
    [TestCase(UOpCode.JmpIf)]
    [TestCase(UOpCode.JmpIfNot)]
    public void Execute_WhenInstructionConsumesEmptyStack_ThrowsRuntimeExecutionException(UOpCode opcode)
    {
        var air = new AbstractIR();
        var instruction = opcode == UOpCode.Drop
            ? new Instruction(opcode)
            : new Instruction(opcode, [Guid.NewGuid()]);
        air.AppendInstructions([instruction]);

        var exception = Assert.Throws<RuntimeExecutionException>(() =>
            new InterpreterImpl().Execute(air, new ExecutionEnvironment([])));

        Assert.That(exception!.Message, Does.Contain("requires a value on the evaluation stack"));
    }
}
