using System.Reflection;
using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.Execution;
using ConditionsModule.Enums;
using ConditionsModule.Optimizers;
using ConditionsModule.Visitors;
using IntermediateRepresentationAbstractions;
using NativeMathModule;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class TypedIntrinsicEmitterOptimizerTests
{
    [Test]
    public void ArithmeticOptimizer_WhenCapabilityMissingDecimal_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(false).ToArray());
        var method = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Add), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

        Assert.That(result, Is.SameAs(input));
    }

    [Test]
    public void ArithmeticOptimizer_WhenCapabilitySupportsAllTypes_RewritesToTypedIntrinsic()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(true).ToArray());
        var method = typeof(NativeArithmetic).GetMethod(nameof(NativeArithmetic.Add), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(typeof(int));
        Assert.That(method, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [2]),
            new Instruction(UOpCode.Push, [3]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

        AssertTypedIntrinsic(result.Instructions[^1], BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int));
    }

    [Test]
    public void ArithmeticOptimizer_WhenZeroPrecedesManagedExternalLoad_DoesNotConsumePartialOperandSequence()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(true).ToArray());
        var multiply = typeof(NativeArithmetic)
            .GetMethod(nameof(NativeArithmetic.Multiply), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(typeof(int));

        var input = CreateIr(
            new Instruction(UOpCode.Push, [0]),
            IntrinsicInstructionFactory.CreateForCapability(
                "call C#",
                ExternalRuntimeMethodDescriptors.LoadEnvironmentDescriptor),
            new Instruction(UOpCode.Push, [0]),
            IntrinsicInstructionFactory.CreateForCapability(
                "call C#",
                ExternalRuntimeMethodDescriptors.CreateLoadExternalMethod(typeof(int))),
            IntrinsicInstructionFactory.CreateForCapability("call C#", multiply));

        var result = optimizer.Optimize(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Instructions, Has.Count.EqualTo(5));
            Assert.That(AirPushOperand.GetValue(result.Instructions[0].Operands.Single()), Is.EqualTo(0));
            Assert.That(CSharpCallIntrinsicReader.TryGetCallDescriptor(result.Instructions[1], out _), Is.True);
            Assert.That(AirPushOperand.GetValue(result.Instructions[2].Operands.Single()), Is.EqualTo(0));
            Assert.That(CSharpCallIntrinsicReader.TryGetCallMethod(result.Instructions[3], out _), Is.True);
            AssertTypedIntrinsic(result.Instructions[4], BuiltinIntrinsicSymbols.Arithmetic.Multiply, typeof(int));
        });
    }

    [Test]
    public void ArithmeticOptimizer_WhenFoldingInt32ZeroMultiplication_PreservesInt32RuntimeType()
    {
        var optimizer = CreateOptimizer(
            new ArithmeticOptimizerModule(),
            CreateArithmeticCapabilities(true).ToArray());
        var multiply = typeof(NativeArithmetic)
            .GetMethod(nameof(NativeArithmetic.Multiply), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(typeof(int));
        var input = CreateIr(
            new Instruction(UOpCode.Push, [0]),
            new Instruction(UOpCode.Push, [1]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", multiply));

        var result = optimizer.Optimize(input);
        var value = AirPushOperand.GetValue(result.Instructions.Single().Operands.Single());

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.TypeOf<int>());
            Assert.That(value, Is.EqualTo(0));
        });
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
            IntrinsicInstructionFactory.CreateForCapability("load_local", "flag", typeof(bool)),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

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
            IntrinsicInstructionFactory.CreateForCapability("load_local", "flag", typeof(bool)),
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

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
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

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
            IntrinsicInstructionFactory.CreateForCapability("call C#", method!));

        var result = optimizer.Optimize(input);

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

        var result = optimizer.Optimize(input);

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

        var result = optimizer.Optimize(input);

        AssertTypedIntrinsic(result.Instructions[0], BuiltinIntrinsicSymbols.Core.LoadConst, typeof(double), 1.5d);
    }

    [Test]
    public void NativeCilOptimizer_WhenCapabilitySupportsRequestedExternalType_RewritesManagedCallSequence()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(double)));

        var input = CreateIr(
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.LoadEnvironmentDescriptor),
            new Instruction(UOpCode.Push, [2]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.CreateLoadExternalMethod(typeof(double))));

        var result = optimizer.Optimize(input);

        Assert.That(result.Instructions, Has.Count.EqualTo(1));
        AssertTypedIntrinsic(result.Instructions[0], BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(double), 2);
    }

    [Test]
    public void NativeCilOptimizer_WhenCapabilityDoesNotSupportRequestedExternalType_KeepsManagedCallSequence()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(int)),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(long)),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(float)),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(decimal)));

        var input = CreateIr(
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.LoadEnvironmentDescriptor),
            new Instruction(UOpCode.Push, [2]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.CreateLoadExternalMethod(typeof(double))));

        var result = optimizer.Optimize(input);

        Assert.That(result, Is.SameAs(input));
    }


    [Test]
    public void NativeCilOptimizer_WhenOnlyRequestedExternalTypeIsSupported_DoesNotRequireOtherExternalTypes()
    {
        var optimizer = CreateOptimizer(
            new NativeCilOptimizerModule(),
            (BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(double)));

        var input = CreateIr(
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.LoadEnvironmentDescriptor),
            new Instruction(UOpCode.Push, [2]),
            IntrinsicInstructionFactory.CreateForCapability("call C#", ExternalRuntimeMethodDescriptors.CreateLoadExternalMethod(typeof(double))));

        var result = optimizer.Optimize(input);

        Assert.That(result.Instructions, Has.Count.EqualTo(1));
        AssertTypedIntrinsic(result.Instructions[0], BuiltinIntrinsicSymbols.Core.LoadExternal, typeof(double), 2);
    }

    [Test]
    public void EGraphOptimizer_WhenCapabilityMissingLoadLocal_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(
            new EGraphOptimizerModule(),
            CreateArithmeticCapabilities(false).ToArray());

        var input = CreateIr(
            BuiltinIntrinsicInstruction.Create(BuiltinIntrinsicSymbols.Storage.LoadLocal, typeof(int), ["x", typeof(int)]),
            new Instruction(UOpCode.Push, [0]),
            BuiltinIntrinsicInstruction.Create(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int)));

        var result = optimizer.Optimize(input);

        Assert.That(result, Is.SameAs(input));
    }

    private static TOptimizer CreateOptimizer<TOptimizer>(
        TOptimizer optimizer,
        params (IntrinsicSymbol Symbol, Type? RuntimeType)[] supportedIntrinsics)
        where TOptimizer : IAirOptimizer
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