using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCanonicalArtifactGraphTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void CanonicalPlan_ContainsSourceSyntaxSemanticBytecodeAirBackendRoute()
    {
        var plan = Compile(new WistLanguageFeaturePackage());
        var route = plan.Routes[Interpreter];

        Assert.Multiple(() =>
        {
            Assert.That(route.SourceContract, Is.EqualTo(StandardLanguageArtifactKinds.SourceText.Contract));
            Assert.That(route.Steps.Select(static step => step.ContributionId), Is.EqualTo(new[]
            {
                WistContributionIds.Frontend,
                WistContributionIds.SemanticBinding,
                WistContributionIds.LoweringToBytecode,
                WistContributionIds.LoweringToAir,
                WistContributionIds.InterpreterBackend
            }));
            Assert.That(route.Steps.Select(static step => step.SourceContract), Is.EqualTo(new[]
            {
                StandardLanguageArtifactKinds.SourceText.Contract,
                WistArtifactKinds.SyntaxTreeContract,
                WistArtifactKinds.SemanticProgramContract,
                WistArtifactKinds.BytecodeContract,
                WistArtifactKinds.AirContract
            }));
            Assert.That(route.Steps.Select(static step => step.TargetContract), Is.EqualTo(new[]
            {
                WistArtifactKinds.SyntaxTreeContract,
                WistArtifactKinds.SemanticProgramContract,
                WistArtifactKinds.BytecodeContract,
                WistArtifactKinds.AirContract,
                WistArtifactKinds.InterpreterArtifactContract
            }));
        });
    }

    [Test]
    public void CanonicalPackage_HasNoSyntaxToBytecodeOrAirShortcut()
    {
        var shortcuts = new WistLanguageFeaturePackage().Descriptor.Contributions
            .Where(static contribution => contribution.Transformation != null)
            .Where(static contribution =>
                contribution.Transformation!.SourceContract == WistArtifactKinds.SyntaxTreeContract &&
                (contribution.Transformation.TargetContract == WistArtifactKinds.BytecodeContract ||
                 contribution.Transformation.TargetContract == WistArtifactKinds.AirContract))
            .Select(static contribution => contribution.Id)
            .ToArray();

        Assert.That(shortcuts, Is.Empty);
    }

    [Test]
    public void Compiler_RejectsBytecodeContractVersionMismatch()
    {
        var descriptor = new WistLanguageFeaturePackage().Descriptor;
        var bytecodeV2 = new LanguageArtifactContract(WistArtifactKinds.Bytecode, "wist.bytecode/v2");
        var mismatched = ReplaceTransformations(
            descriptor,
            contribution => contribution.Id == WistContributionIds.LoweringToAir
                ? new ArtifactTransformationDescriptor(bytecodeV2, WistArtifactKinds.AirContract, contribution.Transformation!.Cost)
                : contribution.Transformation);

        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new Package(mismatched)))
            .Compile(Definition());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "UTL2201"), Is.True);
        });
    }

    [Test]
    public void ContractChange_ChangesPlanHashAndLock_WhileRegistryOrderDoesNot()
    {
        var canonical = new WistLanguageFeaturePackage().Descriptor;
        var bytecodeV2 = new LanguageArtifactContract(WistArtifactKinds.Bytecode, "wist.bytecode/v2");
        var v2 = ReplaceTransformations(
            canonical,
            contribution => contribution.Id == WistContributionIds.LoweringToBytecode
                ? new ArtifactTransformationDescriptor(WistArtifactKinds.SemanticProgramContract, bytecodeV2, contribution.Transformation!.Cost)
                : contribution.Id == WistContributionIds.LoweringToAir
                    ? new ArtifactTransformationDescriptor(bytecodeV2, WistArtifactKinds.AirContract, contribution.Transformation!.Cost)
                    : contribution.Transformation);
        var unrelated = new Package(new LanguagePackageDescriptor(
            new LanguagePackageId("External.Unrelated"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [],
            contributions: [new LanguageContributionDescriptor(new LanguageContributionId("external.unrelated.tooling"), LanguageSlots.Tooling)]));

        var canonicalFirst = Compile(new Package(canonical), unrelated);
        var unrelatedFirst = Compile(unrelated, new Package(canonical));
        var changedContract = Compile(new Package(v2), unrelated);

        Assert.Multiple(() =>
        {
            Assert.That(canonicalFirst.PlanHash, Is.EqualTo(unrelatedFirst.PlanHash));
            Assert.That(LanguageLockFile.Serialize(canonicalFirst), Is.EqualTo(LanguageLockFile.Serialize(unrelatedFirst)));
            Assert.That(changedContract.PlanHash, Is.Not.EqualTo(canonicalFirst.PlanHash));
            Assert.That(LanguageLockFile.Serialize(changedContract), Is.Not.EqualTo(LanguageLockFile.Serialize(canonicalFirst)));
            Assert.That(LanguageLockFile.Serialize(canonicalFirst), Does.Contain("wist.semantic-program"));
            Assert.That(LanguageLockFile.Serialize(canonicalFirst), Does.Contain("wist.semantic-program/v1"));
            Assert.That(LanguageLockFile.Serialize(canonicalFirst), Does.Contain("wist.bytecode"));
            Assert.That(LanguageLockFile.Serialize(canonicalFirst), Does.Contain("wist.bytecode/v1"));
        });
    }

    [Test]
    public void ManifestRoundTrip_PreservesSemanticAndBytecodeBoundaryContracts()
    {
        var descriptor = new WistLanguageFeaturePackage().Descriptor;
        var roundTrip = LanguageFeatureManifestSerializer.Deserialize(LanguageFeatureManifestSerializer.Serialize(descriptor));
        var syntaxToSemantic = roundTrip.Contributions.Single(static contribution => contribution.Id == WistContributionIds.SemanticBinding);
        var semanticToBytecode = roundTrip.Contributions.Single(static contribution => contribution.Id == WistContributionIds.LoweringToBytecode);
        var bytecodeToAir = roundTrip.Contributions.Single(static contribution => contribution.Id == WistContributionIds.LoweringToAir);

        Assert.Multiple(() =>
        {
            Assert.That(syntaxToSemantic.Transformation!.SourceContract, Is.EqualTo(WistArtifactKinds.SyntaxTreeContract));
            Assert.That(syntaxToSemantic.Transformation.TargetContract, Is.EqualTo(WistArtifactKinds.SemanticProgramContract));
            Assert.That(semanticToBytecode.Transformation!.SourceContract, Is.EqualTo(WistArtifactKinds.SemanticProgramContract));
            Assert.That(semanticToBytecode.Transformation.TargetContract, Is.EqualTo(WistArtifactKinds.BytecodeContract));
            Assert.That(bytecodeToAir.Transformation!.SourceContract, Is.EqualTo(WistArtifactKinds.BytecodeContract));
            Assert.That(bytecodeToAir.Transformation.TargetContract, Is.EqualTo(WistArtifactKinds.AirContract));
            Assert.That(roundTrip.Contributions.Any(static contribution =>
                contribution.Transformation != null &&
                contribution.Transformation.SourceContract == WistArtifactKinds.SyntaxTreeContract &&
                contribution.Transformation.TargetContract == WistArtifactKinds.BytecodeContract), Is.False);
            Assert.That(LanguageFeatureManifestSerializer.ComputeSha256(roundTrip), Is.EqualTo(LanguageFeatureManifestSerializer.ComputeSha256(descriptor)));
        });
    }

    private static LanguagePlan Compile(params ILanguageFeaturePackage[] packages)
    {
        var registry = new LanguagePackageRegistry();
        foreach (var package in packages)
            registry.AddPackage(package);
        return new LanguageCompiler(registry).Compile(Definition()).GetRequiredPlan();
    }

    private static LanguageDefinition Definition() =>
        LanguageDefinitionBuilder.Create("Wist.S01.ArtifactGraph", "1")
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(Interpreter)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .Build();

    private static LanguagePackageDescriptor ReplaceTransformations(
        LanguagePackageDescriptor descriptor,
        Func<LanguageContributionDescriptor, ArtifactTransformationDescriptor?> replace)
    {
        var contributions = descriptor.Contributions.Select(contribution => new LanguageContributionDescriptor(
            contribution.Id,
            contribution.Slot,
            contribution.Multiplicity,
            contribution.MergePolicy,
            contribution.RequiresContributions,
            contribution.ProvidesCapabilities,
            contribution.RequiresCapabilities,
            contribution.Conflicts,
            contribution.ConflictsCapabilities,
            contribution.SupportedBackends,
            replace(contribution),
            contribution.RuntimeProviderId,
            contribution.RuntimeProviderVersion,
            contribution.Order,
            contribution.Metadata,
            contribution.RuntimeInputContracts,
            contribution.BackendInputContract,
            contribution.BeforeContributions,
            contribution.AfterContributions)).ToArray();

        return new LanguagePackageDescriptor(
            descriptor.Id,
            descriptor.Version,
            descriptor.ToolchainApiVersion,
            descriptor.Features,
            descriptor.Metadata,
            contributions);
    }

    private sealed class Package(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
