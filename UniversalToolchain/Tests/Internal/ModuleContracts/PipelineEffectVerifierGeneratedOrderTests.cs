using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class PipelineEffectVerifierGeneratedOrderTests
{
    [Test]
    public void Validate_ShouldRespectDeclaredEffectOrder_ForGeneratedProducerConsumerPairs()
    {
        foreach (var caseIndex in Enumerable.Range(0, 16))
        {
            var (table, producerModule, consumerModule) = BuildProducerConsumerPair(caseIndex);

            var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                table,
                CompilerPipelineStage.Bytecode,
                CompilerFactState.Empty,
                CompilerFactVerifierRegistry.Core,
                [consumerModule, producerModule]));

            Assert.That(
                result.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ModuleContractDiagnosticCodes.MissingRequiredCompilerFact),
                $"Case {caseIndex} must fail because the fact producer appears after its consumer in the pipeline order.");
        }
    }

    [Test]
    public void Validate_ShouldAcceptGeneratedProducerConsumerPairs_WhenProducerSortsBeforeConsumer()
    {
        foreach (var caseIndex in Enumerable.Range(0, 16))
        {
            var (table, producerModule, consumerModule) = BuildProducerConsumerPair(caseIndex);

            var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
                table,
                CompilerPipelineStage.Bytecode,
                CompilerFactState.Empty,
                CompilerFactVerifierRegistry.Core,
                [producerModule, consumerModule]));

            Assert.That(
                result.Diagnostics,
                Is.Empty,
                $"Case {caseIndex} must pass because the fact producer appears before its consumer in the pipeline order.");
        }
    }

    private static (SelectedModuleContractTable Table, ModuleId ProducerModule, ModuleId ConsumerModule) BuildProducerConsumerPair(int caseIndex)
    {
        var fact = new CompilerFactId($"test.generated.fact.{caseIndex:00}");
        var producerModule = new ModuleId($"test.generated.{caseIndex:00}.producer");
        var consumerModule = new ModuleId($"test.generated.{caseIndex:00}.consumer");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                producerModule,
                [
                    new CompilerFactOwnershipContract(fact, producerModule)
                ]))
            .AddFacet(new PipelineEffectFacet(
                producerModule,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId($"test.generated.effect.{caseIndex:00}.produce"),
                        CompilerPipelineStage.Bytecode,
                        [],
                        [fact],
                        [],
                        [])
                ]))
            .AddFacet(new PipelineEffectFacet(
                consumerModule,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId($"test.generated.effect.{caseIndex:00}.consume"),
                        CompilerPipelineStage.Bytecode,
                        [fact],
                        [],
                        [],
                        [])
                ]))
            .Build();
        return (table, producerModule, consumerModule);
    }
}
