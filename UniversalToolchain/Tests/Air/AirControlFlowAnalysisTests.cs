using IntermediateRepresentationAbstractions;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalIntermediateRepresentation;

namespace Tests.Air;

[TestFixture]
public sealed class AirControlFlowAnalysisTests
{
    [Test]
    public void Build_ShouldCreateDeterministicBlocksForConditionalBranch()
    {
        var label = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ir = new AbstractIR();
        ir.Push(true);
        ir.JmpIf(label);
        ir.Push(10);
        ir.SetLabel(label);
        ir.Push(20);

        var result = new AirControlFlowGraphBuilder().Build(ir.Instructions);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Graph.Blocks.Select(static x => x.Id.ToString()), Is.EqualTo(new[] { "b0000", "b0002", "label_aaaaaaaa_aaaa_aaaa_aaaa_aaaaaaaaaaaa" }));
        Assert.That(result.Graph.Blocks[0].Terminator.Kind, Is.EqualTo(AirBlockTerminatorKind.ConditionalJump));
        Assert.That(result.Graph.Blocks[0].Terminator.Successors.Select(static x => x.Kind), Is.EquivalentTo(new[] { AirControlFlowEdgeKind.ConditionTrue, AirControlFlowEdgeKind.ConditionFalse }));
    }

    [Test]
    public void Verify_WhenJumpTargetIsUnknown_ReturnsDiagnostic()
    {
        var ir = new AbstractIR();
        ir.Jmp(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var result = new StructuralAirVerifier().Verify(new AirArtifact(ir), new IrPipelineContext());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.cfg.invalid"));
    }

    [Test]
    public void Verify_WhenMergeStackDepthDiffers_ReturnsDiagnostic()
    {
        var merge = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var ir = new AbstractIR();
        ir.Push(true);
        ir.JmpIf(merge);
        ir.Push(1);
        ir.SetLabel(merge);

        var result = new StructuralAirVerifier().Verify(new AirArtifact(ir), new IrPipelineContext());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    [Test]
    public void Verify_WhenConditionalJumpHasNoFallthrough_ReturnsTerminatorShapeDiagnostic()
    {
        var loop = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var ir = new AbstractIR();
        ir.SetLabel(loop);
        ir.Push(true);
        ir.JmpIf(loop);

        var result = new StructuralAirVerifier().Verify(new AirArtifact(ir), new IrPipelineContext());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.terminator.target-count"));
    }
}
