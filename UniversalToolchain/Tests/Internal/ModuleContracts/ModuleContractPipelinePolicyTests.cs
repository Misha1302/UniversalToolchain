using UniversalToolchain.Diagnostics.Abstractions;
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

    [Test]
    public void AirBoundary_WhenTwoBackendFacetsAreSelected_ReportsMultipleBackendCapabilityFacets()
    {
        // The real factory refuses to choose between two runtime backend facets. The observer must convert that
        // refusal into a typed diagnostic rather than let a bare InvalidOperationException escape, otherwise the
        // operator learns nothing about which contract is wrong.
        var table = new ModuleContractTableBuilder()
            .AddFacet(new BackendCapabilityFacet(
                new ModuleId("test.backend.one"),
                [new BackendCapabilityContract(new BackendCapabilityId("test.capability.one"), [])]))
            .AddFacet(new BackendCapabilityFacet(
                new ModuleId("test.backend.two"),
                [new BackendCapabilityContract(new BackendCapabilityId("test.capability.two"), [])]))
            .Build();
        Assert.That(table.Diagnostics, Is.Empty, "the failure must come from selection, not from the table build");

        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P0Structural,
            table,
            new RecordingAirVerifier(),
            new BackendCapabilitySelectionFactory(AirBackendPolicy.CapabilityGated));

        var exception = Assert.Throws<ModuleContractVerificationException>(() => observer.AfterAir(CreateContext([])));

        Assert.That(exception!.Diagnostics.Select(static diagnostic => diagnostic.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.MultipleBackendCapabilityFacets));
    }

    [Test]
    public void AirBoundary_WhenInvalidationRoutesToAnUnhandledVerifier_ReportsReverificationRequired()
    {
        // Invalidation of a fact routed to a different verifier cannot be discharged at the AIR stage. Reporting it
        // is the only honest option: silently dropping the obligation would mean a fact is marked invalid and nobody
        // ever re-verifies it.
        var optimizer = new BackendInputInvalidatingOptimizer();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P3Always,
            CreateTable(optimizer),
            new RecordingAirVerifier());

        var exception = Assert.Throws<ModuleContractVerificationException>(() =>
            observer.AfterAir(CreateContext([optimizer])));

        Assert.That(exception!.Stage, Is.EqualTo("AIR pipeline reverification routing"));
        var diagnostic = exception.Diagnostics.Single(x =>
            x.Code == ModuleContractDiagnosticCodes.CompilerFactReverificationRequired);
        Assert.That(diagnostic.Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
        Assert.That(diagnostic.Message, Does.Contain(KnownCoreVerifierRules.BackendInputContract.Value));
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

    private static ModuleContractPipelineObserver CreateObserver(
        ModuleContractVerificationPolicy policy,
        SelectedModuleContractTable table,
        RecordingAirVerifier verifier,
        IBackendCapabilitySelectionFactory backendSelectionFactory)
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
            backendSelectionFactory,
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

    private static SelectedModuleContractTable CreateTable(IModuleContractDescriptorProvider? contributor = null)
    {
        var builder = new ModuleContractTableBuilder()
            .AddFacet(KnownCoreCompilerFacts.CreateOwnershipFacet());
        if (contributor != null)
            builder.AddFacets(contributor.GetFacets());
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
    private sealed class BackendInputInvalidatingOptimizer : IAirOptimizer, IModuleContractDescriptorProvider
    {
        private static readonly ModuleId Module = new("test.optimizer.invalidate-backend-input");

        public IAbstractIR Optimize(IAbstractIR current) => current;

        public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        [
            new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.optimizer.invalidate-backend-input.effect"),
                        CompilerPipelineStage.Air,
                        [],
                        [],
                        [],
                        [KnownCoreCompilerFacts.BackendInputVerified])
                ])
        ];
    }
}
