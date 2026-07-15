using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaToAirValueReusePreflightTests
{
    [Test]
    public void Run_WhenValueIsConsumedByCallAndReturn_ReportsBothStackSites()
    {
        var left = Value("left");
        var right = Value("right");
        var sum = Value("sum");
        var one = Value("one");
        var product = Value("product");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("add", SsaCallables.AddInt32Unchecked, [left.Id, right.Id], sum),
                Const("one", one, 1),
                Call("multiply", SsaCallables.MultiplyInt32Unchecked, [sum.Id, one.Id], product)
            ],
            terminator: SsaTerminator.Return([sum.Id])));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            AlphaEmitter().Run(artifact, new IrPipelineContext()));
        var diagnostic = exception!.Diagnostics.Single(diagnostic =>
            diagnostic.Code == "ssa.to-air.value-reuse.unsupported");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message, Does.Contain("%sum"));
            Assert.That(diagnostic.Message, Does.Contain("call 'multiply' operand 0"));
            Assert.That(diagnostic.Message, Does.Contain("return operand 0"));
            Assert.That(
                exception.Diagnostics.Select(static item => item.Code),
                Does.Not.Contain("ssa.to-air.stack-shape.unsupported"));
        });
    }

    [Test]
    public void Run_WhenStackOrderIsWrongWithoutValueReuse_PreservesGenericStackDiagnostic()
    {
        var left = Value("left");
        var right = Value("right");
        var result = Value("result");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("left", left, 2),
                Const("right", right, 3),
                Call("subtract", SsaCallables.SubtractInt32Unchecked, [right.Id, left.Id], result)
            ],
            terminator: SsaTerminator.Return([result.Id])));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            AlphaEmitter().Run(artifact, new IrPipelineContext()));

        Assert.Multiple(() =>
        {
            Assert.That(
                exception!.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain("ssa.to-air.stack-shape.unsupported"));
            Assert.That(
                exception.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Not.Contain("ssa.to-air.value-reuse.unsupported"));
        });
    }

    [Test]
    public void Run_WhenRepeatedOperandsHaveNoLoweringTarget_PreservesLoweringDiagnosticOwnership()
    {
        var callable = new CallableId("test.no-air-lowering");
        var descriptors = new SemanticDescriptorSet(
            types:
            [
                new SemanticTypeDescriptor(
                    SsaSemanticTypes.Int32,
                    SemanticTypeTraits.Numeric |
                    SemanticTypeTraits.ValueObject |
                    SemanticTypeTraits.Immutable)
            ],
            callables:
            [
                new CallableDescriptor(
                    callable,
                    new CallableSignature(
                        [SsaSemanticTypes.Int32, SsaSemanticTypes.Int32],
                        [SsaSemanticTypes.Int32]),
                    effects: SemanticEffectSummary.Pure,
                    determinism: Determinism.Deterministic,
                    trustLevel: SemanticTrustLevel.BuiltInTrusted)
            ]);
        var input = Value("input");
        var result = Value("result");
        var artifact = Artifact(new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                Const("input", input, 2),
                Call("unsupported", callable, [input.Id, input.Id], result)
            ],
            terminator: SsaTerminator.Return([result.Id])));
        var planner = new SsaCallableLoweringPlanner(
            descriptors,
            SsaCallableLoweringTargetSet.Empty,
            AirCoreIntrinsicDescriptors.ArithmeticInt32);
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, descriptors),
            new StructuralAirVerifier(),
            planner);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.Multiple(() =>
        {
            Assert.That(
                exception!.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain("ssa.to-air.call-lowering.missing"));
            Assert.That(
                exception.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Not.Contain("ssa.to-air.value-reuse.unsupported"));
        });
    }

    private static SsaToAirConverter AlphaEmitter() =>
        new(
            new StructuralSsaVerifier(
                SsaCoreDescriptors.ConstantMaterialization,
                SsaSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            SsaAirIntrinsicLowerings.ArithmeticInt32,
            AirCoreIntrinsicDescriptors.ArithmeticInt32);

    private static SsaArtifact Artifact(params SsaBlock[] blocks) =>
        new(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("main"),
                    new SsaBlockId("entry"),
                    blocks,
                    returnType: SsaTypes.Int32)
            ]));

    private static SsaValue Value(string id) =>
        new(new SsaValueId($"%{id}"), SsaTypes.Int32);

    private static SsaOperation Const(
        string id,
        SsaValue result,
        int value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantInt32,
            results: [result],
            attributes: new SsaAttributeBag(
            [
                new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())
            ]));

    private static SsaCall Call(
        string id,
        CallableId callable,
        IEnumerable<SsaValueId> operands,
        SsaValue result) =>
        new(new SsaOperationId(id), callable, operands, [result]);
}
