using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractPipelinePolicyTests
{
    [TestCase(ModuleContractVerificationPolicy.P0Structural, AirVerificationScope.Structural)]
    [TestCase(ModuleContractVerificationPolicy.P1Invalidation, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
    [TestCase(ModuleContractVerificationPolicy.P2Selective, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
    [TestCase(ModuleContractVerificationPolicy.P3Always, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
    public void InitialAirBoundary_EstablishesExpectedBaseline(
        ModuleContractVerificationPolicy policy,
        AirVerificationScope expectedScopes)
    {
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(policy, CreateTable(), verifier);

        observer.AfterAir(CreateContext([]));

        Assert.That(Combine(verifier.Requests), Is.EqualTo(expectedScopes));
    }

    [TestCase(ModuleContractVerificationPolicy.P0Structural, AirVerificationScope.Structural)]
    [TestCase(ModuleContractVerificationPolicy.P1Invalidation, AirVerificationScope.Structural)]
    [TestCase(ModuleContractVerificationPolicy.P2Selective, AirVerificationScope.Structural)]
    [TestCase(ModuleContractVerificationPolicy.P3Always, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
    public void CleanOptimizedBoundary_OnlyAlwaysRunsSemanticVerification(
        ModuleContractVerificationPolicy policy,
        AirVerificationScope expectedScopes)
    {
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(policy, CreateTable(), verifier);

        observer.AfterOptimizedAir(CreateContext([]));

        Assert.That(Combine(verifier.Requests), Is.EqualTo(expectedScopes));
    }

    [Test]
    public void SelectiveOptimizedBoundary_RunsSemanticVerificationForTypedInvalidation()
    {
        var optimizer = new InvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P2Selective,
            CreateTable(optimizer),
            verifier);

        observer.AfterOptimizedAir(CreateContext([optimizer]));

        Assert.Multiple(() =>
        {
            Assert.That(Combine(verifier.Requests), Is.EqualTo(AirVerificationScope.Full));
            Assert.That(
                verifier.Requests.Select(static request => request.Scope),
                Is.EqualTo(new[] { AirVerificationScope.Structural, AirVerificationScope.Semantic }));
        });
    }

    [Test]
    public void InvalidationOnlyOptimizedBoundary_DoesNotDischargeSemanticObligation()
    {
        var optimizer = new InvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P1Invalidation,
            CreateTable(optimizer),
            verifier);

        observer.AfterOptimizedAir(CreateContext([optimizer]));

        Assert.That(
            verifier.Requests.Select(static request => request.Scope),
            Is.EqualTo(new[] { AirVerificationScope.Structural }));
    }

    private static ModuleContractPipelineObserver CreateObserver(
        ModuleContractVerificationPolicy policy,
        SelectedModuleContractTable table,
        RecordingAirVerifier verifier)
    {
        var options = ModuleContractPipelineProfiles.StrictEnforced with
        {
            VerificationPolicy = policy
        };
        return new ModuleContractPipelineObserver(
            options,
            new FixedTableProvider(table),
            new BytecodeObservedEmissionReader(),
            new BytecodeVerifier(),
            verifier,
            new FixedBackendSelectionFactory(),
            new ModuleContractDiagnosticPolicy(new InMemoryModuleContractDiagnosticSink()),
            new PipelineEffectVerifier(),
            CompilerFactVerifierRegistry.Core,
            new CoreCompilerStageFactSeedProvider());
    }

    private static CompilationPipelineAirContext CreateContext(IReadOnlyList<IAirOptimizer> optimizers) =>
        new(
            new CompilationInput { SourceText = "1" },
            [],
            optimizers,
            new AbstractIR(),
            [],
            []);

    private static SelectedModuleContractTable CreateTable(InvalidatingOptimizer? optimizer = null)
    {
        var builder = new ModuleContractTableBuilder()
            .AddFacet(KnownCoreCompilerFacts.CreateOwnershipFacet());
        if (optimizer != null)
            builder.AddFacets(optimizer.GetFacets());
        var table = builder.Build();
        Assert.That(table.Diagnostics, Is.Empty);
        return table;
    }

    private static AirVerificationScope Combine(IEnumerable<AirVerificationRequest> requests) =>
        requests.Aggregate(AirVerificationScope.None, static (scope, request) => scope | request.Scope);

    private sealed class FixedTableProvider(SelectedModuleContractTable table) : ISelectedModuleContractTableProvider
    {
        public ModuleContractSelectionReport Build(
            IReadOnlyList<IFrontendCoreModule> frontendModules,
            IReadOnlyList<IAirOptimizer> optimizers,
            IReadOnlyList<IBackendPipelineComponent> backendComponents) =>
            new(table, [], table.Diagnostics);
    }

    private sealed class FixedBackendSelectionFactory : IBackendCapabilitySelectionFactory
    {
        public BackendCapabilitySelection Create(
            SelectedModuleContractTable table,
            IReadOnlyList<string> compilerSupportedIntrinsics) =>
            new([], []);
    }

    private sealed class RecordingAirVerifier : IAirVerifier
    {
        public List<AirVerificationRequest> Requests { get; } = [];

        public AirVerificationResult Verify(AirVerificationRequest request)
        {
            Requests.Add(request);
            return new AirVerificationResult(true, []);
        }
    }

    private sealed class InvalidatingOptimizer : IAirOptimizer, IModuleContractDescriptorProvider
    {
        private static readonly ModuleId Module = new("test.optimizer.invalidate-air");

        public IAbstractIR Optimize(IAbstractIR current) => current;

        public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        [
            new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.optimizer.invalidate-air.effect"),
                        CompilerPipelineStage.OptimizedAir,
                        [],
                        [],
                        [],
                        [KnownCoreCompilerFacts.AirVerified])
                ])
        ];
    }
}
