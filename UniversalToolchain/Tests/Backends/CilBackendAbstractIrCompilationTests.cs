using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;
using System.Reflection.Emit;

namespace Tests.Backends;

[TestFixture]
public class CilBackendAbstractIrCompilationTests
{
    [Test]
    public void SupportedIntrinsics_HaveDescriptorCompileAndTypeHandlers_ForEveryPublishedName()
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var registry = new CilIntrinsicRegistry();

        Assert.That(compiler.SupportedIntrinsics, Is.EqualTo(registry.SupportedIntrinsics));
        Assert.That(compiler.SupportedIntrinsics.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(compiler.SupportedIntrinsics.Count));

        foreach (var intrinsicName in compiler.SupportedIntrinsics)
        {
            var descriptor = registry.GetRequired(intrinsicName);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor, Is.Not.Null, $"Intrinsic '{intrinsicName}' must resolve to a descriptor.");
                Assert.That(descriptor.Name, Is.EqualTo(intrinsicName), $"Descriptor name mismatch for intrinsic '{intrinsicName}'.");
                Assert.That(descriptor.Compile, Is.Not.Null, $"Intrinsic '{intrinsicName}' must expose compile handling.");
                Assert.That(descriptor.ProcessTypes, Is.Not.Null, $"Intrinsic '{intrinsicName}' must expose type-stack handling.");
            });
        }
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
    public void LoadLocalRef_WithRefCall_UsesBackendTypeSimulation()
    {
        var incrementRef = typeof(CilBackendAbstractIrCompilationTests)
            .GetMethod(nameof(IncrementRef), BindingFlags.NonPublic | BindingFlags.Static);

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [41]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local_ref", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", incrementRef!])
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
    public void ArithmeticIntrinsicI32_ProducesCorrectResultWithoutOptimizer()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [19]),
            new Instruction(UOpCode.Push, [23]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void BooleanNotIntrinsic_ProducesCorrectResultWithoutOptimizer()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["boolean_not"])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void UnknownNumericLoaderIntrinsic_ThrowsInvalidOperationException()
    {
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["load_x128", 1]));

        Assert.Throws<InvalidOperationException>(() => CompileAndExecute(ir));
    }

    [Test]
    public void SupportedIntrinsics_ExposeRegisteredFamilies()
    {
        var supportedIntrinsics = new AbstractMethodsCompilerImpl().SupportedIntrinsics;

        Assert.Multiple(() =>
        {
            Assert.That(supportedIntrinsics, Contains.Item("call C#"));
            Assert.That(supportedIntrinsics, Contains.Item("load_decimal"));
            Assert.That(supportedIntrinsics, Contains.Item("add_decimal"));
            Assert.That(supportedIntrinsics, Contains.Item("cmp_le_f64"));
            Assert.That(supportedIntrinsics.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(supportedIntrinsics.Count));
        });
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
    public void BranchWithComparisonCondition_InfersMergedReturnType_WithoutOptimizer()
    {
        var branchTrue = Guid.NewGuid();
        var end = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [7]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["cmp_gt_i32"]),
            new Instruction(UOpCode.JmpIf, [branchTrue]),
            new Instruction(UOpCode.Push, ["no"]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [branchTrue]),
            new Instruction(UOpCode.Push, ["yes"]),
            new Instruction(UOpCode.Label, [end])
        );

        var compiled = Compile(ir);
        var result = Execute(compiled);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(result, Is.EqualTo("yes"));
        });
    }

    [Test]
    public void NestedBranches_InfersBooleanReturnType_WithoutOptimizer()
    {
        var outerTrue = Guid.NewGuid();
        var innerTrue = Guid.NewGuid();
        var end = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [outerTrue]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [outerTrue]),
            new Instruction(UOpCode.Push, [2.5d]),
            new Instruction(UOpCode.Push, [2.5d]),
            new Instruction(UOpCode.Intrinsic, ["cmp_le_f64"]),
            new Instruction(UOpCode.JmpIf, [innerTrue]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [innerTrue]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Label, [end])
        );

        var compiled = Compile(ir);
        var result = Execute(compiled);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(result, Is.EqualTo(true));
        });
    }

    [Test]
    public void LocalLoadAfterBranchMerge_UsesMergedLabelState_WithoutOptimizer()
    {
        var branchTrue = Guid.NewGuid();
        var end = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.JmpIf, [branchTrue]),
            new Instruction(UOpCode.Push, [41]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Jmp, [end]),
            new Instruction(UOpCode.Label, [branchTrue]),
            new Instruction(UOpCode.Push, [40]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Label, [end]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", typeof(CilBackendAbstractIrCompilationTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!])
        );

        var compiled = Compile(ir);
        var result = Execute(compiled);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.ReturnType, Is.EqualTo(typeof(int)));
            Assert.That(result, Is.EqualTo(41));
        });
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


    [Test]
    public void TypedArithmeticIntrinsicI32_ProducesCorrectResult()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [19]),
            new Instruction(UOpCode.Push, [23]),
            CreateTypedIntrinsic(BuiltinIntrinsicSymbols.Arithmetic.Add, [IntrinsicTypeArgument.From(typeof(int))])
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TypedBooleanNotIntrinsic_ProducesCorrectResult()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            CreateTypedIntrinsic(BuiltinIntrinsicSymbols.Boolean.Not)
        );

        var result = CompileAndExecute(ir);

        Assert.That(result, Is.EqualTo(false));
    }

    private static int AddOne(int value) => value + 1;

    private static int IncrementRef(ref int value)
    {
        value++;
        return value;
    }

    private static int CombineDigits(int acc, int nextDigit) => acc * 10 + nextDigit;

    private static T Echo<T>(T value) => value;


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
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static DynamicMethod Compile(IAbstractIR ir)
    {
        var compiler = new AbstractMethodsCompilerImpl();
        var input = new CompilationInput { SourceText = string.Empty };
        return compiler.Compile(ir, input);
    }

    private static object Execute(DynamicMethod method)
    {
        var executor = new DynamicMethodExecutor();
        return executor.Execute(method, new ExecutionEnvironment([]));
    }

    private static object CompileAndExecute(IAbstractIR ir)
    {
        return Execute(Compile(ir));
    }

    private sealed class ReflectionTarget(int seed)
    {
        public int IncrementBy(int value) => seed + value;
    }
}
