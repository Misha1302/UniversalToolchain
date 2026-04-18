using BasicCore.Builtins;
using BasicCore.Capabilities;
using ConditionsModule.Enums;
using ConditionsModule.Optimizers;

namespace Tests.Internal;

[TestFixture]
public sealed class ComparisonIntrinsicOptimizerTypeFlowTests
{
    [Test]
    public void ApplyInstructionTypes_UsesSharedProcessor_ForNonBuiltinIntrinsicFlow()
    {
        var optimizer = CreateOptimizer();
        var lessOrEqualMethod = typeof(Comparisons)
            .GetMethod(nameof(Comparisons.LessOrEqual), BindingFlags.Public | BindingFlags.Static)?
            .MakeGenericMethod(typeof(double));
        Assert.That(lessOrEqualMethod, Is.Not.Null);

        var input = CreateIr(
            new Instruction(UOpCode.Push, [1.0d]),
            new Instruction(UOpCode.Push, [2.0d]),
            new Instruction(UOpCode.Intrinsic, ["call C#", lessOrEqualMethod!]));

        Assert.DoesNotThrow(() => optimizer.ProcessIr(input, new FakeCompiler()));

        var optimized = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(BuiltinIntrinsicInstruction.Is(optimized.Instructions[^1], BuiltinIntrinsicSymbols.Comparison.LessOrEqual), Is.True);
    }

    private static ComparisonIntrinsicOptimizerModule CreateOptimizer()
    {
        var optimizer = new ComparisonIntrinsicOptimizerModule();
        optimizer.InitIntrinsicCapabilityContext(new OptimizerIntrinsicCapabilityContext(new FakeCapabilitySet(CreateComparisonCapabilities())));
        return optimizer;
    }

    private static AbstractIR CreateIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static (IntrinsicSymbol Symbol, Type? RuntimeType)[] CreateComparisonCapabilities()
    {
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

        return symbols
            .SelectMany(symbol => types.Select(type => (symbol, (Type?)type)))
            .ToArray();
    }

    private sealed class FakeCompiler : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public object Compile(IAbstractIR air, CompilationInput input) => throw new NotSupportedException();
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