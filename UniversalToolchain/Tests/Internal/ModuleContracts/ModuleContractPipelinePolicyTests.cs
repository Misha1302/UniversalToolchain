using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractPipelinePolicyTests
{
    [TestCase(ModuleContractVerificationPolicy.P0Structural, AirVerificationScope.Structural)]
    [TestCase(ModuleContractVerificationPolicy.P1Invalidation, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
    [TestCase(ModuleContractVerificationPolicy.P1DemandRecomputation, AirVerificationScope.Structural | AirVerificationScope.Semantic)]
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
    [TestCase(ModuleContractVerificationPolicy.P1DemandRecomputation, AirVerificationScope.Structural)]
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
    public void DemandRecomputationWithoutDownstreamQuery_DoesNotDischargeSemanticObligation()
    {
        var optimizer = new InvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P1DemandRecomputation,
            CreateTable(optimizer),
            verifier);

        observer.AfterOptimizedAir(CreateContext([optimizer]));

        Assert.That(
            verifier.Requests.Select(static request => request.Scope),
            Is.EqualTo(new[] { AirVerificationScope.Structural }));
    }

    [Test]
    public void DemandRecomputationWithExplicitDownstreamQuery_RecomputesInvalidatedFact()
    {
        var optimizer = new InvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P1DemandRecomputation,
            CreateTable(optimizer),
            verifier,
            new HashSet<CompilerFactId> { KnownCoreCompilerFacts.AirVerified });

        observer.AfterOptimizedAir(CreateContext([optimizer]));

        Assert.That(
            verifier.Requests.Select(static request => request.Scope),
            Is.EqualTo(new[] { AirVerificationScope.Structural, AirVerificationScope.Semantic }));
    }

    [TestCase(ModuleContractVerificationPolicy.P1Invalidation)]
    [TestCase(ModuleContractVerificationPolicy.P1DemandRecomputation)]
    public void NonEnforcingPolicies_CarryPassiveStateWithoutTurningItIntoABoundaryFailure(
        ModuleContractVerificationPolicy policy)
    {
        var optimizer = new BackendInputInvalidatingOptimizer();
        var input = new CompilationInput { SourceText = "1" };
        var airVerifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            policy,
            CreateTable(optimizer),
            airVerifier);
        var context = CreateContext(input, [optimizer]);

        Assert.DoesNotThrow(() =>
        {
            observer.AfterAir(context);
            observer.AfterOptimizedAir(context);
            observer.BeforeBackend(context);
        });
        Assert.That(
            airVerifier.Requests.Count(static request => request.Scope.HasFlag(AirVerificationScope.Semantic)),
            Is.EqualTo(1));
    }

    [Test]
    public void Selective_CarriesDeferredBackendObligationAcrossBoundaries()
    {
        var optimizer = new BackendInputInvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P2Selective,
            CreateTable(optimizer),
            verifier);
        var input = new CompilationInput { SourceText = "1" };
        var context = CreateContext(input, [optimizer]);

        observer.AfterAir(context);
        observer.AfterOptimizedAir(context);
        observer.BeforeBackend(context);

        Assert.That(
            verifier.Requests.Select(static request => request.Scope),
            Is.EqualTo(new[]
            {
                AirVerificationScope.Structural,
                AirVerificationScope.Semantic,
                AirVerificationScope.Structural,
                AirVerificationScope.Semantic
            }));
    }

    [Test]
    public void InterleavedCompilations_KeepLifecycleStateIsolatedByInputIdentity()
    {
        var optimizer = new BackendInputInvalidatingOptimizer();
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P2Selective,
            CreateTable(optimizer),
            verifier);
        var first = CreateContext(new CompilationInput { SourceText = "1" }, [optimizer]);
        var second = CreateContext(new CompilationInput { SourceText = "2" }, [optimizer]);

        Assert.DoesNotThrow(() =>
        {
            observer.AfterAir(first);
            observer.AfterAir(second);
            observer.AfterOptimizedAir(first);
            observer.AfterOptimizedAir(second);
            observer.BeforeBackend(second);
            observer.BeforeBackend(first);
        });
        Assert.Multiple(() =>
        {
            Assert.That(
                verifier.Requests.Count(static request => request.Scope == AirVerificationScope.Structural),
                Is.EqualTo(4));
            Assert.That(
                verifier.Requests.Count(static request => request.Scope == AirVerificationScope.Semantic),
                Is.EqualTo(4));
        });
    }

    [Test]
    public void OutOfOrderBoundary_RemovesFailedLifecycleBeforeRetry()
    {
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P2Selective,
            CreateTable(),
            verifier);
        var input = new CompilationInput { SourceText = "1" };
        var context = CreateContext(input, []);

        observer.AfterOptimizedAir(context);

        Assert.That(() => observer.AfterAir(context), Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => observer.AfterAir(context),
            Throws.Nothing,
            "A failed lifecycle must not contaminate a retry with the same input identity.");
    }

    [Test]
    public void FinalBoundary_RemovesLifecycleSoInputCanBeCompiledAgain()
    {
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P2Selective,
            CreateTable(),
            verifier);
        var input = new CompilationInput { SourceText = "1" };
        var context = CreateContext(input, []);

        static void ExecuteLifecycle(
            ModuleContractPipelineObserver pipelineObserver,
            CompilationPipelineAirContext pipelineContext)
        {
            pipelineObserver.AfterAir(pipelineContext);
            pipelineObserver.AfterOptimizedAir(pipelineContext);
            pipelineObserver.BeforeBackend(pipelineContext);
        }

        ExecuteLifecycle(observer, context);

        Assert.That(() => ExecuteLifecycle(observer, context), Throws.Nothing);
    }

    [Test]
    public void AlwaysPolicy_RunsBackendInputRouteWithoutAnObligation()
    {
        var verifier = new RecordingAirVerifier();
        var observer = CreateObserver(
            ModuleContractVerificationPolicy.P3Always,
            CreateTable(),
            verifier);
        var context = CreateContext([]);

        observer.BeforeBackend(context);

        Assert.That(
            verifier.Requests.Select(static request => request.Scope),
            Is.EqualTo(new[] { AirVerificationScope.Semantic }));
    }

    private static ModuleContractPipelineObserver CreateObserver(
        ModuleContractVerificationPolicy policy,
        SelectedModuleContractTable table,
        RecordingAirVerifier verifier,
        IReadOnlySet<CompilerFactId>? demandedFacts = null)
    {
        var options = ModuleContractPipelineProfiles.StrictEnforced with
        {
            VerificationPolicy = policy,
            DemandedFacts = demandedFacts ?? new HashSet<CompilerFactId>()
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
        CreateContext(new CompilationInput { SourceText = "1" }, optimizers);

    private static CompilationPipelineAirContext CreateContext(
        CompilationInput input,
        IReadOnlyList<IAirOptimizer> optimizers) =>
        new(
            input,
            [],
            optimizers,
            new AbstractIR(),
            [],
            []);

    private static SelectedModuleContractTable CreateTable(IModuleContractDescriptorProvider? descriptor = null)
    {
        var builder = new ModuleContractTableBuilder()
            .AddFacet(KnownCoreCompilerFacts.CreateOwnershipFacet());
        if (descriptor != null)
            builder.AddFacets(descriptor.GetFacets());
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
        private static readonly ModuleId Module = new("test.optimizer.defer-backend-input");

        public IAbstractIR Optimize(IAbstractIR current) => current;

        public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        [
            new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("test.optimizer.defer-backend-input.effect"),
                        CompilerPipelineStage.OptimizedAir,
                        [],
                        [],
                        [],
                        [KnownCoreCompilerFacts.BackendInputVerified])
                ])
        ];
    }
}
