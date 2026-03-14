namespace Tests.Infrastructure;

public class OptimizerRegressionTests
{
    [Test]
    public void BooleanNot_ShouldKeepStackTypesValid()
    {
        var module = new BooleanOptimizerModule();
        var compiler = new FakeCompiler(["boolean_and", "boolean_or", "boolean_not"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["boolean_not"])
        );

        var optimized = module.ProcessIr(ir, compiler);
        var stack = new List<Type>();

        Assert.DoesNotThrow(() => optimized.Instructions.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic));
        Assert.That(stack, Has.Count.EqualTo(1));
        Assert.That(stack[0], Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void BooleanOptimizer_ShouldFoldLiteralNotToPush()
    {
        var module = new BooleanOptimizerModule();
        var compiler = new FakeCompiler(["boolean_and", "boolean_or", "boolean_not"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["boolean_not"])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo(false));
    }

    [Test]
    public void BooleanOptimizer_ShouldFoldLiteralAndAndOr()
    {
        var module = new BooleanOptimizerModule();
        var compiler = new FakeCompiler(["boolean_and", "boolean_or", "boolean_not"]);
        var andIr = BuildIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.Intrinsic, ["boolean_and"])
        );
        var orIr = BuildIr(
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["boolean_or"])
        );

        var andOptimized = module.ProcessIr(andIr, compiler);
        var orOptimized = module.ProcessIr(orIr, compiler);

        Assert.That(andOptimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(andOptimized.Instructions[0].Operands[0], Is.EqualTo(false));
        Assert.That(orOptimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(orOptimized.Instructions[0].Operands[0], Is.EqualTo(true));
    }

    [Test]
    public void BooleanOptimizer_ShouldApplyOnlySafeIdentityRules()
    {
        var module = new BooleanOptimizerModule();
        var compiler = new FakeCompiler(["boolean_and", "boolean_or", "boolean_not"]);
        var andTrueIr = BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "flag", typeof(bool)]),
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["boolean_and"])
        );
        var andFalseIr = BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "flag", typeof(bool)]),
            new Instruction(UOpCode.Push, [false]),
            new Instruction(UOpCode.Intrinsic, ["boolean_and"])
        );

        var andTrueOptimized = module.ProcessIr(andTrueIr, compiler);
        var andFalseOptimized = module.ProcessIr(andFalseIr, compiler);

        Assert.That(andTrueOptimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(andTrueOptimized.Instructions[0].Operands[0], Is.EqualTo("load_local"));

        Assert.That(andFalseOptimized.Instructions, Has.Count.EqualTo(3));
        Assert.That(andFalseOptimized.Instructions[2].Operands[0], Is.EqualTo("boolean_and"));
    }

    [Test]
    public void NativeCilOptimizer_ShouldKeepDecimalPush_WhenDecimalIntrinsicUnsupported()
    {
        var module = new NativeCilOptimizerModule();
        var compiler = new FakeCompiler(["load_i32", "load_i64", "load_f32", "load_f64"]);
        var ir = BuildIr(new Instruction(UOpCode.Push, [1.25m]));

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(1));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo(1.25m));
    }

    [Test]
    public void ComparisonIntrinsicOptimizer_ShouldReplaceCallAndPreserveStackTyping()
    {
        var module = new ComparisonIntrinsicOptimizerModule();
        var compiler = new FakeCompiler([
            "cmp_eq_i32", "cmp_ne_i32", "cmp_gt_i32", "cmp_ge_i32", "cmp_lt_i32", "cmp_le_i32",
            "cmp_eq_i64", "cmp_ne_i64", "cmp_gt_i64", "cmp_ge_i64", "cmp_lt_i64", "cmp_le_i64",
            "cmp_eq_f32", "cmp_ne_f32", "cmp_gt_f32", "cmp_ge_f32", "cmp_lt_f32", "cmp_le_f32",
            "cmp_eq_f64", "cmp_ne_f64", "cmp_gt_f64", "cmp_ge_f64", "cmp_lt_f64", "cmp_le_f64"
        ]);
        var lessMethod = typeof(Comparisons).GetMethod(nameof(Comparisons.Less))!;
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Push, [8]),
            new Instruction(UOpCode.Intrinsic, ["call C#", lessMethod])
        );

        var optimized = module.ProcessIr(ir, compiler);
        var stack = new List<Type>();

        Assert.That(optimized.Instructions[^1].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[^1].Operands[0], Is.EqualTo("cmp_lt_i32"));
        Assert.DoesNotThrow(() => optimized.Instructions.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic));
        Assert.That(stack, Has.Count.EqualTo(1));
        Assert.That(stack[0], Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void ComparisonIntrinsicOptimizer_ShouldUseGenericMethodType_WhenStackTypesUnavailable()
    {
        var module = new ComparisonIntrinsicOptimizerModule();
        var compiler = new FakeCompiler([
            "cmp_eq_i32", "cmp_ne_i32", "cmp_gt_i32", "cmp_ge_i32", "cmp_lt_i32", "cmp_le_i32",
            "cmp_eq_i64", "cmp_ne_i64", "cmp_gt_i64", "cmp_ge_i64", "cmp_lt_i64", "cmp_le_i64",
            "cmp_eq_f32", "cmp_ne_f32", "cmp_gt_f32", "cmp_ge_f32", "cmp_lt_f32", "cmp_le_f32",
            "cmp_eq_f64", "cmp_ne_f64", "cmp_gt_f64", "cmp_ge_f64", "cmp_lt_f64", "cmp_le_f64"
        ]);
        var equalIntMethod = typeof(Comparisons).GetMethod(nameof(Comparisons.Equal))!.MakeGenericMethod(typeof(int));
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Push, [2L]),
            new Instruction(UOpCode.Intrinsic, ["call C#", equalIntMethod])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(3));
        Assert.That(optimized.Instructions[^1].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[^1].Operands[0], Is.EqualTo("cmp_eq_i32"));
    }

    [Test]
    public void LabelsSharedData_GetIdByName_ShouldReturnStableIdForSameName()
    {
        var data = new LabelsSharedData();

        var id1 = data.GetIdByName("label");
        var id2 = data.GetIdByName("label");
        var id3 = data.GetGuidByName("label");

        Assert.That(id2, Is.EqualTo(id1));
        Assert.That(id3, Is.EqualTo(id1));
    }


    [Test]
    public void ScopesCreator_ShouldThrowOnUnclosedOpeningBracket()
    {
        var creator = new ScopesCreator();
        var root = new AstNode(
            ExtensibleEnum<AstNodeTag>.CreateOrGet("Root"),
            null,
            [new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("OpenPar"), null, [])]
        );

        var exception = Assert.Throws<InvalidOperationException>(() => creator.TryCreateNode(root, 0));
        Assert.That(exception!.Message, Does.Contain("opening bracket was not closed"));
    }

    [Test]
    public void LocalVariablesOptimizer_ShouldNotRemoveNearbyInstructionsForNonMatchingPattern()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var getRefInt = typeof(VariablesContainer<int>).GetMethod(nameof(VariablesContainer<int>.GetRef))!;
        var setValueToString = GetSetValueToMethod().MakeGenericMethod(typeof(string), typeof(VariableReference<string>));

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Push, ["x"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", getRefInt]),
            new Instruction(UOpCode.Intrinsic, ["call C#", setValueToString]),
            new Instruction(UOpCode.Push, [7])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[1].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[2].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[3].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
    }


    [Test]
    public void LocalVariablesOptimizer_ShouldOptimizeOnlyExactPattern_WhenSimilarSequenceIsNearby()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var getRefInt = typeof(VariablesContainer<int>).GetMethod(nameof(VariablesContainer<int>.GetRef))!;
        var setValueToInt = GetSetValueToMethod().MakeGenericMethod(typeof(int), typeof(VariableReference<int>));
        var setValueToString = GetSetValueToMethod().MakeGenericMethod(typeof(string), typeof(VariableReference<string>));

        var ir = BuildIr(
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Push, ["x"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", getRefInt]),
            new Instruction(UOpCode.Intrinsic, ["call C#", setValueToInt]),
            new Instruction(UOpCode.Push, ["x"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", getRefInt]),
            new Instruction(UOpCode.Intrinsic, ["call C#", setValueToString]),
            new Instruction(UOpCode.Push, [1])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(6));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[0].Operands[0], Is.EqualTo(10));
        Assert.That(optimized.Instructions[1].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[2].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[3].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[4].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(optimized.Instructions[5].UOpCode, Is.EqualTo(UOpCode.Push));
    }


    [Test]
    public void LocalRoundtripPass_ShouldRemoveAdjacentStoreLoad_WhenValueIsNotReadLater()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Drop)
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions.Select(x => x.ToString()), Is.EqualTo(new[]
        {
            new Instruction(UOpCode.Push, [1]).ToString(),
            new Instruction(UOpCode.Drop).ToString()
        }));
    }

    [Test]
    public void LocalRoundtripPass_ShouldFoldStoreLoadStoreIntoDirectStore()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "y", typeof(int)])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(2));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[1].Operands[1], Is.EqualTo("y"));
    }


    [Test]
    public void LocalRoundtripPass_ShouldRemoveStoreLoad_WhenUsedInPureArithmeticAndNotReadLater()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Push, [4]),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]),
            new Instruction(UOpCode.Drop)
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions.Select(x => x.ToString()), Is.EqualTo(new[]
        {
            new Instruction(UOpCode.Push, [3]).ToString(),
            new Instruction(UOpCode.Push, [4]).ToString(),
            new Instruction(UOpCode.Intrinsic, ["add_i32"]).ToString(),
            new Instruction(UOpCode.Drop).ToString()
        }));
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimize_WhenLocalIsReadBeforeBoundary()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [7]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimize_WhenPatternTouchesBranchTarget()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var label = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Jmp, [label]),
            new Instruction(UOpCode.Label, [label]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
    }

    [Test]
    public void LocalRoundtripPass_ShouldKeepStackNonNegative_OnStraightLineSequence()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [11]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "y", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "y", typeof(int)]),
            new Instruction(UOpCode.Drop)
        );

        var optimized = module.ProcessIr(ir, compiler);
        var stack = new List<Type>();

        Assert.DoesNotThrow(() => optimized.Instructions.ManipulateTypesStack(stack, AirTypes.ProcessTypesIntrinsic));
        Assert.That(stack, Is.Empty);
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimizeRoundtripInsideLoop()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var loopLabel = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Label, [loopLabel]),
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Jmp, [loopLabel])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[3].Operands[0], Is.EqualTo("load_local"));
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimizeLdlocStloc_WhenImmediatelyAfterLabel()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var loopLabel = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Label, [loopLabel]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Jmp, [loopLabel])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("load_local"));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("store_local"));
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimizeStoreLoadStoreInsideLoop()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var loopLabel = Guid.NewGuid();
        var ir = BuildIr(
            new Instruction(UOpCode.Label, [loopLabel]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "y", typeof(int)]),
            new Instruction(UOpCode.Jmp, [loopLabel])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[3].Operands[0], Is.EqualTo("load_local"));
        Assert.That(optimized.Instructions[4].Operands[0], Is.EqualTo("store_local"));
    }

    [Test]
    public void LocalRoundtripPass_ShouldRecognizeCilStyleLocalAliases()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Intrinsic, ["ldloc.0"]),
            new Instruction(UOpCode.Intrinsic, ["stloc.0"])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Is.Empty);
    }

    [Test]
    public void LocalRoundtripPass_ShouldNotOptimizeCilStyleStoreLoad_WhenLocalReadBeforeBoundary()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [5]),
            new Instruction(UOpCode.Intrinsic, ["stloc.s", "0"]),
            new Instruction(UOpCode.Intrinsic, ["ldloc.s", "0"]),
            new Instruction(UOpCode.Drop),
            new Instruction(UOpCode.Intrinsic, ["ldloc.s", "0"])
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(ir.Instructions.Count));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("stloc.s"));
        Assert.That(optimized.Instructions[2].Operands[0], Is.EqualTo("ldloc.s"));
    }

    [Test]
    public void LocalRoundtripPass_ShouldOptimizeLdlocStloc_WhenNotNearLabelOrTarget()
    {
        var module = new LocalVariablesOptimizer();
        var compiler = new FakeCompiler(["store_local", "load_local", "load_local_ref"]);
        var ir = BuildIr(
            new Instruction(UOpCode.Push, [10]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["load_local", "x", typeof(int)]),
            new Instruction(UOpCode.Intrinsic, ["store_local", "x", typeof(int)]),
            new Instruction(UOpCode.Drop)
        );

        var optimized = module.ProcessIr(ir, compiler);

        Assert.That(optimized.Instructions, Has.Count.EqualTo(3));
        Assert.That(optimized.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Push));
        Assert.That(optimized.Instructions[1].Operands[0], Is.EqualTo("store_local"));
        Assert.That(optimized.Instructions[2].UOpCode, Is.EqualTo(UOpCode.Drop));
    }

    private static MethodInfo GetSetValueToMethod()
    {
        var helperType = typeof(AbstractIrExtensions.AbstractIrExtensions)
            .GetNestedType("VariablesHelper", BindingFlags.NonPublic)!;
        return helperType.GetMethod("SetValueTo", BindingFlags.Public | BindingFlags.Static)!;
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private sealed class FakeCompiler(IReadOnlyList<string> intrinsics) : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => intrinsics;

        public object Compile(IAbstractIR air, CompilationInput input) => Thrower.NotSupported<object>("Fake compiler cannot compile IR in this test scenario.");
    }
}