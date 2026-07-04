using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Lowering;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Runs the verifier-gated preview SSA optimization route as an opt-in dialect optimizer.
/// </summary>
[DialectOptimizerAlias("SsaConstantFolding", "SsaPreviewOptimization")]
[DialectRuntimeExport("Optimizer", "Ssa")]
public sealed class SsaPreviewOptimizerModule : IIRProcessingModule
{
    public IAbstractIR ProcessIr<TCompilationOutput>(
        IAbstractIR current,
        IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(compiler);

        var lowering = new AirToSsaConverter();
        var optimizer = new SsaOptimizerPipeline(
            [new SsaConstantFoldingPass()],
            SsaCoreDescriptors.ConstantMaterialization,
            SsaPreviewSemanticDescriptors.ArithmeticInt32);
        var emission = new SsaToAirConverter();

        var loweringResult = lowering.Run(new AirArtifact(current), new IrPipelineContext());
        var ssaArtifact = loweringResult.Artifact.As<SsaArtifact>();
        var optimizationResult = optimizer.Run(
            ssaArtifact,
            new IrPipelineContext(facts: loweringResult.Facts));
        var emissionResult = emission.Run(
            optimizationResult.Artifact,
            new IrPipelineContext(facts: optimizationResult.Facts));

        return emissionResult.Artifact.As<AirArtifact>().Program;
    }
}
