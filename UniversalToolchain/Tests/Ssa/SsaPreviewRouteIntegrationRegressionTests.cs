using IntermediateRepresentationAbstractions;
using System.Reflection;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaPreviewRouteIntegrationRegressionTests
{
    [Test]
    public void Run_WhenBranchConditionFolds_RemovesConditionalJumpAndDeadConditionFromEmittedAir()
    {
        var high = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var merge = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var source = new AbstractIR();
        source.Push(true);
        source.JmpIf(high);
        source.Push(10);
        source.Jmp(merge);
        source.SetLabel(high);
        source.Push(20);
        source.SetLabel(merge);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        var opcodes = result.Program.Instructions.Select(static x => x.UOpCode).ToArray();
        var pushOperands = PushOperands(result.Program).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(opcodes, Does.Not.Contain(UOpCode.JmpIf));
            Assert.That(pushOperands, Does.Not.Contain(true));
            Assert.That(pushOperands, Does.Not.Contain(10));
            Assert.That(pushOperands, Does.Contain(20));
            Assert.That(IsAirStructurallyValid(result.Program), Is.True);
        });
    }

    [Test]
    public void Run_WhenOptimizationFailsAndPolicyIsPrefer_FallsBackToInputWithOptimizationDiagnostics()
    {
        var source = new AbstractIR();
        source.Push(1);
        var route = SsaRouteFactory.CreateRoundtripRoute(ProfileWithFailingOptimizer(SsaRoutePolicy.Prefer));

        var result = route.Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
        });
    }

    [Test]
    public void Run_WhenOptimizationFailsAndPolicyIsRequire_ThrowsOptimizationDiagnostics()
    {
        var source = new AbstractIR();
        source.Push(1);
        var route = SsaRouteFactory.CreateRoundtripRoute(ProfileWithFailingOptimizer(SsaRoutePolicy.Require));

        var exception = Assert.Throws<SsaRouteException>(() => route.Run(source));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
    }

    [Test]
    public void Run_PreviewOptimizerPipelineProducesStructurallyVerifiedSsa()
    {
        var input = BranchArtifact();
        var profile = SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug);

        var result = SsaRouteFactory
            .CreateOptimizer(profile)
            .Run(input, new IrPipelineContext());
        var optimized = result.Artifact.As<SsaArtifact>();
        var verification = new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, profile.SemanticDescriptors)
            .Verify(optimized, new IrPipelineContext());

        Assert.Multiple(() =>
        {
            Assert.That(verification.IsSuccess, Is.True, string.Join("; ", verification.Diagnostics.Select(static x => $"{x.Code}: {x.Message}")));
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WhenPassCreatesInvalidBlockArguments_PipelineRejectsIntermediateOutput()
    {
        var input = ValidTransferArgumentArtifact();
        var pipeline = new SsaOptimizerPipeline(
            [new InvalidTransferArgumentPass()],
            SsaCoreDescriptors.ConstantMaterialization,
            SsaPreviewSemanticDescriptors.ArithmeticInt32);

        var exception = Assert.Throws<SsaOptimizationException>(() => pipeline.Run(input, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.output.invalid"));
    }

    [Test]
    public void Run_PreviewOptimizerPipelineIsIdempotentOnFoldedBranchShape()
    {
        var profile = SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug);
        var pipeline = SsaRouteFactory.CreateOptimizer(profile);
        var once = pipeline.Run(BranchArtifact(), new IrPipelineContext()).Artifact.As<SsaArtifact>();
        var twice = pipeline.Run(once, new IrPipelineContext()).Artifact.As<SsaArtifact>();

        Assert.That(ModuleSignature(twice.Module), Is.EqualTo(ModuleSignature(once.Module)));
    }

    [Test]
    public void DeadPureInstructionEliminationDoesNotMixValueDefinitionsAcrossFunctions()
    {
        var sharedA = new SsaValue(new SsaValueId("%shared"), SsaTypes.Int32);
        var sharedB = new SsaValue(new SsaValueId("%shared"), SsaTypes.Int32);
        var module = new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("keeps.live"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [ConstI32("live.shared", sharedA, 1)],
                            terminator: SsaTerminator.Return([sharedA.Id]))
                    ],
                    returnType: SsaTypes.Int32),
                new SsaFunction(
                    new SsaFunctionId("drops.dead"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [ConstI32("dead.shared", sharedB, 2)],
                            terminator: SsaTerminator.Return())
                    ])
            ]);

        var result = new SsaDeadPureInstructionEliminationPass()
            .Run(new SsaArtifact(module), new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Module.Functions.Single(static x => x.Id.Value == "keeps.live").Blocks.Single().Instructions.Select(static x => x.Id.Value),
                Is.EqualTo(new[] { "live.shared" }));
            Assert.That(result.Module.Functions.Single(static x => x.Id.Value == "drops.dead").Blocks.Single().Instructions, Is.Empty);
        });
    }

    [Test]
    public void Run_PreviewOptimizerKeepsUnusedManagedCallableAsObservableUntilDescriptorIsExplicitlyTrustedByDce()
    {
        var method = typeof(SsaPreviewRouteIntegrationRegressionTests).GetMethod(
            nameof(ManagedIdentity),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.That(
            SsaManagedCallables.TryCreateMethod(method, consumesInstanceReceiver: false, out var callable, out _, out var diagnostic),
            Is.True,
            diagnostic);

        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var callResult = new SsaValue(new SsaValueId("%call.result"), SsaTypes.Int32);
        var artifact = new SsaArtifact(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("managed.call"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions:
                            [
                                ConstI32("input", input, 42),
                                new SsaCall(new SsaOperationId("call.managed"), callable, [input.Id], [callResult])
                            ],
                            terminator: SsaTerminator.Return())
                    ])
            ]));

        var result = SsaRouteFactory
            .CreateOptimizer(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(artifact, new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

        var instructions = result.Module.Functions.Single().Blocks.Single().Instructions;
        Assert.That(instructions.Select(static x => x.Id.Value), Is.EqualTo(new[] { "input", "call.managed" }));
    }

    private static IEnumerable<object?> PushOperands(IAbstractIR program) =>
        program.Instructions
            .Where(static x => x.UOpCode == UOpCode.Push)
            .SelectMany(static x => x.Operands);

    private static bool IsAirStructurallyValid(IAbstractIR program) =>
        new StructuralAirVerifier().Verify(new AirArtifact(program), new IrPipelineContext()).IsSuccess;

    private static SsaRouteProfile ProfileWithFailingOptimizer(SsaRoutePolicy policy) =>
        new(
            policy,
            [
                SsaPreviewArithmeticInt32Pack.Instance,
                FailingOptimizationPack.Instance
            ]);

    private static SsaArtifact BranchArtifact()
    {
        var condition = new SsaValue(new SsaValueId("%condition"), SsaTypes.Bool);
        var live = new SsaValue(new SsaValueId("%live"), SsaTypes.Int32);
        var dead = new SsaValue(new SsaValueId("%dead"), SsaTypes.Int32);
        return new SsaArtifact(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("branch"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [ConstBool("condition", condition, true)],
                            terminator: SsaTerminator.Branch(
                                condition.Id,
                                new SsaBlockId("then"),
                                [],
                                new SsaBlockId("else"),
                                [])),
                        new SsaBlock(
                            new SsaBlockId("then"),
                            instructions: [ConstI32("live", live, 1)],
                            terminator: SsaTerminator.Return([live.Id])),
                        new SsaBlock(
                            new SsaBlockId("else"),
                            instructions: [ConstI32("dead", dead, 2)],
                            terminator: SsaTerminator.Return([dead.Id]))
                    ],
                    returnType: SsaTypes.Int32)
            ]));
    }

    private static SsaArtifact ValidTransferArgumentArtifact()
    {
        var value = new SsaValue(new SsaValueId("%value"), SsaTypes.Int32);
        var exitArgument = new SsaBlockParameter(new SsaValue(new SsaValueId("%exit.arg"), SsaTypes.Int32));
        return new SsaArtifact(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("transfer"),
                    new SsaBlockId("entry"),
                    [
                        new SsaBlock(
                            new SsaBlockId("entry"),
                            instructions: [ConstI32("value", value, 1)],
                            terminator: SsaTerminator.Jump(new SsaBlockId("exit"), [value.Id])),
                        new SsaBlock(
                            new SsaBlockId("exit"),
                            parameters: [exitArgument],
                            terminator: SsaTerminator.Return([exitArgument.Value.Id]))
                    ],
                    returnType: SsaTypes.Int32)
            ]));
    }

    private static SsaOperation ConstI32(string id, SsaValue result, int value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantInt32,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static SsaOperation ConstBool(string id, SsaValue result, bool value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantBool,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static string ModuleSignature(SsaModule module) =>
        string.Join("\n", module.Functions.Select(FunctionSignature));

    private static string FunctionSignature(SsaFunction function) =>
        $"fn {function.Id.Value} entry {function.EntryBlockId.Value} " +
        string.Join(" | ", function.Blocks.Select(BlockSignature));

    private static string BlockSignature(SsaBlock block) =>
        $"block {block.Id.Value}({string.Join(",", block.Parameters.Select(static x => x.Value.Id.Value + ':' + x.Value.Type.Value))}) " +
        string.Join(";", block.Instructions.Select(InstructionSignature)) +
        " -> " + TerminatorSignature(block.Terminator);

    private static string InstructionSignature(ISsaInstruction instruction) =>
        instruction switch
        {
            SsaOperation operation =>
                $"op {operation.Id.Value} {operation.OpId.Value} [{string.Join(",", operation.Operands.Select(static x => x.Value))}] -> [{string.Join(",", operation.Results.Select(static x => x.Id.Value + ':' + x.Type.Value))}] {string.Join(",", operation.Attributes.Values.Select(static x => x.Key + '=' + x.Value))}",
            SsaCall call =>
                $"call {call.Id.Value} {call.Callee.Value} [{string.Join(",", call.Operands.Select(static x => x.Value))}] -> [{string.Join(",", call.Results.Select(static x => x.Id.Value + ':' + x.Type.Value))}]",
            _ => $"unknown {instruction.Id.Value}"
        };

    private static string TerminatorSignature(SsaTerminator? terminator) =>
        terminator is null
            ? "<null>"
            : $"{terminator.Kind} operands=[{string.Join(",", terminator.Operands.Select(static x => x.Value))}] transfers=[{string.Join(";", terminator.Transfers.Select(static x => x.Target.Value + '(' + string.Join(",", x.Arguments.Select(static y => y.Value)) + ')'))}]";

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
    private static int ManagedIdentity(int value) => value;

    private sealed class FailingOptimizationPack : ISsaSemanticExtensionPack
    {
        public static FailingOptimizationPack Instance { get; } = new();

        public string Id => "FailingOptimization";

        public SemanticDescriptorSet SemanticDescriptors => SemanticDescriptorSet.Empty;

        public AirIntrinsicDescriptorSet AirIntrinsics => AirIntrinsicDescriptorSet.Empty;

        public IAirIntrinsicDescriptorResolver AirIntrinsicResolver => AirIntrinsicDescriptorSet.Empty;

        public IReadOnlyDictionary<string, CallableId> AirIntrinsicCallables { get; } =
            new Dictionary<string, CallableId>(StringComparer.Ordinal);

        public SsaCallableLoweringTargetSet AirLoweringTargets => SsaCallableLoweringTargetSet.Empty;

        public bool EnablesManagedCallables => false;

        public IReadOnlyList<IIrOptimizationPass> CreateOptimizationPasses() => [new InvalidReturnOperandPass()];
    }

    private sealed class InvalidReturnOperandPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("ssa.test.invalid-return-operand");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = IrStageContract.Empty;

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            var artifact = input.As<SsaArtifact>();
            var invalid = new SsaModule(
                artifact.Module.Id,
                artifact.Module.Functions.Select(static function => new SsaFunction(
                    function.Id,
                    function.EntryBlockId,
                    function.Blocks.Select(static block => block.Id == function.EntryBlockId
                        ? new SsaBlock(
                            block.Id,
                            block.Parameters,
                            instructions: block.Instructions,
                            terminator: SsaTerminator.Return([new SsaValueId("%missing.after.pass")]))
                        : block),
                    function.Parameters,
                    function.ReturnType)));

            return new IrStageResult(new SsaArtifact(invalid));
        }
    }

    private sealed class InvalidTransferArgumentPass : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("ssa.test.invalid-transfer-argument");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = IrStageContract.Empty;

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context)
        {
            var artifact = input.As<SsaArtifact>();
            var invalid = new SsaModule(
                artifact.Module.Id,
                artifact.Module.Functions.Select(static function => new SsaFunction(
                    function.Id,
                    function.EntryBlockId,
                    function.Blocks.Select(static block => block.Id.Value == "entry"
                        ? new SsaBlock(
                            block.Id,
                            block.Parameters,
                            instructions: block.Instructions,
                            terminator: SsaTerminator.Jump(new SsaBlockId("exit"), []))
                        : block),
                    function.Parameters,
                    function.ReturnType)));

            return new IrStageResult(new SsaArtifact(invalid));
        }
    }
}
