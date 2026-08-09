using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;
using UniversalToolchain.Testing.Infrastructure;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistVerificationPolicyTests
{
    [TestCase((int)WistVerificationPolicy.P0Structural)]
    [TestCase((int)WistVerificationPolicy.P1Invalidation)]
    [TestCase((int)WistVerificationPolicy.P2Selective)]
    [TestCase((int)WistVerificationPolicy.P3Always)]
    public void Create_WithExplicitPolicy_EvaluatesValidProgram(int policyValue)
    {
        var policy = (WistVerificationPolicy)policyValue;
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = policy
        });

        Assert.That(engine.Evaluate<double>("1 + 2"), Is.EqualTo(3.0d).Within(1e-9));
    }

    [TestCase((int)WistVerificationPolicy.P0Structural, false)]
    [TestCase((int)WistVerificationPolicy.P1Invalidation, false)]
    [TestCase((int)WistVerificationPolicy.P2Selective, true)]
    [TestCase((int)WistVerificationPolicy.P3Always, true)]
    public void ExactPlannedFault_UsesConfiguredVerificationPolicy(int policyValue, bool expectRejection)
    {
        var policy = (WistVerificationPolicy)policyValue;
        var wistPackage = WistVerificationRuntimePackageFactory.Create(policy);
        var faultPackage = CreateFaultPackage();
        var registry = new LanguagePackageRegistry()
            .AddPackage(wistPackage)
            .AddPackage(faultPackage);
        var definition = AddFeature(
            WistLanguageDefinitions.Create("pricing-restricted"),
            faultPackage.FeatureId);
        var plan = new LanguageCompiler(registry)
            .Compile(definition)
            .GetRequiredPlan();
        using var runtime = LanguageRuntime.Create(
            plan,
            new ILanguageRouteComponentSource[] { wistPackage, faultPackage },
            new LanguageRuntimeOptions());

        if (!expectRejection)
        {
            var result = runtime.Run(new LanguageExecutionRequest("2 + 3", new BackendId("cil")));
            Assert.That(Convert.ToDouble(result.Value), Is.EqualTo(1d));
            return;
        }

        var exception = Assert.Throws<ModuleContractVerificationException>(() =>
            runtime.Run(new LanguageExecutionRequest("2 + 3", new BackendId("cil"))));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Stage, Is.EqualTo("optimized AIR contract verification"));
            Assert.That(
                exception.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ModuleContractDiagnosticCodes.MissingBackendCapability));
        });
    }

    [Test]
    public void Create_RejectsUnknownPolicy()
    {
        var options = new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = (WistVerificationPolicy)int.MaxValue
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => WistEngine.Create(options));
    }

    [Test]
    public void Create_SnapshotsVerificationPolicy()
    {
        var options = new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset("pricing-restricted"),
            VerificationPolicy = WistVerificationPolicy.P3Always
        };
        using var engine = WistEngine.Create(options);

        options.VerificationPolicy = (WistVerificationPolicy)int.MaxValue;

        Assert.That(engine.Evaluate<double>("2 + 3"), Is.EqualTo(5.0d).Within(1e-9));
    }

    private static WistAirOptimizerTestPackage CreateFaultPackage() => new(
        new LanguagePackageId("tests.wist.verification-fault"),
        new LanguageVersion("1.0.0"),
        new LanguageFeatureId("tests.wist.verification-fault"),
        new LanguageContributionId("tests.wist.verification-fault"),
        static () => new VerificationFaultOptimizer(),
        traits: LanguageRuntimeComponentTraits.DeterministicNoHostInterop);

    private static LanguageDefinition AddFeature(LanguageDefinition baseline, LanguageFeatureId feature) => new(
        baseline.Id,
        baseline.Version,
        baseline.ToolchainApiVersion,
        baseline.SelectedFeatures.Append(feature),
        baseline.Backends,
        baseline.RuntimeProvider,
        baseline.RuntimePolicy,
        baseline.Metadata,
        baseline.SlotOverrides,
        baseline.CapabilityProviders,
        baseline.ExcludedContributions,
        baseline.EntryArtifact,
        baseline.ContributionOrderConstraints,
        baseline.IntrinsicPolicy);

    private sealed class VerificationFaultOptimizer : IAirOptimizer, IModuleContractDescriptorProvider
    {
        private static readonly ModuleId Module = new("tests.wist.verification-fault");
        private static readonly BackendCapabilityId MissingCapability = KnownCoreBackendCapabilities.ConditionalBranches;
        private static readonly IntrinsicSymbolId ReplacementIntrinsic = new("load_i32");
        private static readonly IntrinsicSymbolId ContractOnlyMarker = new("tests_wist_verification_contract_only_marker");

        public IAbstractIR Optimize(IAbstractIR current)
        {
            ArgumentNullException.ThrowIfNull(current);
            current.AppendInstructions(
            [
                new Instruction(UOpCode.Drop),
                IntrinsicInstructionFactory.CreateForCapability("load_i32", 1)
            ]);
            return current;
        }

        public IReadOnlyList<IModuleContractFacet> GetFacets() =>
        [
            new AirContractFacet(
                Module,
                [
                    new AirEmissionContract(
                        new BytecodePatternId("tests.wist.verification.source-result"),
                        [new AirPatternId("tests.wist.verification.replace-result")],
                        [ReplacementIntrinsic, ContractOnlyMarker],
                        [MissingCapability])
                ]),
            new PipelineEffectFacet(
                Module,
                [
                    new PipelineEffectContract(
                        new CompilerEffectId("tests.wist.verification.invalidate-air-result"),
                        CompilerPipelineStage.OptimizedAir,
                        [],
                        [],
                        [],
                        [KnownCoreCompilerFacts.AirVerified])
                ])
        ];
    }
}
