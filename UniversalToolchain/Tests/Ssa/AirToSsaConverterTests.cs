using IntermediateRepresentationAbstractions;
using System.Reflection;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Lowering;
using UniversalIntermediateRepresentation;

namespace Tests.Ssa;

[TestFixture]
public sealed class AirToSsaConverterTests
{
    [Test]
    public void Run_WhenAirUsesSupportedSubset_ProducesVerifiableSsa()
    {
        var high = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var merge = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var ir = new AbstractIR();
        ir.Push(true);
        ir.JmpIf(high);
        ir.Push(10);
        ir.Jmp(merge);
        ir.SetLabel(high);
        ir.Push(20);
        ir.SetLabel(merge);

        var result = new AirToSsaConverter().Run(new AirArtifact(ir), new IrPipelineContext());
        var artifact = result.Artifact.As<SsaArtifact>();

        Assert.That(artifact.Kind, Is.EqualTo(SsaIrKinds.Ssa));
        Assert.That(artifact.Module.Functions.Single().Blocks, Has.Count.EqualTo(4));
        Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        Assert.That(result.Facts.Contains(UniversalToolchain.Air.Analysis.AirFacts.ControlFlowGraph), Is.False);
    }

    [Test]
    public void Run_WhenAirUsesSupportedArithmeticIntrinsic_ProducesCallableInstruction()
    {
        var ir = new AbstractIR();
        ir.Push(2);
        ir.Push(3);
        ir.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = new AirToSsaConverter().Run(new AirArtifact(ir), new IrPipelineContext());
        var block = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single();
        var call = block.Instructions.OfType<SsaCall>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(call.Callee, Is.EqualTo(SsaPreviewCallables.AddInt32Unchecked));
            Assert.That(call.Operands, Has.Count.EqualTo(2));
            Assert.That(call.Results.Single().Type, Is.EqualTo(SsaTypes.Int32));
            Assert.That(block.Terminator!.Operands, Is.EqualTo(new[] { call.Results.Single().Id }));
        });
    }

    [Test]
    public void Run_WhenAirUsesManagedStaticMethod_ProducesManagedCallableInstruction()
    {
        var method = typeof(AirToSsaConverterTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!;
        var ir = new AbstractIR();
        ir.Push(41);
        ir.AppendInstructions(new List<Instruction>
        {
            new(UOpCode.Intrinsic, [AirIntrinsicIds.CallCSharp, method])
        });

        var result = new AirToSsaConverter().Run(new AirArtifact(ir), new IrPipelineContext());
        var block = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single();
        var call = block.Instructions.OfType<SsaCall>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(SsaManagedCallables.IsManagedCallable(call.Callee), Is.True);
            Assert.That(call.Operands, Has.Count.EqualTo(1));
            Assert.That(call.Results.Single().Type, Is.EqualTo(SsaTypes.Int32));
            Assert.That(block.Terminator!.Operands, Is.EqualTo(new[] { call.Results.Single().Id }));
        });
    }

    [Test]
    public void Run_WhenAirUsesManagedConstructor_ProducesObjectCallableInstruction()
    {
        var constructor = typeof(ManagedTestBox).GetConstructor(Type.EmptyTypes)!;
        var ir = new AbstractIR();
        ir.AppendInstructions(new List<Instruction>
        {
            new(UOpCode.Intrinsic, [AirIntrinsicIds.CallCSharpConstructor, constructor])
        });

        var result = new AirToSsaConverter().Run(new AirArtifact(ir), new IrPipelineContext());
        var block = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single();
        var call = block.Instructions.OfType<SsaCall>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(SsaManagedCallables.IsManagedCallable(call.Callee), Is.True);
            Assert.That(call.Operands, Is.Empty);
            Assert.That(call.Results.Single().Type, Is.EqualTo(SsaTypes.Object));
            Assert.That(block.Terminator!.Operands, Is.EqualTo(new[] { call.Results.Single().Id }));
        });
    }

    [Test]
    public void Run_WhenAirUsesUnsupportedIntrinsic_ThrowsDiagnosticException()
    {
        var ir = new AbstractIR();
        ir.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<AirToSsaConversionException>(() =>
            new AirToSsaConverter().Run(new AirArtifact(ir), new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    private static int AddOne(int value) => value + 1;

    private sealed class ManagedTestBox;
}
