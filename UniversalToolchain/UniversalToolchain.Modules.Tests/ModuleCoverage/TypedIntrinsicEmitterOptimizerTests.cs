using System.Reflection;
using BasicCore.Compilation;
using BasicCore.Contracts;
using ConditionsModule.Enums;
using ConditionsModule.Optimizers;
using ConditionsModule.Visitors;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;
using NativeMathModule;
using SettableGettableModule.Core;
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
    public void ArithmeticOptimizer_WhenCapabilityMissingDecimal_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(includeDecimal: false).ToArray());
        var method = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Add), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void ArithmeticOptimizer_WhenCapabilitySupportsAllTypes_RewritesToTypedIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(includeDecimal: true).ToArray());
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
    public void BooleanOptimizer_WhenCapabilityIncomplete_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new BooleanOptimizerModule(),
            (BuiltinIntrinsicSymbols.Boolean.And, null),
            (BuiltinIntrinsicSymbols.Boolean.Or, null));
        var method = typeof(BooleanVisitor.BooleanOperations).GetMethod(nameof(BooleanVisitor.BooleanOperations.Not), BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Intrinsic, ["load_local", "flag", typeof(bool)]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void BooleanOptimizer_WhenCapabilitySupportsFamily_RewritesToTypedIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new BooleanOptimizerModule(),
            (BuiltinIntrinsicSymbols.Boolean.And, null),
            (BuiltinIntrinsicSymbols.Boolean.Or, null),
            (BuiltinIntrinsicSymbols.Boolean.Not, null));
        var method = typeof(BooleanVisitor.BooleanOperations).GetMethod(nameof(BooleanVisitor.BooleanOperations.Not), BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [true]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(BuiltinIntrinsicInstruction.Is(result.Instructions[^1], BuiltinIntrinsicSymbols.Boolean.Not), Is.True);
    }

    [Test]
    public void ComparisonOptimizer_WhenCapabilityIncomplete_ReturnsInputUnchanged()
    {
        var capability = CreateComparisonCapabilities();
        capability.Remove((BuiltinIntrinsicSymbols.Comparison.Less, typeof(double)));

        var optimizer = CreateOptimizer(new ComparisonIntrinsicOptimizerModule(), capability.ToArray());
        var method = typeof(Comparisons).GetMethod(nameof(Comparisons.Less), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [1]),
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void ComparisonOptimizer_WhenCapabilitySupportsFamily_RewritesToTypedIntrinsic()
    {
        var optimizer = CreateOptimizer(new ComparisonIntrinsicOptimizerModule(), CreateComparisonCapabilities().ToArray());
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
    public void NativeCilOptimizer_WhenCapabilityMissingDecimal_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(int)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(long)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(float)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(double)));

        var input = CreateIr(new Instruction(UOpCode.Push, [1.5d]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void NativeCilOptimizer_WhenCapabilitySupportsAllLoadConstTypes_RewritesToTypedIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(int)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(long)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(float)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(double)),
            (BuiltinIntrinsicSymbols.Core.LoadConst, typeof(decimal)));

        var input = CreateIr(new Instruction(UOpCode.Push, [1.5d]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        AssertTypedIntrinsic(result.Instructions[0], BuiltinIntrinsicSymbols.Core.LoadConst, typeof(double), 1.5d);
    }

    [Test]
    public void LocalVariablesOptimizer_WhenCapabilityMissingLoadLocalRef_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new LocalVariablesOptimizer(),
            (BuiltinIntrinsicSymbols.Storage.LoadLocal, null),
            (BuiltinIntrinsicSymbols.Storage.StoreLocal, null));
        var method = typeof(VariablesContainer<int>)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(x => x.Name == nameof(VariablesContainer<int>.Get) &&
                                 x.GetParameters().Length == 1 &&
                                 x.GetParameters()[0].ParameterType == typeof(string));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, ["x"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!]));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void EGraphOptimizer_WhenCapabilityMissingLoadLocal_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new EGraphOptimizerModule(),
            CreateArithmeticCapabilities(includeDecimal: false).ToArray());

        var input = CreateIr(
            BuiltinIntrinsicInstruction.Create(BuiltinIntrinsicSymbols.Storage.LoadLocal, typeof(int), ["x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            BuiltinIntrinsicInstruction.Create(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int)));

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    private static TOptimizer CreateOptimizer<TOptimizer>(
        TOptimizer optimizer,
        params (IntrinsicSymbol Symbol, Type? RuntimeType)[] supportedIntrinsics)
        where TOptimizer : IIRProcessingModule
    {
        optimizer.InitIntrinsicCapabilityContext(new OptimizerIntrinsicCapabilityContext(new FakeCapabilitySet(supportedIntrinsics)));
        return optimizer;
    }

    private static HashSet<(IntrinsicSymbol Symbol, Type? RuntimeType)> CreateArithmeticCapabilities(bool includeDecimal)
    {
        var supported = new HashSet<(IntrinsicSymbol Symbol, Type? RuntimeType)>();
        var types = new[] { typeof(int), typeof(long), typeof(float), typeof(double) };

        foreach (var type in types)
        {
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Add, type));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Subtract, type));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Multiply, type));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Divide, type));
        }

        if (includeDecimal)
        {
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(decimal)));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Subtract, typeof(decimal)));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(decimal)));
            supported.Add((BuiltinIntrinsicSymbols.Arithmetic.Divide, typeof(decimal)));
        }

        return supported;
    }

    private static HashSet<(IntrinsicSymbol Symbol, Type? RuntimeType)> CreateComparisonCapabilities()
    {
        var supported = new HashSet<(IntrinsicSymbol Symbol, Type? RuntimeType)>();
        var symbols = new[]
        {
            BuiltinIntrinsicSymbols.Comparison.Equal,
            BuiltinIntrinsicSymbols.Comparison.NotEqual,
            BuiltinIntrinsicSymbols.Comparison.Greater,
            BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
            BuiltinIntrinsicSymbols.Comparison.Less,
            BuiltinIntrinsicSymbols.Comparison.LessOrEqual
        };
        var types = new[] { typeof(int), typeof(long), typeof(float), typeof(double) };

        foreach (var type in types)
        foreach (var symbol in symbols)
            supported.Add((symbol, type));

        return supported;
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
