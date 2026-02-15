using System.Reflection;
using AbstractIrExtensions;
using DotnetAirHelper;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;
using NativeMathModule;
using SettableGettableModule;
using UniversalIntermediateRepresentation;

namespace Tests;

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

        public object Compile(IAbstractIR air, OrderedDictionary<string, Type> parameters) => Thrower.NotSupported<object>("Fake compiler cannot compile IR in this test scenario.");
    }
}