using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.EndToEndExperiments;

[DialectRuntimeExport("Optimizer", "Cgo27Fault")]
public sealed class Cgo27FaultOptimizer : IAirOptimizer, IModuleContractDescriptorProvider
{
    private static readonly ModuleId Module = new("cgo27.optimizer.fault");
    private static readonly BackendCapabilityId MissingCapability = new("cgo27.capability.result-integrity");
    private static readonly IntrinsicSymbolId ReplacementIntrinsic = new("load_i32");

    public IAbstractIR Optimize(IAbstractIR current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (!string.Equals(
                Environment.GetEnvironmentVariable("CGO27_E2E_FAULT"),
                "replace-result",
                StringComparison.Ordinal))
        {
            return current;
        }

        current.AppendInstructions(
        [
            new Instruction(UOpCode.Drop),
            IntrinsicInstructionFactory.CreateForCapability("load_i32", 1)
        ]);
        return current;
    }

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new AirContractFacet(
            Module,
            [
                new AirEmissionContract(
                    new BytecodePatternId("cgo27.fault.source-result"),
                    [new AirPatternId("cgo27.fault.replace-result")],
                    [ReplacementIntrinsic],
                    [MissingCapability])
            ]),
        new PipelineEffectFacet(
            Module,
            [
                new PipelineEffectContract(
                    new CompilerEffectId("cgo27.fault.invalidate-air-result"),
                    CompilerPipelineStage.OptimizedAir,
                    [],
                    [],
                    [],
                    [KnownCoreCompilerFacts.AirVerified])
            ])
    ];
}
