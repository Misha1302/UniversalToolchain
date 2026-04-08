using System.Reflection;
using BasicCore.Compilation;
using BasicCore.Contracts;
using ConditionsModule.Enums;
using ConditionsModule.Optimizers;
using ConditionsModule.Visitors;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;
using NativeMathModule;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class TypedIntrinsicEmitterOptimizerTests
{
    [Test]
    public void BooleanOptimizer_EmitsTypedBooleanIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new BooleanOptimizerModule(),
            (BuiltinIntrinsicSymbols.Boolean.And, null),
            (BuiltinIntrinsicSymbols.Boolean.Or, null),
            (BuiltinIntrinsicSymbols.Boolean.Not, null));
        var method = typeof(BooleanVisitor.BooleanOperations).GetMethod(nameof(BooleanVisitor.BooleanOperations.Not), BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "flag", typeof(bool)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        AssertTypedIntrinsic(result.Instructions[^1], BuiltinIntrinsicSymbols.Boolean.Not);
    }

    [Test]
    public void ArithmeticOptimizer_EmitsTypedArithmeticIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            (BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int)),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, typeof(int)),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(int)),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, typeof(int)),
            (BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(long)),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, typeof(long)),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(long)),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, typeof(long)),
            (BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(float)),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, typeof(float)),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(float)),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, typeof(float)),
            (BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(double)),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, typeof(double)),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(double)),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, typeof(double)));
        var method = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Add), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        AssertTypedIntrinsic(result.Instructions[^1], BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int));
    }

    [Test]
    public void ComparisonOptimizer_EmitsTypedComparisonIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new ComparisonIntrinsicOptimizerModule(),
            (BuiltinIntrinsicSymbols.Comparison.Equal, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.NotEqual, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.Greater, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.Less, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.LessOrEqual, typeof(int)),
            (BuiltinIntrinsicSymbols.Comparison.Equal, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.NotEqual, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.Greater, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.Less, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.LessOrEqual, typeof(long)),
            (BuiltinIntrinsicSymbols.Comparison.Equal, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.NotEqual, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.Greater, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.Less, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.LessOrEqual, typeof(float)),
            (BuiltinIntrinsicSymbols.Comparison.Equal, typeof(double)),
            (BuiltinIntrinsicSymbols.Comparison.NotEqual, typeof(double)),
            (BuiltinIntrinsicSymbols.Comparison.Greater, typeof(double)),
            (BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual, typeof(double)),
            (BuiltinIntrinsicSymbols.Comparison.Less, typeof(double)),
            (BuiltinIntrinsicSymbols.Comparison.LessOrEqual, typeof(double)));
        var method = typeof(Comparisons).GetMethod(nameof(Comparisons.Less), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        AssertTypedIntrinsic(result.Instructions[^1], BuiltinIntrinsicSymbols.Comparison.Less, typeof(int));
    }

    [Test]
    public void NativeCilOptimizer_EmitsTypedLoadConstIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(int)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(long)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(float)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(double)));
        var input = CreateIr(new Instruction(UOpCode.Push, [1.5d]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        AssertTypedIntrinsic(
            result.Instructions[0],
            BuiltinIntrinsicSymbols.Core.LoadConst,
            typeof(double),
            1.5d);
    }

    private static TOptimizer CreateOptimizer<TOptimizer>(
        TOptimizer optimizer,
        params (IntrinsicSymbol Symbol, Type? RuntimeType)[] supportedIntrinsics)
        where TOptimizer : IIRProcessingModule
    {
        optimizer.InitIntrinsicCapabilityContext(new OptimizerIntrinsicCapabilityContext(new FakeCapabilitySet(supportedIntrinsics)));
        return optimizer;
    }

    private static void AssertTypedIntrinsic(
        Instruction instruction,
        IntrinsicSymbol expectedSymbol,
        Type? expectedRuntimeType = null,
        params object?[] expectedDataOperands)
    {
        Assert.That(instruction.TryGetTypedIntrinsicInvocation(out var invocation), Is.True);
        Assert.That(invocation.Symbol, Is.EqualTo(expectedSymbol));

        if (expectedRuntimeType is null)
            Assert.That(invocation.TypeArguments, Is.Empty);
        else
            Assert.That(invocation.TypeArguments, Is.EqualTo(new[] { IntrinsicTypeArgument.From(expectedRuntimeType) }));

        Assert.That(invocation.DataOperands, Is.EqualTo(expectedDataOperands));
    }

    private static AbstractIR CreateIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private sealed class FakeCompiler : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public object Compile(IAbstractIR air, CompilationInput input)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeCapabilitySet(
        params (IntrinsicSymbol Symbol, Type? RuntimeType)[] supportedIntrinsics) : IIntrinsicCapabilitySet
    {
        private readonly HashSet<(IntrinsicSymbol Symbol, Type? RuntimeType)> _supportedIntrinsics = supportedIntrinsics.ToHashSet();

        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments)
        {
            if (_supportedIntrinsics.Contains((symbol, null)))
                return true;

            var runtimeType = typeArguments.Count == 1 ? typeArguments[0].RuntimeType : null;
            return _supportedIntrinsics.Contains((symbol, runtimeType));
        }
    }
}
