namespace Tests.Intrinsics;

[TestFixture]
public sealed class InterpreterIntrinsicSurfaceContractTests
{
    [TestCase("load_const")]
    [TestCase("load_external")]
    [TestCase("load_local")]
    [TestCase("store_local")]
    [TestCase("native_add_i32")]
    public void Execute_WhenBackendSpecificIntrinsicIsPassed_ShouldRejectIt(string intrinsicName)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Intrinsic, [intrinsicName])
        ]);
        var interpreter = new InterpreterImpl();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            interpreter.Execute(ir, new ExecutionEnvironment([])));

        Assert.That(exception!.Message, Does.Contain("Unsupported intrinsic"));
        Assert.That(exception.Message, Does.Contain(intrinsicName));
    }

    [Test]
    public void Execute_WhenLegacyCallCSharpIntrinsicIsPassed_ShouldRemainSupported()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)]);
        Assert.That(method, Is.Not.Null);
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Push, [-7]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!])
        ]);
        var interpreter = new InterpreterImpl();

        var result = interpreter.Execute(ir, new ExecutionEnvironment([]));

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Execute_WhenLegacyCallCSharpCtorIntrinsicIsPassed_ShouldRemainSupported()
    {
        var constructor = typeof(InvalidOperationException).GetConstructor([typeof(string)]);
        Assert.That(constructor, Is.Not.Null);
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Push, ["created"]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", constructor!])
        ]);
        var interpreter = new InterpreterImpl();

        var result = interpreter.Execute(ir, new ExecutionEnvironment([]));

        Assert.That(result, Is.TypeOf<InvalidOperationException>());
        Assert.That(((InvalidOperationException)result!).Message, Is.EqualTo("created"));
    }
}
