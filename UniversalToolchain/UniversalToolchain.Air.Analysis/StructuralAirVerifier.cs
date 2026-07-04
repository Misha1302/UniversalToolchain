using IntermediateRepresentationAbstractions;
using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Air.Analysis;

public sealed class StructuralAirVerifier : IIrVerifier
{
    private readonly AirControlFlowGraphBuilder _builder;
    private readonly AirStackAnalyzer _stackAnalyzer;

    public StructuralAirVerifier()
        : this(new AirControlFlowGraphBuilder(), new AirStackAnalyzer(AirCoreIntrinsicDescriptors.DefaultResolver))
    {
    }

    public StructuralAirVerifier(AirControlFlowGraphBuilder builder, AirStackAnalyzer stackAnalyzer)
    {
        _builder = builder;
        _stackAnalyzer = stackAnalyzer;
    }

    public IrKind Kind => AirIrKinds.Air;

    public IrVerificationResult Verify(IIrArtifact artifact, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact is not AirArtifact airArtifact)
        {
            return new IrVerificationResult(
            [
                Diagnostic("air.artifact.type", $"Expected AIR artifact, got artifact kind '{artifact.Kind}'.")
            ]);
        }

        var diagnostics = new List<IrDiagnostic>();
        var buildResult = _builder.Build(airArtifact.Program.Instructions);
        diagnostics.AddRange(buildResult.Diagnostics.Select(x => Diagnostic("air.cfg.invalid", x)));
        VerifyReachability(buildResult.Graph, diagnostics);
        VerifyTerminatorShapes(buildResult.Graph, diagnostics);
        var stackResult = _stackAnalyzer.Analyze(buildResult.Graph);
        diagnostics.AddRange(stackResult.Diagnostics.Select(x => Diagnostic("air.stack.invalid", x)));

        return diagnostics.Count == 0 ? IrVerificationResult.Success : new IrVerificationResult(diagnostics);
    }

    private static void VerifyReachability(AirControlFlowGraph graph, List<IrDiagnostic> diagnostics)
    {
        var reachable = ComputeReachable(graph);
        foreach (var block in graph.Blocks)
        {
            if (!reachable.Contains(block.Id))
            {
                diagnostics.Add(Diagnostic(
                    "air.block.unreachable",
                    $"AIR block '{block.Id}' is unreachable from entry block '{graph.EntryBlockId}'."));
            }
        }
    }

    private static void VerifyTerminatorShapes(AirControlFlowGraph graph, List<IrDiagnostic> diagnostics)
    {
        foreach (var block in graph.Blocks)
        {
            var terminator = block.Terminator;
            if (terminator.Kind == AirBlockTerminatorKind.Invalid)
            {
                diagnostics.Add(Diagnostic(
                    "air.terminator.invalid",
                    terminator.Diagnostic ?? $"AIR block '{block.Id}' has an invalid terminator."));
                continue;
            }

            if (terminator.Kind == AirBlockTerminatorKind.Jump && terminator.Successors.Count != 1)
            {
                diagnostics.Add(Diagnostic(
                    "air.terminator.target-count",
                    $"AIR block '{block.Id}' unconditional jump must have exactly one successor."));
            }

            if (terminator.Kind == AirBlockTerminatorKind.Fallthrough && terminator.Successors.Count != 1)
            {
                diagnostics.Add(Diagnostic(
                    "air.terminator.target-count",
                    $"AIR block '{block.Id}' fallthrough must have exactly one successor."));
            }

            if (terminator.Kind == AirBlockTerminatorKind.ConditionalJump && terminator.Successors.Count != 2)
            {
                diagnostics.Add(Diagnostic(
                    "air.terminator.target-count",
                    $"AIR block '{block.Id}' conditional jump must have exactly two successors."));
            }
        }
    }

    private static HashSet<AirBlockId> ComputeReachable(AirControlFlowGraph graph)
    {
        var reachable = new HashSet<AirBlockId>();
        var pending = new Stack<AirBlockId>();
        pending.Push(graph.EntryBlockId);

        while (pending.Count > 0)
        {
            var blockId = pending.Pop();
            if (!reachable.Add(blockId) || !graph.BlocksById.TryGetValue(blockId, out var block))
                continue;

            foreach (var target in block.Terminator.Successors.Select(static x => x.Target).Order())
                pending.Push(target);
        }

        return reachable;
    }

    private static IrDiagnostic Diagnostic(string code, string message) =>
        new(IrDiagnosticSeverity.Error, code, message);
}
