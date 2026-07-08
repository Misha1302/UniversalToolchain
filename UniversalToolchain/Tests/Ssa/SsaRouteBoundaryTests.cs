using IntermediateRepresentationAbstractions;
using System.Reflection;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRouteBoundaryTests
{
    [Test]
    public void Route_WhenConstantBranchFolds_EliminatesConditionalJumpAndDeadConditionPush()
    {
        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(CreateConstantTrueBranchAir());
        var instructions = result.Program.Instructions;

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(instructions.Select(static x => x.UOpCode), Does.Not.Contain(UOpCode.JmpIf));
            Assert.That(instructions.Select(static x => x.UOpCode), Does.Not.Contain(UOpCode.JmpIfNot));
            Assert.That(PushOperands(instructions), Is.EqualTo(new object[] { 1 }));
        });
    }

    [Test]
    public void Route_WhenManagedCallResultIsUsed_DoesNotDeleteManagedCallableDuringOptimization()
    {
        var method = typeof(SsaRouteBoundaryTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!;
        var source = new AbstractIR();
        source.Push(41);
        source.AppendInstructions(
        [
            new Instruction(UOpCode.Intrinsic, [AirIntrinsicIds.CallCSharp, method])
        ]);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);
        var intrinsic = result.Program.Instructions.Single(static x => x.UOpCode == UOpCode.Intrinsic);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(intrinsic.Operands, Is.EqualTo(new object[] { AirIntrinsicIds.CallCSharp, method }));
        });
    }

    [Test]
    public void Route_WhenOptimizerFailsAndPolicyIsPrefer_FallsBackToOriginalAirWithDiagnostics()
    {
        var source = CreateArithmeticAir();
        var result = SsaRouteFactory
            .CreateRoundtripRoute(CreateInvalidatingProfile(SsaRoutePolicy.Prefer))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.value.undefined"));
        });
    }

    [Test]
    public void Route_WhenOptimizerFailsAndPolicyIsRequire_ThrowsDiagnosticException()
    {
        var exception = Assert.Throws<SsaRouteException>(() =>
            SsaRouteFactory
                .CreateRoundtripRoute(CreateInvalidatingProfile(SsaRoutePolicy.Require))
                .Run(CreateArithmeticAir()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
            Assert.That(exception.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.value.undefined"));
        });
    }

    [Test]
    public void OptimizerPipeline_WhenPassProducesInvalidSsa_ThrowsAfterPassVerificationDiagnostic()
    {
        var exception = Assert.Throws<SsaOptimizationException>(() =>
            new SsaOptimizerPipeline(
                    [new InvalidatingOptimizationPass()],
                    SsaCoreDescriptors.ConstantMaterialization,
                    SsaPreviewSemanticDescriptors.ArithmeticInt32)
                .Run(new SsaArtifact(CreatePreviewAddModule()), new IrPipelineContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
            Assert.That(exception.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.value.undefined"));
        });
    }

    [Test]
    public void OptimizerPipeline_WhenRunningPreviewPipelineTwice_IsIdempotent()
    {
        var optimizer = SsaRouteFactory.CreateOptimizer(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Require));
        var first = optimizer
            .Run(new SsaArtifact(CreatePreviewAddModule()), new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>()
            .Module;
        var second = optimizer
            .Run(new SsaArtifact(first), new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>()
            .Module;

        Assert.That(Serialize(first), Is.EqualTo(Serialize(second)));
    }

    [Test]
    public void StructuralVerifier_WhenBlockArgumentCountMismatches_ReportsDiagnostic()
    {
        var value = new SsaValue(new SsaValueId("value"), SsaTypes.Int32);
        var targetParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("target.arg"), SsaTypes.Int32));
        var module = ModuleWith(
            new SsaBlock(
                new SsaBlockId("entry"),
                instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("value.op"), value, 1)],
                terminator: SsaTerminator.Jump(new SsaBlockId("target"), [])),
            new SsaBlock(
                new SsaBlockId("target"),
                parameters: [targetParameter],
                terminator: SsaTerminator.Return([targetParameter.Value.Id])));

        var verification = Verify(module);

        Assert.That(verification.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.block-argument.count"));
    }

    [Test]
    public void StructuralVerifier_WhenBlockArgumentTypeMismatches_ReportsDiagnostic()
    {
        var condition = new SsaValue(new SsaValueId("condition"), SsaTypes.Bool);
        var targetParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("target.arg"), SsaTypes.Int32));
        var module = ModuleWith(
            new SsaBlock(
                new SsaBlockId("entry"),
                instructions: [SsaConstantMaterializer.Bool(new SsaOperationId("condition.op"), condition, true)],
                terminator: SsaTerminator.Jump(new SsaBlockId("target"), [condition.Id])),
            new SsaBlock(
                new SsaBlockId("target"),
                parameters: [targetParameter],
                terminator: SsaTerminator.Return([targetParameter.Value.Id])));

        var verification = Verify(module);

        Assert.That(verification.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.block-argument.type"));
    }

    [Test]
    public void DeadPureInstructionElimination_WhenDifferentFunctionsReuseValueIds_DoesNotLeakUseDefAcrossFunctions()
    {
        var sharedA = new SsaValue(new SsaValueId("shared"), SsaTypes.Int32);
        var sharedB = new SsaValue(new SsaValueId("shared"), SsaTypes.Int32);
        var module = new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("live.function"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("live.shared"), sharedA, 1)],
                            terminator: SsaTerminator.Return([sharedA.Id]))
                    ],
                    returnType: SsaTypes.Int32),
                new SsaFunction(
                    new SsaFunctionId("dead.function"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [SsaConstantMaterializer.Int32(new SsaOperationId("dead.shared"), sharedB, 2)],
                            terminator: SsaTerminator.Return())
                    ])
            ]);

        var result = new SsaDeadPureInstructionEliminationPass()
            .Run(new SsaArtifact(module), new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>()
            .Module;

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Functions.Single(static x => x.Id.Value == "live.function").Blocks.Single().Instructions.Select(static x => x.Id.Value),
                Is.EqualTo(new[] { "live.shared" }));
            Assert.That(
                result.Functions.Single(static x => x.Id.Value == "dead.function").Blocks.Single().Instructions,
                Is.Empty);
        });
    }

    private static AbstractIR CreateArithmeticAir()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);
        return source;
    }

    private static AbstractIR CreateConstantTrueBranchAir()
    {
        var high = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var merge = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var source = new AbstractIR();
        source.Push(true);
        source.JmpIf(high);
        source.Push(2);
        source.Jmp(merge);
        source.SetLabel(high);
        source.Push(1);
        source.SetLabel(merge);
        return source;
    }

    private static SsaModule CreatePreviewAddModule()
    {
        var left = new SsaValue(new SsaValueId("left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("result"), SsaTypes.Int32);
        return ModuleWith(
            new SsaBlock(
                new SsaBlockId("entry"),
                instructions:
                [
                    SsaConstantMaterializer.Int32(new SsaOperationId("left.op"), left, 2),
                    SsaConstantMaterializer.Int32(new SsaOperationId("right.op"), right, 3),
                    new SsaCall(new SsaOperationId("call.add"), SsaPreviewCallables.AddInt32Unchecked, [left.Id, right.Id], [result])
                ],
                terminator: SsaTerminator.Return([result.Id])));
    }

    private static SsaRouteProfile CreateInvalidatingProfile(SsaRoutePolicy policy) =>
        SsaRouteProfileBuilder
            .Create(policy)
            .AddPack(SsaPreviewArithmeticInt32Pack.Instance)
            .AddPack(InvalidatingOptimizationPack.Instance)
            .Build();

    private static IrVerificationResult Verify(SsaModule module) =>
        new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32)
            .Verify(new SsaArtifact(module), new IrPipelineContext());

    private static SsaModule ModuleWith(params SsaBlock[] blocks) =>
        new(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("test.function"),
                    new SsaBlockId("entry"),
                    blocks,
                    returnType: SsaTypes.Int32)
            ]);

    private static IReadOnlyList<object> PushOperands(IReadOnlyList<Instruction> instructions) =>
        instructions
            .Where(static x => x.UOpCode == UOpCode.Push)
            .Select(static x => x.Operands.Single())
            .ToArray();

    private static string Serialize(SsaModule module) =>
        string.Join("|", module.Functions.Select(SerializeFunction));

    private static string SerializeFunction(SsaFunction function) =>
        $"{function.Id}:{string.Join(",", function.Blocks.Select(SerializeBlock))}";

    private static string SerializeBlock(SsaBlock block) =>
        $"{block.Id}[{string.Join(",", block.Parameters.Select(static x => x.Value.Id.Value))}]" +
        $"<{string.Join(",", block.Instructions.Select(SerializeInstruction))}>" +
        $"=>{SerializeTerminator(block.Terminator)}";

    private static string SerializeInstruction(ISsaInstruction instruction) =>
        instruction switch
        {
            SsaOperation operation =>
                $"op:{operation.Id}:{operation.OpId}:{string.Join(",", operation.Operands.Select(static x => x.Value))}->{string.Join(",", operation.Results.Select(static x => x.Id.Value))}",
            SsaCall call =>
                $"call:{call.Id}:{call.Callee}:{string.Join(",", call.Operands.Select(static x => x.Value))}->{string.Join(",", call.Results.Select(static x => x.Id.Value))}",
            _ => $"unknown:{instruction.Id}"
        };

    private static string SerializeTerminator(SsaTerminator? terminator) =>
        terminator is null
            ? "<null>"
            : $"{terminator.Kind}:{string.Join(",", terminator.Operands.Select(static x => x.Value))}:{string.Join(",", terminator.Transfers.Select(SerializeTransfer))}";

    private static string SerializeTransfer(SsaBlockTransfer transfer) =>
        $"{transfer.Target}({string.Join(",", transfer.Arguments.Select(static x => x.Value))})";

    private static int AddOne(int value) => value + 1;

    private sealed class InvalidatingOptimizationPack : ISsaSemanticExtensionPack
    {
        public static InvalidatingOptimizationPack Instance { get; } = new();

        public string Id => "test.invalidating-optimizer";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => [new InvalidatingOptimizationPass()];
    }

    private sealed class InvalidatingOptimizationPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("test.ssa.invalidating-pass");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = new(
            requiresFacts: [SsaFacts.StructuralVerification],
            preservesFacts: [SsaFacts.StructuralVerification]);

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            var entry = new SsaBlock(
                new SsaBlockId("entry"),
                terminator: SsaTerminator.Return([new SsaValueId("undefined")]));
            var function = new SsaFunction(
                new SsaFunctionId("invalid.function"),
                entry.Id,
                [entry],
                returnType: SsaTypes.Int32);
            return new IrStageResult(new SsaArtifact(new SsaModule(new SsaModuleId("invalid.module"), [function])));
        }
    }
}
