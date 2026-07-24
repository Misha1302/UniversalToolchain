using BasicCore.Builtins;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using System.Reflection;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRoundtripRouteTests
{
    [Test]
    public void Run_WhenPolicyIsOff_ReturnsInputWithoutUsingSsa()
    {
        var source = new AbstractIR();
        source.Push(42);

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Off);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Run_WhenPolicyIsPreferAndRouteIsUnsupported_FallsBackWithDiagnostics()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Prefer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
        });
    }

    [Test]
    public void Run_WhenPolicyIsRequireAndRouteIsUnsupported_ThrowsDiagnosticException()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<SsaRouteException>(() =>
            new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Require));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    [Test]
    public void Run_WhenPolicyIsDebugAndRouteIsSupported_ReturnsOptimizedRoundtrippedAir()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Report.ExecutedPasses, Is.Not.Empty);
            Assert.That(result.Report.Trace.Select(static entry => entry.Stage), Does.Contain("optimization"));
            Assert.That(result.Report.OutputAirInstructionCount, Is.LessThan(result.Report.InputAirInstructionCount));
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push
            }));
            Assert.That(result.Program.Instructions[1].Operands.Single(), Is.EqualTo(5));
        });
    }

    [Test]
    public void Run_WhenInputUsesTypedLoadConstant_NormalizesBeforeSsa()
    {
        var source = new AbstractIR();
        source.AppendInstructions(
        [
            BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(int),
                [42])
        ]);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Report.Trace.Select(static entry => entry.Stage), Does.Contain("normalization"));
            Assert.That(result.Program.Instructions.Select(static instruction => instruction.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push
            }));
            Assert.That(result.Program.Instructions[1].Operands.Single(), Is.EqualTo(42));
        });
    }

    [Test]
    public void Run_WhenInputLoadsExternalInt32_PreservesSlotAcrossRoundtrip()
    {
        var source = new AbstractIR();
        source.AppendInstructions(
        [
            BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadExternal,
                typeof(int),
                [3])
        ]);
        source.Push(2);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        var externalLoad = result.Program.Instructions.Single(static instruction =>
            instruction.UOpCode == UOpCode.Intrinsic &&
            BuiltinIntrinsicInstruction.Is(instruction, BuiltinIntrinsicSymbols.Core.LoadExternal));
        var invocation = IntrinsicInstructionView.ReadOrThrow(externalLoad).Invocation;

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(invocation.Symbol, Is.EqualTo(BuiltinIntrinsicSymbols.Core.LoadExternal));
            Assert.That(invocation.TypeArguments.Select(static x => x.RuntimeType), Is.EqualTo(new[] { typeof(int) }));
            Assert.That(invocation.DataOperands, Is.EqualTo(new object?[] { 3, typeof(int) }));
        });
    }

    [Test]
    public void Run_WhenManagedCallableAndExternalLoadCoexist_PreservesCoreDescriptorsAcrossRoundtrip()
    {
        var method = typeof(SsaRoundtripRouteTests)
            .GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!;
        var source = new AbstractIR();
        source.AppendInstructions(
        [
            BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadExternal,
                typeof(int),
                [0])
        ]);
        source.Push(40);
        source.AppendInstructions(
        [
            IntrinsicInstructionFactory.CreateForCapability(AirIntrinsicIds.CallCSharp, method)
        ]);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaRouteProfiles.Create(SsaRoutePolicy.Require))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Any(static instruction =>
                BuiltinIntrinsicInstruction.Is(instruction, BuiltinIntrinsicSymbols.Core.LoadExternal)), Is.True);
            Assert.That(result.Program.Instructions.Any(static instruction =>
                CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out _)), Is.True);
        });
    }

    [Test]
    public void Run_WhenUsingRawConvertersWithoutProfile_DoesNotRunOptimizationPasses()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = new SsaRoundtripRoute(
                SsaRouteFactory.CreateLowerer(SsaRouteProfiles.Create(SsaRoutePolicy.Debug)),
                SsaRouteFactory.CreateEmitter(SsaRouteProfiles.Create(SsaRoutePolicy.Debug)))
            .Run(source, SsaRoutePolicy.Debug);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push,
                UOpCode.Push,
                UOpCode.Intrinsic
            }));
        });
    }
    private static int AddOne(int value) => value + 1;

}
