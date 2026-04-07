namespace Tests.Infrastructure;

[TestFixture]
public class CilBackendAbstractIrCompilationTests
{
    [Test]
    public void SupportedIntrinsics_AreUniqueSortedAndIncludeRegisteredFamilies()
    {
        var compiler = new AbstractMethodsCompilerImpl();

        Assert.Multiple(() =>
        {
            Assert.That(compiler.SupportedIntrinsics, Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(compiler.SupportedIntrinsics.Distinct(StringComparer.Ordinal), Is.EqualTo(compiler.SupportedIntrinsics));
            Assert.That(compiler.SupportedIntrinsics, Does.Contain("call C#"));
            Assert.That(compiler.SupportedIntrinsics, Does.Contain("load_decimal"));
            Assert.That(compiler.SupportedIntrinsics, Does.Contain("add_decimal"));
            Assert.That(compiler.SupportedIntrinsics, Does.Contain("cmp_le_f64"));
        });
    }

    [Test]
    public void LocalStoreAndLoad_WithStaticCall_ProducesCorrectResult()
    {
        var addOneMethod = typeof(CilBackendAbstractIrCompilationTests)
            .GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [41]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", addOneMethod!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void BranchWithStackMerge_InfersReturnTypeAndExecutesCorrectly()
    {
        var trueLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [trueLabel]),
            new Instruction(UOpCode.Push, ["left"]),
            new Instruction(UOpCode.Jmp, [endLabel]),
            new Instruction(UOpCode.Label, [trueLabel]),
            new Instruction(UOpCode.Push, ["right"]),
            new Instruction(UOpCode.Label, [endLabel])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo("right"));
    }

    [Test]
    public void GenericMethodCall_ResolvesMethodViaReflection()
    {
        var genericEcho = typeof(CilBackendAbstractIrCompilationTests)
            .GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, ["generic"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", genericEcho!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo("generic"));
    }

    [Test]
    public void ConstructorAndInstanceCall_UsesReflectionMembersCorrectly()
    {
        var ctor = typeof(ReflectionTarget).GetConstructor([typeof(int)]);
        var increment = typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.IncrementBy), [typeof(int)]);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [40]),
            new Instruction(UOpCode.Intrinsic, ["call C# ctor", ctor!]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", increment!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ComparisonIntrinsicI32_ProducesCorrectResult()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [7]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["cmp_gt_i32"])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void ComparisonIntrinsicF64_ProducesCorrectResult()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [2.5d]),
            new Instruction(UOpCode.Push, [2.5d]),
            new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void UnknownNumericLoaderIntrinsic_ThrowsInvalidOperationException()
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["load_x128", 1]));

        Assert.Throws<InvalidOperationException>(() => CompileAndExecute(ir));
    }

    [Test]
    public void DeepNestedConditionsWithSharedStackState_HandlesComplexControlFlow()
    {
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var branch3 = Guid.NewGuid();
        var afterInner = Guid.NewGuid();
        var finish = Guid.NewGuid();

        var combineMethod = typeof(CilBackendAbstractIrCompilationTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [branch1]),
            new Instruction(UOpCode.Push, [999]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Label, [branch1]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [branch2]),
            new Instruction(UOpCode.Jmp, [finish]),
            new Instruction(UOpCode.Label, [branch2]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.JmpIf, [branch3]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Jmp, [afterInner]),
            new Instruction(UOpCode.Label, [branch3]),
            new Instruction(UOpCode.Push, [8]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [afterInner]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [finish])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(1234));
    }

    [Test]
    public void BranchingAndDropPipeline_CombinesStackOperationsWithoutLeakingGarbage()
    {
        var toBranch = Guid.NewGuid();
        var end = Guid.NewGuid();

        var combineMethod = typeof(CilBackendAbstractIrCompilationTests)
            .GetMethod(nameof(CombineDigits), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Push, [111]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [toBranch]),
            new Instruction(UOpCode.Push, [77]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [toBranch]),
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!]),
            new Instruction(UOpCode.Label, [end]),
            new Instruction(UOpCode.Push, [6]),
            new Instruction(UOpCode.Intrinsic, ["call C#", combineMethod!])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(1056));
    }

    private static int AddOne(int value) => value + 1;

    private static int CombineDigits(int acc, int nextDigit) => acc * 10 + nextDigit;

    private static T Echo<T>(T value) => value;

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static object CompileAndExecute(IAbstractIR ir)
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var input = new CompilationInput { SourceText = string.Empty };
        var compiled = compiler.Compile(ir, input);
        var executor = new DynamicMethodExecutor();
        return executor.Execute(compiled, new ExecutionEnvironment([]));
    }

    private sealed class ReflectionTarget(int seed)
    {
        public int IncrementBy(int value) => seed + value;
    }
}
