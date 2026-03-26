namespace Tests.Native;

[TestFixture]
public class CSharpInteropResolutionAndNegativeContractsTests
{
    [Test]
    public void SameInteropCall_ShouldResolveSameOverload_AcrossRepeatedExecutions()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var ir = BuildIr(new Instruction(UOpCode.Push, [-7]), new Instruction(UOpCode.Intrinsic, ["call C#", method]));

        var baseline = ExecuteInInterpreter(ir);
        for (var i = 0; i < 20; i++)
        {
            Assert.That(ExecuteInInterpreter(ir), Is.EqualTo(baseline));
        }
    }

    [Test]
    public void AmbiguousInteropCall_ShouldFailPredictably()
    {
        var ir = BuildIr(new Instruction(UOpCode.Push, [1]), new Instruction(UOpCode.Push, [2]), new Instruction(UOpCode.Intrinsic, ["call C#", typeof(InteropHost).GetMethods().Single(x => x.Name == nameof(InteropHost.Combine) && x.GetParameters()[0].ParameterType == typeof(int))]));

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo("int"));
    }

    [Test]
    public void ConstructorInterop_ShouldSelectExpectedConstructor()
    {
        var ctor = typeof(InteropHost).GetConstructor([typeof(int)])!;
        var add = typeof(InteropHost).GetMethod(nameof(InteropHost.Add), [typeof(int)])!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [40]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", ctor]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", add])
        );

        Assert.That(ExecuteInInterpreter(ir), Is.EqualTo(42));
    }

    [Test]
    public void NonPublicInteropTarget_ShouldRemainDeterministic()
    {
        var hidden = typeof(InteropHost).GetMethod("Hidden", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["call C#", hidden]));

        Assert.That(ExecuteInInterpreter(ir), Is.EqualTo(0));
    }

    [Test]
    public void UnsupportedRefOutInteropCall_ShouldRemainDeterministic()
    {
        var tryParse = typeof(int).GetMethod(nameof(int.TryParse), [typeof(string), typeof(int).MakeByRefType()])!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["7"]),
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Intrinsic, ["call C#", tryParse])
        );

        Assert.That(ExecuteInInterpreter(ir), Is.EqualTo(true));
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object? ExecuteInInterpreter(IAbstractIR ir)
    {
        return new InterpreterImpl().Execute(ir, new ExecutionEnvironment([]));
    }

    public sealed class InteropHost
    {
        private readonly int _seed;

        public InteropHost(int seed)
        {
            _seed = seed;
        }

        public int Add(int value) => _seed + value;
        public static string Combine(int x, int y) => "int";
        public static string Combine(long x, long y) => "long";
        private static int Hidden() => 0;
    }
}
