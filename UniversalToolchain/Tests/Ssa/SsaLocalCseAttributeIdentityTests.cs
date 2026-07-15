using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaLocalCseAttributeIdentityTests
{
    [Test]
    public void Run_WhenOnlyAttributesDiffer_PreservesBothCalls()
    {
        var callable = new CallableId("test.attribute-sensitive");
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
                    new CallableSignature([], [SsaSemanticTypes.Int32]),
                    effects: SemanticEffectSummary.Pure,
                    determinism: Determinism.Deterministic,
                    trustLevel: SemanticTrustLevel.BuiltInTrusted,
                    allowedAttributes: [new SemanticAttributeKey("mode")])
            ]);
        var first = new SsaValue(new SsaValueId("%first"), SsaTypes.Int32);
        var second = new SsaValue(new SsaValueId("%second"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                new SsaCall(
                    new SsaOperationId("first"),
                    callable,
                    results: [first],
                    attributes: Attributes("first")),
                new SsaCall(
                    new SsaOperationId("second"),
                    callable,
                    results: [second],
                    attributes: Attributes("second"))
            ],
            terminator: SsaTerminator.Return([second.Id]));
        var artifact = new SsaArtifact(new SsaModule(
            new SsaModuleId("test.module"),
            [
                new SsaFunction(
                    new SsaFunctionId("test.function"),
                    block.Id,
                    [block],
                    returnType: SsaTypes.Int32)
            ]));

        var optimized = new SsaLocalCommonSubexpressionEliminationPass(descriptors)
            .Run(artifact, new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

        Assert.That(
            optimized.Module.Functions.Single().Blocks.Single().Instructions
                .Select(static instruction => instruction.Id.Value),
            Is.EqualTo(new[] { "first", "second" }));
    }

    private static SsaAttributeBag Attributes(string mode) =>
        new([new SsaAttribute(new SsaAttributeKey("mode"), mode)]);
}
