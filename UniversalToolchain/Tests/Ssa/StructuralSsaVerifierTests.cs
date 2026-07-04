using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;

namespace Tests.Ssa;

[TestFixture]
public sealed class StructuralSsaVerifierTests
{
    private static readonly IrPipelineContext Context = new();

    [Test]
    public void Verify_WhenProgramIsValid_ReturnsSuccess()
    {
        var result = Verify(CreateValidBranchArtifact());

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void Verify_WhenBranchConditionIsNotBool_ReturnsDiagnostic()
    {
        var condition = new SsaBlockParameter(new SsaValue(new SsaValueId("%condition"), SsaTypes.Int32));
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    terminator: SsaTerminator.Branch(
                        condition.Value.Id,
                        new SsaBlockId("then"),
                        [],
                        new SsaBlockId("else"),
                        [])),
                new SsaBlock(new SsaBlockId("then"), terminator: SsaTerminator.Return()),
                new SsaBlock(new SsaBlockId("else"), terminator: SsaTerminator.Return())
            ],
            parameters: [condition]));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.branch.condition-type");
    }

    [Test]
    public void Verify_WhenEntryBlockIsMissing_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("missing"),
            [new SsaBlock(new SsaBlockId("entry"), terminator: SsaTerminator.Return())]));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.entry.missing");
    }

    [Test]
    public void Verify_WhenValueIsDefinedTwice_ReturnsDiagnostic()
    {
        var duplicate = new SsaValue(new SsaValueId("%x"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        new SsaOperation(new SsaOperationId("op0"), TestOperations.Int32Binary, [new SsaValueId("%a"), new SsaValueId("%b")], [duplicate]),
                        new SsaOperation(new SsaOperationId("op1"), TestOperations.Int32Binary, [new SsaValueId("%a"), new SsaValueId("%b")], [duplicate])
                    ],
                    terminator: SsaTerminator.Return([duplicate.Id]))
            ],
            parameters:
            [
                new SsaBlockParameter(new SsaValue(new SsaValueId("%a"), SsaTypes.Int32)),
                new SsaBlockParameter(new SsaValue(new SsaValueId("%b"), SsaTypes.Int32))
            ],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.value.duplicate");
    }

    [Test]
    public void Verify_WhenOperationUsesUndefinedValue_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        new SsaOperation(
                            new SsaOperationId("op0"),
                            TestOperations.Int32Binary,
                            [new SsaValueId("%missing"), new SsaValueId("%b")],
                            [new SsaValue(new SsaValueId("%x"), SsaTypes.Int32)])
                    ],
                    terminator: SsaTerminator.Return([new SsaValueId("%x")]))
            ],
            parameters: [new SsaBlockParameter(new SsaValue(new SsaValueId("%b"), SsaTypes.Int32))],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.value.undefined");
    }

    [Test]
    public void Verify_WhenOperationUsesFutureValueInSameBlock_ReturnsDiagnostic()
    {
        var future = new SsaValue(new SsaValueId("%future"), SsaTypes.Int32);
        var current = new SsaValue(new SsaValueId("%current"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        new SsaOperation(new SsaOperationId("op0"), TestOperations.Int32Binary, [future.Id, new SsaValueId("%b")], [current]),
                        new SsaOperation(new SsaOperationId("op1"), TestOperations.Int32Binary, [new SsaValueId("%b"), new SsaValueId("%b")], [future])
                    ],
                    terminator: SsaTerminator.Return([current.Id]))
            ],
            parameters: [new SsaBlockParameter(new SsaValue(new SsaValueId("%b"), SsaTypes.Int32))],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.value.use-before-definition");
    }

    [Test]
    public void Verify_WhenTargetBlockIsMissing_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [new SsaBlock(new SsaBlockId("entry"), terminator: SsaTerminator.Jump(new SsaBlockId("missing")))]));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.terminator.target.missing");
    }

    [Test]
    public void Verify_WhenBlockArgumentCountDoesNotMatch_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    terminator: SsaTerminator.Jump(new SsaBlockId("exit"))),
                new SsaBlock(
                    new SsaBlockId("exit"),
                    parameters: [new SsaBlockParameter(new SsaValue(new SsaValueId("%x"), SsaTypes.Int32))],
                    terminator: SsaTerminator.Return([new SsaValueId("%x")]))
            ],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.block-argument.count");
    }

    [Test]
    public void Verify_WhenDescriptorTypesDoNotMatch_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        new SsaOperation(
                            new SsaOperationId("op0"),
                            TestOperations.Int32Binary,
                            [new SsaValueId("%a"), new SsaValueId("%b")],
                            [new SsaValue(new SsaValueId("%x"), SsaTypes.Bool)])
                    ],
                    terminator: SsaTerminator.Return([new SsaValueId("%x")]))
            ],
            parameters:
            [
                new SsaBlockParameter(new SsaValue(new SsaValueId("%a"), SsaTypes.Int32)),
                new SsaBlockParameter(new SsaValue(new SsaValueId("%b"), SsaTypes.Int32))
            ],
            returnType: SsaTypes.Bool));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.operation.result-type");
    }

    [Test]
    public void Verify_WhenNonVoidFunctionReturnsNoValue_ReturnsDiagnostic()
    {
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [new SsaBlock(new SsaBlockId("entry"), terminator: SsaTerminator.Return())],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.return.value-count");
    }

    [Test]
    public void Verify_WhenReturnValueTypeDoesNotMatchFunctionReturnType_ReturnsDiagnostic()
    {
        var value = new SsaValue(new SsaValueId("%value"), SsaTypes.Bool);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations: [ConstBool("value", value, true)],
                    terminator: SsaTerminator.Return([value.Id]))
            ],
            returnType: SsaTypes.Int32));

        var result = Verify(artifact);

        AssertDiagnostic(result, "ssa.return.type");
    }

    [Test]
    public void Verify_WhenCallableInstructionMatchesSemanticDescriptor_ReturnsSuccess()
    {
        var input = new SsaBlockParameter(new SsaValue(new SsaValueId("%input"), SsaTypes.Int32));
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
                            new SsaOperationId("call.identity"),
                            TestCallables.IdentityInt32,
                            [input.Value.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            parameters: [input],
            returnType: SsaTypes.Int32));

        var verifier = new StructuralSsaVerifier(SsaDescriptorSet.Empty, TestSemanticDescriptors);
        var verification = verifier.Verify(artifact, Context);

        Assert.That(verification.IsSuccess, Is.True);
    }

    [Test]
    public void Verify_WhenCallableUsesFutureValueInSameBlock_ReturnsDiagnostic()
    {
        var input = new SsaBlockParameter(new SsaValue(new SsaValueId("%input"), SsaTypes.Int32));
        var future = new SsaValue(new SsaValueId("%future"), SsaTypes.Int32);
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
                            [future.Id],
                            [callResult]),
                        new SsaCall(
                            new SsaOperationId("call.future"),
                            TestCallables.IdentityInt32,
                            [input.Value.Id],
                            [future])
                    ],
                    terminator: SsaTerminator.Return([callResult.Id]))
            ],
            parameters: [input],
            returnType: SsaTypes.Int32));

        var verifier = new StructuralSsaVerifier(SsaDescriptorSet.Empty, TestSemanticDescriptors);
        var verification = verifier.Verify(artifact, Context);

        AssertDiagnostic(verification, "ssa.value.use-before-definition");
    }

    private static IrVerificationResult Verify(SsaArtifact artifact)
    {
        var verifier = new StructuralSsaVerifier(TestOperationDescriptors);
        return verifier.Verify(artifact, Context);
    }

    private static SsaArtifact CreateValidBranchArtifact()
    {
        var a = new SsaBlockParameter(new SsaValue(new SsaValueId("%a"), SsaTypes.Int32));
        var b = new SsaBlockParameter(new SsaValue(new SsaValueId("%b"), SsaTypes.Int32));
        var condition = new SsaValue(new SsaValueId("%cond"), SsaTypes.Bool);
        var sum = new SsaValue(new SsaValueId("%sum"), SsaTypes.Int32);
        var difference = new SsaValue(new SsaValueId("%diff"), SsaTypes.Int32);
        var result = new SsaBlockParameter(new SsaValue(new SsaValueId("%result"), SsaTypes.Int32));

        return Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        new SsaOperation(
                            new SsaOperationId("cmp"),
                            TestOperations.Int32Equal,
                            [a.Value.Id, b.Value.Id],
                            [condition])
                    ],
                    terminator: SsaTerminator.Branch(
                        condition.Id,
                        new SsaBlockId("then"),
                        [],
                        new SsaBlockId("else"),
                        [])),
                new SsaBlock(
                    new SsaBlockId("then"),
                    operations:
                    [
                        new SsaOperation(
                            new SsaOperationId("add"),
                            TestOperations.Int32Binary,
                            [a.Value.Id, b.Value.Id],
                            [sum])
                    ],
                    terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [sum.Id])),
                new SsaBlock(
                    new SsaBlockId("else"),
                    operations:
                    [
                        new SsaOperation(
                            new SsaOperationId("sub"),
                            TestOperations.Int32Binary,
                            [a.Value.Id, b.Value.Id],
                            [difference])
                    ],
                    terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [difference.Id])),
                new SsaBlock(
                    new SsaBlockId("merge"),
                    parameters: [result],
                    terminator: SsaTerminator.Return([result.Value.Id]))
            ],
            parameters: [a, b],
            returnType: SsaTypes.Int32));
    }

    private static SsaArtifact Artifact(SsaFunction function) =>
        new(new SsaModule(new SsaModuleId("test.module"), [function]));

    private static SsaOperation ConstBool(string id, SsaValue result, bool value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantBool,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static void AssertDiagnostic(IrVerificationResult result, string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(code));
    }

    private static class TestCallables
    {
        public static CallableId IdentityInt32 { get; } = new("test.core.i32.identity");
    }

    private static class TestOperations
    {
        public static SsaOpId Int32Binary { get; } = new("test.i32.binary");

        public static SsaOpId Int32Equal { get; } = new("test.i32.eq");
    }

    private static SsaDescriptorSet TestOperationDescriptors { get; } = new(
    [
        new SsaOpDescriptor(
            SsaOperations.ConstantBool,
            resultTypes: [SsaTypes.Bool],
            requiredAttributes: [SsaAttributeKeys.ConstantValue],
            allowedAttributes: [SsaAttributeKeys.ConstantValue]),
        new SsaOpDescriptor(TestOperations.Int32Binary, [SsaTypes.Int32, SsaTypes.Int32], [SsaTypes.Int32]),
        new SsaOpDescriptor(TestOperations.Int32Equal, [SsaTypes.Int32, SsaTypes.Int32], [SsaTypes.Bool])
    ]);

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
}
