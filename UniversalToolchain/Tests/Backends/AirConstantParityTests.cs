namespace Tests.Backends;

[TestFixture]
public sealed class AirConstantParityTests
{
    [Test]
    public void TypedNull_InterpreterAndCil_ReturnTheSameValue()
    {
        var air = new AbstractIR();
        air.Push<string?>(null);
        var environment = new ExecutionEnvironment([]);

        var interpreterValue = new InterpreterImpl().Execute(air, environment);
        var cilOutput = new AbstractMethodsCompilerImpl().Compile(
            air,
            new CompilationInput { SourceText = string.Empty });
        var cilValue = new DynamicMethodExecutor().Execute(cilOutput, environment);

        Assert.Multiple(() =>
        {
            Assert.That(interpreterValue, Is.Null);
            Assert.That(cilValue, Is.Null);
        });
    }

    [Test]
    public void NullableValueTypeNull_InterpreterAndCil_ReturnTheSameValue()
    {
        var air = new AbstractIR();
        air.Push<int?>(null);
        var environment = new ExecutionEnvironment([]);

        var interpreterValue = new InterpreterImpl().Execute(air, environment);
        var cilOutput = new AbstractMethodsCompilerImpl().Compile(
            air,
            new CompilationInput { SourceText = string.Empty });
        var cilValue = new DynamicMethodExecutor().Execute(cilOutput, environment);

        Assert.Multiple(() =>
        {
            Assert.That(interpreterValue, Is.Null);
            Assert.That(cilValue, Is.Null);
        });
    }

    [Test]
    public void MultipleTerminalValues_AreRejectedByBothBackends()
    {
        var air = new AbstractIR();
        air.Push(1);
        air.Push(2);

        var interpreterException = Assert.Throws<RuntimeExecutionException>(() =>
            new InterpreterImpl().Execute(air, new ExecutionEnvironment([])));
        var compilerException = Assert.Throws<InvalidOperationException>(() =>
            new AbstractMethodsCompilerImpl().Compile(
                air,
                new CompilationInput { SourceText = string.Empty }));

        Assert.Multiple(() =>
        {
            Assert.That(interpreterException!.Message, Does.Contain("expected zero or one"));
            Assert.That(compilerException!.Message, Does.Contain("expected zero or one"));
        });
    }

    [Test]
    public void RawNullPush_IsRejectedByBothBackends()
    {
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Push, [null])]);
        var environment = new ExecutionEnvironment([]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RuntimeExecutionException>(() => new InterpreterImpl().Execute(air, environment));
            Assert.Throws<InvalidOperationException>(() =>
                new AbstractMethodsCompilerImpl().Compile(
                    air,
                    new CompilationInput { SourceText = string.Empty }));
        });
    }
}
