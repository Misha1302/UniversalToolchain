using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterIntrinsicSurfaceTests
{
    private static readonly string[] _forbiddenIntrinsics =
    [
        "load_bool",
        "boolean_and",
        "boolean_or",
        "boolean_not",
        "load_external",
        "store_external",
        "load_local",
        "store_local",
        "load_local_ref",
        "load_i32",
        "load_i64",
        "load_f32",
        "load_f64",
        "load_decimal",
        "add_i32",
        "sub_i32",
        "mul_i32",
        "div_i32",
        "cmp_eq_i32",
        "cmp_gt_i32"
    ];

    [Test]
    public void Interpreter_Allows_CallCSharp()
    {
        var abs = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [-12]),
            new Instruction(UOpCode.Intrinsic, ["call C#", abs]));

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Interpreter_Allows_CallCSharpCtor()
    {
        var ctor = typeof(Version).GetConstructor([typeof(int), typeof(int)])!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", ctor]));

        var result = ExecuteInInterpreter(ir);

        Assert.That(result, Is.TypeOf<Version>());
        Assert.That(((Version)result!).ToString(), Is.EqualTo("1.2"));
    }

    [TestCase("load_local", "x", typeof(int))]
    [TestCase("store_local", "x", typeof(int))]
    [TestCase("load_local_ref", "x", typeof(int))]
    public void Interpreter_Rejects_LocalIntrinsics(string intrinsicName, object arg1, object arg2)
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, [intrinsicName, arg1, arg2]));

        var ex = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(ex!.Message, Does.Contain("supports only 'call C#' and 'call C# ctor'"));
        Assert.That(ex.Message, Does.Contain(intrinsicName));
    }

    [TestCase("load_i32", 7)]
    [TestCase("add_i32")]
    [TestCase("cmp_gt_i32")]
    [TestCase("load_bool", true)]
    [TestCase("boolean_and")]
    public void Interpreter_Rejects_ArithmeticAndComparisonIntrinsics(string intrinsicName, params object[] args)
    {
        var operands = new List<object> { intrinsicName };
        operands.AddRange(args);
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, operands));

        var ex = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(ex!.Message, Does.Contain("supports only 'call C#' and 'call C# ctor'"));
        Assert.That(ex.Message, Does.Contain(intrinsicName));
    }

    [TestCase("load_external", 0, typeof(int))]
    [TestCase("store_external", 0, typeof(int))]
    public void Interpreter_Rejects_ExternalIntrinsics(string intrinsicName, object arg1, object arg2)
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, [intrinsicName, arg1, arg2]));

        var ex = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir));

        Assert.That(ex!.Message, Does.Contain("supports only 'call C#' and 'call C# ctor'"));
        Assert.That(ex.Message, Does.Contain(intrinsicName));
    }

    [Test]
    public void Interpreter_Guardrail_RejectsAllForbiddenIntrinsicSamples()
    {
        foreach (var intrinsicName in _forbiddenIntrinsics)
        {
            var operands = intrinsicName switch
            {
                "load_bool" => new List<object> { intrinsicName, true },
                "load_i32" or "load_i64" or "load_f32" or "load_f64" or "load_decimal" => new List<object> { intrinsicName, 1 },
                "load_external" or "store_external" => new List<object> { intrinsicName, 0, typeof(int) },
                "load_local" or "store_local" or "load_local_ref" => new List<object> { intrinsicName, "x", typeof(int) },
                _ => new List<object> { intrinsicName }
            };

            var ir = BuildIr(new Instruction(UOpCode.Intrinsic, operands));
            var ex = Assert.Throws<RuntimeExecutionException>(() => ExecuteInInterpreter(ir), intrinsicName);
            Assert.That(ex!.Message, Does.Contain("supports only 'call C#' and 'call C# ctor'"), intrinsicName);
        }
    }

    [Test]
    public void InterpreterPipeline_WithOptimizersEnabled_ContainsOnlyUniversalCallIntrinsics()
    {
        var dialect = """
                      dialect Tiny
                      use NativeTypes, BooleanConditions, ComparisonConditions, Conditions, Identifier, Numbers, Scopes, Variables, Whitespaces
                      backend interpreter
                      enable ArithmeticOptimization
                      enable BooleanOptimization
                      enable ComparisonIntrinsicOptimization
                      enable NativeCilOptimization
                      enable EGraphOptimization
                      """;

        using var host = DialectTestHostInfrastructure.CreateInterpreterHost(dialect);
        var compiler = host.GetBackendSpecificArtifactCompiler<IAbstractIR>("interpreter");
        var artifact = compiler.Compile("(1 + 2) > 0 and true");

        var intrinsics = CollectIntrinsicNames(artifact.CompilationOutput).Distinct(StringComparer.Ordinal).ToArray();

        Assert.That(intrinsics, Is.Not.Empty);
        Assert.That(intrinsics, Is.SubsetOf(new[] { "call C#", "call C# ctor" }));
        Assert.That(intrinsics, Has.None.Matches<string>(name => _forbiddenIntrinsics.Contains(name, StringComparer.Ordinal)));
    }

    private static IEnumerable<string> CollectIntrinsicNames(IAbstractIR air)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;

            var normalized = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction);
            yield return (string)normalized.Operands[0];
        }
    }

    private static object? ExecuteInInterpreter(IAbstractIR ir)
    {
        var interpreter = new InterpreterImpl();
        return interpreter.Execute(ir, new ExecutionEnvironment([]));
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }
}
