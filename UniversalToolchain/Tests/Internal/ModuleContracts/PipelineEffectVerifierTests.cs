using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class PipelineEffectVerifierTests
{
    private static readonly ModuleId _producer = new("test.producer");
    private static readonly ModuleId _consumer = new("test.consumer");
    private static readonly CompilerFactId _ownedFact = new("test.fact.owned");
    private static readonly CompilerEffectId _effect = new("test.effect.pipeline");

    [Test]
    public void Validate_WhenRequiredFactIsMissing_ReportsDiagnostic()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Bytecode,
                        [KnownCoreCompilerFacts.AstBound],
                        [],
                        [],
                        [])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Bytecode,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core,
            [_consumer]));

        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.MissingRequiredCompilerFact));
    }

    [Test]
    public void Validate_WhenVerifiableFactIsInvalidated_CreatesReverificationRequest()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Air,
                        [],
                        [],
                        [],
                        [KnownCoreCompilerFacts.AirVerified])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Air,
            new CompilerFactState(
                new HashSet<CompilerFactId> { KnownCoreCompilerFacts.AirVerified },
                new HashSet<CompilerFactId>()),
            CompilerFactVerifierRegistry.Core,
            [_consumer]));

        Assert.That(result.ReverificationRequests.Single().RuleId, Is.EqualTo(KnownCoreVerifierRules.AirContract));
        Assert.That(result.ReverificationRequests.Single().InvalidatedFacts, Does.Contain(KnownCoreCompilerFacts.AirVerified));
    }

    [Test]
    public void Validate_WhenExternalProviderRoutesCustomFact_CreatesCustomReverificationRequest()
    {
        var customFact = new CompilerFactId("extension.fact.verified");
        var customRule = new VerifierRuleId("extension.verifier.contract");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                _consumer,
                [new CompilerFactOwnershipContract(customFact, _consumer)]))
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Air,
                        [],
                        [],
                        [],
                        [customFact])
                ]))
            .Build();
        var registry = new CompilerFactVerifierRegistry(
            [new TestVerifierRuleProvider(customFact, customRule)]);

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Air,
            new CompilerFactState([customFact], []),
            registry,
            [_consumer]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.ReverificationRequests.Single().RuleId, Is.EqualTo(customRule));
            Assert.That(result.ReverificationRequests.Single().InvalidatedFacts, Is.EqualTo(new[] { customFact }));
        });
    }

    [Test]
    public void Validate_WhenProducerAppearsAfterConsumer_DoesNotInventAlternativePipelineOrder()
    {
        var producedFact = new CompilerFactId("test.fact.produced-late");
        var consumerModule = new ModuleId("test.a-consumer");
        var producerModule = new ModuleId("test.z-producer");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                producerModule,
                [
                    new CompilerFactOwnershipContract(producedFact, producerModule)
                ]))
            .AddFacet(new PipelineEffectFacet(
                consumerModule,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.consume-late-fact"),
                        CompilerPipelineStage.Bytecode,
                        [producedFact],
                        [],
                        [],
                        [])
                ]))
            .AddFacet(new PipelineEffectFacet(
                producerModule,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.effect.produce-late-fact"),
                        CompilerPipelineStage.Bytecode,
                        [],
                        [producedFact],
                        [],
                        [])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Bytecode,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core,
            [consumerModule, producerModule]));

        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.MissingRequiredCompilerFact));
    }

    [Test]
    public void Validate_WhenPipelineContainsRepeatedModule_RejectsAmbiguousOccurrenceModel()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Bytecode,
                        [],
                        [],
                        [],
                        [])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Bytecode,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core,
            [_consumer, _consumer]));

        Assert.That(
            result.Diagnostics.Select(static diagnostic => diagnostic.Code),
            Does.Contain(ModuleContractDiagnosticCodes.DuplicatePipelineModuleOccurrence));
    }

    [Test]
    public void Validate_WhenEffectPreservesUnavailableFact_ReportsDiagnostic()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                _producer,
                [
                    new CompilerFactOwnershipContract(_ownedFact, _producer)
                ]))
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Bytecode,
                        [],
                        [],
                        [_ownedFact],
                        [])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Bytecode,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core,
            [_consumer]));

        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.MissingRequiredCompilerFact));
    }

    [Test]
    public void Build_WhenModuleProducesForeignOwnedFact_ReportsDiagnostic()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                _producer,
                [
                    new CompilerFactOwnershipContract(_ownedFact, _producer)
                ]))
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Bytecode,
                        [],
                        [_ownedFact],
                        [],
                        [])
                ]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.ForeignCompilerFactProduction));
    }

    [Test]
    public void Build_WhenFactOwnershipIsDeclaredTwiceBySameOwner_ReportsDiagnostic()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new CompilerFactOwnershipFacet(
                _producer,
                [
                    new CompilerFactOwnershipContract(_ownedFact, _producer),
                    new CompilerFactOwnershipContract(_ownedFact, _producer)
                ]))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.DuplicateCompilerFactOwner));
    }

    [Test]
    public void Validate_WhenPipelineOrderIsMissing_ReportsStrictDiagnostic()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new PipelineEffectFacet(
                _consumer,
                [
                    new PipelineEffectContract(
                        _effect,
                        CompilerPipelineStage.Bytecode,
                        [],
                        [],
                        [],
                        [])
                ]))
            .Build();

        var result = new PipelineEffectVerifier().Validate(new PipelineEffectValidationRequest(
            table,
            CompilerPipelineStage.Bytecode,
            CompilerFactState.Empty,
            CompilerFactVerifierRegistry.Core));

        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.MissingPipelineOrder));
    }

    private sealed class TestVerifierRuleProvider(
        CompilerFactId fact,
        VerifierRuleId rule) : ICompilerFactVerifierRuleProvider
    {
        public IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules() =>
            new Dictionary<CompilerFactId, VerifierRuleId> { [fact] = rule };
    }
}
