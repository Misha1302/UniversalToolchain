using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageAuthoring;

/// <summary>
/// Immutable descriptor and runtime-component snapshot produced from one authoring source of truth.
/// </summary>
public sealed class AuthoredLanguagePackage : ILanguageExtensionPackage, ILanguageRouteComponentSource
{
    internal AuthoredLanguagePackage(
        LanguagePackageDescriptor descriptor,
        LanguageRuntimeProviderReference? runtimeProvider,
        LanguageContributionId? runtimeContributionId,
        LanguageRouteComponentCatalog components)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        RuntimeProvider = runtimeProvider;
        RuntimeContributionId = runtimeContributionId;
        Components = components ?? throw new ArgumentNullException(nameof(components));
    }

    public LanguagePackageDescriptor Descriptor { get; }
    public LanguagePackageId PackageId => Descriptor.Id;
    public LanguageVersion PackageVersion => Descriptor.Version;
    public LanguageRuntimeProviderReference? RuntimeProvider { get; }
    public LanguageContributionId? RuntimeContributionId { get; }
    public LanguageRouteComponentCatalog Components { get; }
    public bool IsExecutable => RuntimeProvider != null;

    public static LanguageRouteRuntimeProvider CreateRuntimeProvider(
        LanguagePlan plan,
        params ILanguageRouteComponentSource[] componentSources) =>
        LanguageRouteRuntimeAssembler.CreateProvider(plan, componentSources);
}
