using System.Globalization;
using System.Reflection;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaOptimizationTests
{
    [Test]
    public void Run_WithConstantBinaryInt32Callable_FoldsToConstant()
    {
        var artifact = CreateConstantBinaryArtifact(SsaPreviewCallables.AddInt32Unchecked, 2, 3);

        var result = RunConstantFolding(artifact);
        var operations = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single().Operations;

        Assert.Multiple(() =>
        {
            Assert.That(operations[2].OpId, Is.EqualTo(SsaOperations.ConstantInt32));
            Assert.That(operations[2].Operands, Is.Empty);
            Assert.That(operations[2].Results.Single().Id, Is.EqualTo(new SsaValueId("%result")));
            Assert.That(ReadConstant(operations[2]), Is.EqualTo("5"));
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
            Assert.That(result.Facts.Contains(SsaOptimizationFacts.StructurallyVerifiedSsa), Is.True);
            Assert.That(result.Facts.Contains(SsaOptimizationFacts.LocallyConstantFolded), Is.True);
        });
    }

    [Test]
    public void Run_WithConstantEqualCallable_FoldsToBoolConstant()
    {
        var artifact = CreateConstantBinaryArtifact(SsaPreviewCallables.EqualInt32, 7, 7, SsaTypes.Bool);

        var result = RunConstantFolding(artifact);
        var folded = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single().Operations[2];

        Assert.Multiple(() =>
        {
            Assert.That(folded.OpId, Is.EqualTo(SsaOperations.ConstantBool));
            Assert.That(folded.Results.Single().Type, Is.EqualTo(SsaTypes.Bool));
            Assert.That(ReadConstant(folded), Is.EqualTo("True"));
        });
    }

    [Test]
    public void Run_WithOverflowingConstantBinaryInt32Callable_UsesUncheckedInt32Semantics()
    {
        var artifact = CreateConstantBinaryArtifact(SsaPreviewCallables.AddInt32Unchecked, int.MaxValue, 1);

        var result = RunConstantFolding(artifact);
        var folded = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single().Operations[2];

        Assert.Multiple(() =>
        {
            Assert.That(folded.OpId, Is.EqualTo(SsaOperations.ConstantInt32));
            Assert.That(ReadConstant(folded), Is.EqualTo(int.MinValue.ToString()));
        });
    }

    [Test]
    public void Run_WhenOperandIsNotConstant_LeavesCallUnchanged()
    {
        var parameter = new SsaBlockParameter(new SsaValue(new SsaValueId("%p"), SsaTypes.Int32));
        var constant = new SsaValue(new SsaValueId("%c"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("c", constant, 4),
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [parameter.Value.Id, constant.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            parameters: [parameter],
            returnType: SsaTypes.Int32));

        var optimized = RunConstantFolding(artifact).Artifact.As<SsaArtifact>();
        var add = optimized.Module.Functions.Single().Blocks.Single().Calls.Single();

        Assert.Multiple(() =>
        {
            Assert.That(add.Callee, Is.EqualTo(SsaPreviewCallables.AddInt32Unchecked));
            Assert.That(add.Operands, Is.EqualTo(new[] { parameter.Value.Id, constant.Id }));
        });
    }

    [Test]
    public void Run_WhenInputSsaIsInvalid_ThrowsBeforeRunningPass()
    {
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [new SsaValueId("%missing"), new SsaValueId("%missing2")],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));

        var exception = Assert.Throws<SsaOptimizationException>(() => RunConstantFolding(artifact));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.optimization.input.invalid"));
        Assert.That(exception.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.value.undefined"));
    }

    [Test]
    public void Run_AppliesDeclaredFactEffectsAroundPassResult()
    {
        var produced = new FactId("ssa.test.produced");
        var preserved = new FactId("ssa.test.preserved");
        var invalidated = new FactId("ssa.test.invalidated");
        var artifact = CreateConstantBinaryArtifact(SsaPreviewCallables.AddInt32Unchecked, 1, 2);
        var context = new IrPipelineContext(
            facts: new IrFactSet([preserved, invalidated]));

        var result = new SsaOptimizerPipeline(
            [new FactEffectProbePass(produced, preserved, invalidated)],
            SsaCoreDescriptors.ConstantMaterialization,
            SsaPreviewSemanticDescriptors.ArithmeticInt32).Run(artifact, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Facts.Contains(produced), Is.True);
            Assert.That(result.Facts.Contains(preserved), Is.True);
            Assert.That(result.Facts.Contains(invalidated), Is.False);
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_IgnoresFactsReturnedOutsideDeclaredContract()
    {
        var undeclared = new FactId("ssa.test.undeclared");
        var artifact = CreateConstantBinaryArtifact(SsaPreviewCallables.AddInt32Unchecked, 1, 2);

        var result = new SsaOptimizerPipeline(
            [new UndeclaredFactProbePass(undeclared)],
            SsaCoreDescriptors.ConstantMaterialization,
            SsaPreviewSemanticDescriptors.ArithmeticInt32).Run(artifact, new IrPipelineContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Facts.Contains(undeclared), Is.False);
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WithCallableInstruction_PreservesInstructionOrder()
    {
        var input = new SsaBlockParameter(new SsaValue(new SsaValueId("%input"), SsaTypes.Int32));
        var callResult = new SsaValue(new SsaValueId("%call"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        new SsaCall(
                            new SsaOperationId("call.identity"),
                            TestCallables.IdentityInt32,
                            [input.Value.Id],
                            [callResult])
                    ],
                    terminator: SsaTerminator.Return([callResult.Id]))
            ],
            parameters: [input],
            returnType: SsaTypes.Int32));

        var result = new SsaOptimizerPipeline(
                [new SsaConstantFoldingPass()],
                SsaDescriptorSet.Empty,
                TestSemanticDescriptors)
            .Run(artifact, new IrPipelineContext());

        var block = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(block.Instructions, Has.Count.EqualTo(1));
            Assert.That(block.Instructions.Single(), Is.TypeOf<SsaCall>());
            Assert.That(block.Calls.Single().Callee, Is.EqualTo(TestCallables.IdentityInt32));
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WithConstantCallableInstruction_FoldsThroughDescriptorEvaluator()
    {
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var resultValue = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, 40),
                        ConstI32("right", right, 2),
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [left.Id, right.Id],
                            [resultValue])
                    ],
                    terminator: SsaTerminator.Return([resultValue.Id]))
            ],
            returnType: SsaTypes.Int32));

        var optimized = RunPreviewConstantFolding(artifact).Artifact.As<SsaArtifact>();
        var block = optimized.Module.Functions.Single().Blocks.Single();
        var folded = block.Instructions[2];

        Assert.Multiple(() =>
        {
            Assert.That(folded, Is.TypeOf<SsaOperation>());
            Assert.That(((SsaOperation)folded).OpId, Is.EqualTo(SsaOperations.ConstantInt32));
            Assert.That(ReadConstant((SsaOperation)folded), Is.EqualTo("42"));
            Assert.That(block.Calls, Is.Empty);
        });
    }

    [Test]
    public void Run_WhenCallableDescriptorIsUntrusted_DoesNotFold()
    {
        var untrustedCallable = new CallableId("plugin.untrusted.identity");
        var semanticDescriptors = new SemanticDescriptorSet(
            types: [new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32)],
            callables:
            [
                new CallableDescriptor(
                    untrustedCallable,
                    new CallableSignature(
                        [SsaPreviewSemanticTypes.Int32],
                        [SsaPreviewSemanticTypes.Int32]),
                    effects: SemanticEffectSummary.Pure,
                    determinism: Determinism.Deterministic,
                    trustLevel: SemanticTrustLevel.UserProvidedUnchecked)
            ]);
        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var resultValue = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 1),
                        new SsaCall(
                            new SsaOperationId("call.untrusted"),
                            untrustedCallable,
                            [input.Id],
                            [resultValue])
                    ],
                    terminator: SsaTerminator.Return([resultValue.Id]))
            ],
            returnType: SsaTypes.Int32));

        var result = new SsaOptimizerPipeline(
                [new SsaConstantFoldingPass(semanticDescriptors, new AlwaysReturnsInt32Evaluator())],
                SsaCoreDescriptors.ConstantMaterialization,
                semanticDescriptors)
            .Run(artifact, new IrPipelineContext());

        var block = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(block.Instructions[1], Is.TypeOf<SsaCall>());
            Assert.That(block.Calls.Single().Callee, Is.EqualTo(untrustedCallable));
            Assert.That(result.Facts.Contains(SsaFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WithTrustedPureManagedCallable_FoldsThroughCallableDescriptor()
    {
        var method = typeof(SsaOptimizationTests).GetMethod(
            nameof(TrustedManagedAdd),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.That(
            SsaManagedCallables.TryCreateMethod(
                method,
                consumesInstanceReceiver: false,
                out var callable,
                out var descriptor,
                out var diagnostic),
            Is.True,
            diagnostic);

        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var resultValue = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, 10),
                        ConstI32("right", right, 32),
                        new SsaCall(
                            new SsaOperationId("call.managed.add"),
                            callable,
                            [left.Id, right.Id],
                            [resultValue])
                    ],
                    terminator: SsaTerminator.Return([resultValue.Id]))
            ],
            returnType: SsaTypes.Int32));
        var semanticDescriptors = new SemanticDescriptorSet(
            types: [new SemanticTypeDescriptor(SsaPreviewSemanticTypes.Int32)],
            callables: [descriptor]);

        var result = new SsaOptimizerPipeline(
                [new SsaConstantFoldingPass(semanticDescriptors, new ManagedAddEvaluator(callable))],
                SsaCoreDescriptors.ConstantMaterialization,
                semanticDescriptors)
            .Run(artifact, new IrPipelineContext());

        var folded = result.Artifact.As<SsaArtifact>().Module.Functions.Single().Blocks.Single().Instructions[2];

        Assert.Multiple(() =>
        {
            Assert.That(folded, Is.TypeOf<SsaOperation>());
            Assert.That(((SsaOperation)folded).OpId, Is.EqualTo(SsaOperations.ConstantInt32));
            Assert.That(ReadConstant((SsaOperation)folded), Is.EqualTo("42"));
            Assert.That(result.Facts.Contains(SsaOptimizationFacts.LocallyConstantFolded), Is.True);
        });
    }

    private static IrStageResult RunConstantFolding(SsaArtifact artifact) =>
        new SsaOptimizerPipeline(
                [PreviewConstantFoldingPass()],
                SsaCoreDescriptors.ConstantMaterialization,
                SsaPreviewSemanticDescriptors.ArithmeticInt32)
            .Run(artifact, new IrPipelineContext());

    private static IrStageResult RunPreviewConstantFolding(SsaArtifact artifact) =>
        new SsaOptimizerPipeline(
                [PreviewConstantFoldingPass()],
                SsaCoreDescriptors.ConstantMaterialization,
                SsaPreviewSemanticDescriptors.ArithmeticInt32)
            .Run(artifact, new IrPipelineContext());

    private static SsaConstantFoldingPass PreviewConstantFoldingPass() =>
        new(SsaPreviewSemanticDescriptors.ArithmeticInt32, new SsaPreviewInt32ConstantEvaluator());

    private static SsaArtifact CreateConstantBinaryArtifact(CallableId callable, int leftValue, int rightValue, SsaTypeId? resultType = null)
    {
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), resultType ?? SsaTypes.Int32);

        return Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, leftValue),
                        ConstI32("right", right, rightValue),
                        new SsaCall(new SsaOperationId("fold"), callable, [left.Id, right.Id], [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: result.Type));
    }

    private static SsaOperation ConstI32(string id, SsaValue result, int value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantInt32,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static string ReadConstant(SsaOperation operation)
    {
        Assert.That(operation.Attributes.TryGet(SsaAttributeKeys.ConstantValue, out var attribute), Is.True);
        return attribute.Value;
    }

    private static SsaArtifact Artifact(SsaFunction function) =>
        new(new SsaModule(new SsaModuleId("test.module"), [function]));

    private static class TestCallables
    {
        public static CallableId IdentityInt32 { get; } = new("test.core.i32.identity");
    }

    private static SemanticDescriptorSet TestSemanticDescriptors { get; } = new(
        types:
        [
            new SemanticTypeDescriptor(new SemanticTypeId(SsaTypes.Int32.Value))
        ],
        callables:
        [
            new CallableDescriptor(
                TestCallables.IdentityInt32,
                new CallableSignature(
                    [new SemanticTypeId(SsaTypes.Int32.Value)],
                    [new SemanticTypeId(SsaTypes.Int32.Value)]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.BuiltInTrusted)
        ]);

    private sealed class FactEffectProbePass(FactId produced, FactId preserved, FactId invalidated) : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("ssa.test.fact-effect-probe");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = new(
            producesFacts: [produced],
            preservesFacts: [preserved],
            invalidatesFacts: [invalidated]);

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            new(input.As<SsaArtifact>(), IrFactSet.Empty);
    }

    private sealed class UndeclaredFactProbePass(FactId undeclared) : IIrOptimizationPass
    {
        public IrStageId Id { get; } = new("ssa.test.undeclared-fact-probe");

        public IrKind InputKind => SsaIrKinds.Ssa;

        public IrKind OutputKind => SsaIrKinds.Ssa;

        public IrStageContract Contract { get; } = IrStageContract.Empty;

        public IrStageResult Run(IIrArtifact input, IrPipelineContext context) =>
            new(input.As<SsaArtifact>(), new IrFactSet([undeclared]));
    }

    private sealed class AlwaysReturnsInt32Evaluator : IConstantEvaluator
    {
        public bool TryEvaluate(
            CallableDescriptor descriptor,
            IReadOnlyList<ConstantValue> arguments,
            out ConstantValue result)
        {
            result = new ConstantValue(SsaPreviewSemanticTypes.Int32, "999");
            return true;
        }
    }

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        AlgebraicTraits = AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
        TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
    private static int TrustedManagedAdd(int left, int right) => left + right;

    private sealed class ManagedAddEvaluator(CallableId callable) : IConstantEvaluator
    {
        public bool TryEvaluate(
            CallableDescriptor descriptor,
            IReadOnlyList<ConstantValue> arguments,
            out ConstantValue result)
        {
            result = default!;
            if (descriptor.Id != callable ||
                arguments.Count != 2 ||
                arguments[0].Type != SsaPreviewSemanticTypes.Int32 ||
                arguments[1].Type != SsaPreviewSemanticTypes.Int32 ||
                !int.TryParse(arguments[0].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var left) ||
                !int.TryParse(arguments[1].CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var right))
            {
                return false;
            }

            result = new ConstantValue(
                SsaPreviewSemanticTypes.Int32,
                TrustedManagedAdd(left, right).ToString(CultureInfo.InvariantCulture));
            return true;
        }
    }
}
