using System.Reflection;
using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;
using SettableGettableModule.Core;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class LocalVariablesOptimizerCapabilityTests
{
    [Test]
    public void ProcessIr_WhenCapabilitySupportsStorageIntrinsics_RewritesLoadPattern()
    {
        var optimizer = CreateOptimizer(CreateCapabilityContext(
            BuiltinIntrinsicSymbols.Storage.LoadLocal,
            BuiltinIntrinsicSymbols.Storage.StoreLocal,
            BuiltinIntrinsicSymbols.Storage.LoadLocalRef));
        var input = CreateLoadPatternIr();

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.Not.SameAs(input));
        Assert.That(result.Instructions.Count, Is.EqualTo(1));
        Assert.That(result.Instructions[0].UOpCode, Is.EqualTo(UOpCode.Intrinsic));
        Assert.That(result.Instructions[0].Operands[0], Is.EqualTo("load_local"));
        Assert.That(result.Instructions[0].Operands[1], Is.EqualTo("x"));
        Assert.That(result.Instructions[0].Operands[2], Is.EqualTo(typeof(int)));
    }

    [Test]
    public void ProcessIr_WhenCapabilityDoesNotSupportStorageIntrinsics_ReturnsInputUnchanged()
    {
        var optimizer = CreateOptimizer(CreateCapabilityContext());
        var input = CreateLoadPatternIr();

        var result = optimizer.ProcessIr(input, new FakeCompiler());

        Assert.That(result, Is.SameAs(input));
    }

    private static LocalVariablesOptimizer CreateOptimizer(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        var optimizer = new LocalVariablesOptimizer();
        optimizer.InitIntrinsicCapabilityContext(capabilityContext);
        return optimizer;
    }

    private static IOptimizerIntrinsicCapabilityContext CreateCapabilityContext(params IntrinsicSymbol[] supportedSymbols)
    {
        return new OptimizerIntrinsicCapabilityContext(new FakeCapabilitySet(supportedSymbols));
    }

    private static AbstractIR CreateLoadPatternIr()
    {
        var method = typeof(VariablesContainer<int>).GetMethod(nameof(VariablesContainer<int>.Get), BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);

        var air = new AbstractIR();
        air.AppendInstructions(
        [
            new Instruction(UOpCode.Push, ["x"]),
            new Instruction(UOpCode.Intrinsic, ["call C#", method!])
        ]);
        return air;
    }

    private sealed class FakeCompiler : IAbstractIrCompiler<object>
    {
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public object Compile(IAbstractIR air, CompilationInput input)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeCapabilitySet(params IntrinsicSymbol[] supportedSymbols) : IIntrinsicCapabilitySet
    {
        private readonly HashSet<IntrinsicSymbol> _supportedSymbols = supportedSymbols.ToHashSet();

        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments)
        {
            return _supportedSymbols.Contains(symbol);
        }
    }
}
