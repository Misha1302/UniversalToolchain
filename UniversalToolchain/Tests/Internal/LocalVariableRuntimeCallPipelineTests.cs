using AbstractIrExtensions;
using BasicCore.Core;
using BasicCore.Execution;
using BasicInterpreter;
using UniversalIntermediateRepresentation;

namespace Tests.Internal;

public sealed class LocalVariableRuntimeCallPipelineTests
{
    [Test]
    public void LocalVariableLowering_UsesRuntimeCalls_WithoutVariablesContainerOrLocalIntrinsics()
    {
        var ir = new AbstractIR();

        ir.Push(42);
        ir.SetValueToLocal("x", typeof(int));
        ir.LdLoc("x", typeof(int));

        var callOperands = ir.Instructions
            .Where(static x => x.UOpCode == UOpCode.Intrinsic && x.Operands.Count > 1 && Equals(x.Operands[0], "call C#"))
            .Select(static x => x.Operands[1])
            .ToList();

        Assert.That(callOperands.Any(static x => x is CSharpCallDescriptor), Is.True);
        Assert.That(callOperands.Any(static x => x is MethodInfo method && method.DeclaringType == typeof(VariablesRuntimeCalls)), Is.True);
        Assert.That(callOperands.Any(static x => x is MethodInfo method && method.DeclaringType != null && method.DeclaringType.Name.Contains("VariablesContainer", StringComparison.Ordinal)), Is.False);

        var intrinsicNames = ir.Instructions
            .Where(static x => x.UOpCode == UOpCode.Intrinsic)
            .Select(static x => x.Operands[0].ToString())
            .ToList();

        Assert.That(intrinsicNames.Contains("load_local"), Is.False);
        Assert.That(intrinsicNames.Contains("store_local"), Is.False);
        Assert.That(intrinsicNames.Contains("load_local_ref"), Is.False);
    }

    [Test]
    public void VariablesRuntimeContext_IsSessionScoped()
    {
        var first = new ExecutionEnvironment([]);
        var second = new ExecutionEnvironment([]);

        var firstProvider = new VariablesRuntimeCallProvider(first);
        var secondProvider = new VariablesRuntimeCallProvider(second);

        firstProvider.LoadVariablesContext().StoreLocal("x", 10);

        Assert.That(firstProvider.LoadVariablesContext().LoadLocal<int>("x"), Is.EqualTo(10));
        Assert.Throws<InvalidOperationException>(() => secondProvider.LoadVariablesContext().LoadLocal<int>("x"));
    }

    [Test]
    public void RuntimeCallProviderResolver_ReturnsSameProviderPerSession_AndDifferentAcrossSessions()
    {
        var first = new ExecutionEnvironment([]);
        var second = new ExecutionEnvironment([]);

        var firstProviderA = first.GetRequiredProvider(typeof(VariablesRuntimeCallProvider));
        var firstProviderB = first.GetRequiredProvider(typeof(VariablesRuntimeCallProvider));
        var secondProvider = second.GetRequiredProvider(typeof(VariablesRuntimeCallProvider));

        Assert.That(firstProviderA, Is.SameAs(firstProviderB));
        Assert.That(secondProvider, Is.Not.SameAs(firstProviderA));
    }

    [Test]
    public void CallCSharp_WithMethodInfoOperand_RemainsCompatible()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)]);
        Assert.That(method, Is.Not.Null);
        var ir = new AbstractIR();
        ir.Push(-12);
        ir.CallCSharp(method!);

        var interpreter = new InterpreterImpl();
        var result = interpreter.Execute(ir, new ExecutionEnvironment([]));

        Assert.That(result, Is.EqualTo(12));
    }
}
