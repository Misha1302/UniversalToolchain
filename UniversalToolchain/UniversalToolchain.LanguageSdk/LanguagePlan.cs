using System.Collections.ObjectModel;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

public sealed record ResolvedLanguageFeature
{
    internal ResolvedLanguageFeature(
        LanguagePackageRegistrationIdentity packageIdentity,
        LanguageFeatureDescriptor feature)
    {
        PackageIdentity = packageIdentity ?? throw new ArgumentNullException(nameof(packageIdentity));
        Feature = feature ?? throw new ArgumentNullException(nameof(feature));
    }

    public LanguagePackageRegistrationIdentity PackageIdentity { get; }
    public LanguagePackageId PackageId => PackageIdentity.PackageId;
    public LanguageVersion PackageVersion => PackageIdentity.PackageVersion;
    public string ManifestSha256 => PackageIdentity.ManifestSha256;
    public LanguageFeatureDescriptor Feature { get; }
}

public sealed record ResolvedLanguageContribution
{
    internal ResolvedLanguageContribution(
        LanguagePackageRegistrationIdentity packageIdentity,
        LanguageContributionDescriptor contribution)
    {
        PackageIdentity = packageIdentity ?? throw new ArgumentNullException(nameof(packageIdentity));
        Contribution = contribution ?? throw new ArgumentNullException(nameof(contribution));
    }

    public LanguagePackageRegistrationIdentity PackageIdentity { get; }
    public LanguagePackageId PackageId => PackageIdentity.PackageId;
    public LanguageVersion PackageVersion => PackageIdentity.PackageVersion;
    public string ManifestSha256 => PackageIdentity.ManifestSha256;
    public LanguageContributionDescriptor Contribution { get; }
}

public sealed record LanguageArtifactRouteStep(
    LanguageContributionId ContributionId,
    LanguageArtifactContract SourceContract,
    LanguageArtifactContract TargetContract,
    int Cost)
{
    public LanguageArtifactKindId Source => SourceContract.Kind;
    public LanguageArtifactKindId Target => TargetContract.Kind;
}

public sealed class LanguageArtifactRoute
{
    public LanguageArtifactRoute(
        BackendId backend,
        LanguageArtifactContract source,
        LanguageArtifactContract target,
        IEnumerable<LanguageArtifactRouteStep> steps)
    {
        Backend = backend;
        SourceContract = source;
        TargetContract = target;
        Steps = new ReadOnlyCollection<LanguageArtifactRouteStep>(steps.ToArray());
        TotalCost = Steps.Sum(static x => x.Cost);
        if (Steps.Count == 0 && source != target)
            throw new ArgumentException("A non-identity route must contain at least one transformation.", nameof(steps));
        var current = source;
        foreach (var step in Steps)
        {
            if (!ContractsConnect(current, step.SourceContract))
                throw new ArgumentException("Artifact route steps are not type-compatible and contiguous.", nameof(steps));
            current = step.TargetContract;
        }
        if (!ContractsConnect(current, target))
            throw new ArgumentException("Artifact route does not reach its declared target contract.", nameof(steps));
    }

    public BackendId Backend { get; }
    public LanguageArtifactContract SourceContract { get; }
    public LanguageArtifactContract TargetContract { get; }
    public LanguageArtifactKindId Source => SourceContract.Kind;
    public LanguageArtifactKindId Target => TargetContract.Kind;
    public IReadOnlyList<LanguageArtifactRouteStep> Steps { get; }
    public int TotalCost { get; }

    public static bool ContractsConnect(LanguageArtifactContract produced, LanguageArtifactContract consumed)
    {
        if (produced.Kind != consumed.Kind)
            return false;
        if (produced.ValueTypeIdentity == null || consumed.ValueTypeIdentity == null)
            return produced.ValueTypeIdentity == null && consumed.ValueTypeIdentity == null;
        return StringComparer.Ordinal.Equals(produced.ValueTypeIdentity, consumed.ValueTypeIdentity);
    }
}

public sealed class LanguagePlan
{
    internal LanguagePlan(
        LanguageDefinition definition,
        IEnumerable<ResolvedLanguageFeature> features,
        IEnumerable<ResolvedLanguageContribution> contributions,
        ResolvedLanguageContribution? runtimeProviderContribution,
        IEnumerable<LanguageArtifactRoute> routes)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Features = new ReadOnlyCollection<ResolvedLanguageFeature>(features.ToList());
        Contributions = new ReadOnlyCollection<ResolvedLanguageContribution>(contributions.ToList());
        RuntimeProviderContribution = runtimeProviderContribution;
        Routes = new ReadOnlyDictionary<BackendId, LanguageArtifactRoute>(
            routes.ToDictionary(static x => x.Backend));

        if (runtimeProviderContribution != null)
        {
            var runtimeProviderId = runtimeProviderContribution.Contribution.RuntimeProviderId
                ?? throw new ArgumentException("The selected runtime contribution does not declare a runtime provider.", nameof(runtimeProviderContribution));
            var runtimeProviderVersion = runtimeProviderContribution.Contribution.RuntimeProviderVersion
                ?? throw new ArgumentException("The selected runtime contribution does not declare a runtime provider version.", nameof(runtimeProviderContribution));
            RuntimeProvider = new LanguageRuntimeProviderReference(runtimeProviderId, runtimeProviderVersion);
        }
        else if (definition.IsExecutable)
        {
            throw new ArgumentException("An executable language plan requires a runtime provider contribution.", nameof(runtimeProviderContribution));
        }

        PlanHash = LanguagePlanCanonicalizer.ComputeHash(
            Definition,
            Features,
            Contributions,
            RuntimeProviderContribution,
            Routes.Values);
        LanguagePlanVerifier.Verify(this);

        Summary = new LanguagePlanSummary(
            definition.Id,
            definition.Version,
            PlanHash,
            Features.Select(static x => x.Feature.Id).ToArray(),
            definition.Backends,
            RuntimeProvider,
            definition.EntryArtifact);
    }

    public LanguageDefinition Definition { get; }
    public IReadOnlyList<ResolvedLanguageFeature> Features { get; }
    public IReadOnlyList<ResolvedLanguageContribution> Contributions { get; }
    public ResolvedLanguageContribution? RuntimeProviderContribution { get; }
    public LanguageRuntimeProviderReference? RuntimeProvider { get; }
    public IReadOnlyDictionary<BackendId, LanguageArtifactRoute> Routes { get; }
    public string PlanHash { get; }
    public LanguagePlanSummary Summary { get; }
    public bool IsExecutable => RuntimeProvider != null;
}

public sealed class LanguageBuildResult
{
    private LanguageBuildResult(LanguagePlan? plan, IReadOnlyList<LanguageDiagnostic> diagnostics)
    {
        Plan = plan;
        Diagnostics = diagnostics;
    }

    public LanguagePlan? Plan { get; }
    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }
    public bool IsSuccess => Plan != null && Diagnostics.All(static x => x.Severity != LanguageDiagnosticSeverity.Error);

    public LanguagePlan GetRequiredPlan()
    {
        if (Plan != null)
            return Plan;
        var details = string.Join(Environment.NewLine, Diagnostics.Select(static x => $"[{x.Code}] {x.Message}"));
        throw new InvalidOperationException($"Language plan compilation failed:{Environment.NewLine}{details}");
    }

    public static LanguageBuildResult Success(LanguagePlan plan) => new(plan, []);
    public static LanguageBuildResult Failure(IEnumerable<LanguageDiagnostic> diagnostics) => new(null, diagnostics.ToArray());
}
