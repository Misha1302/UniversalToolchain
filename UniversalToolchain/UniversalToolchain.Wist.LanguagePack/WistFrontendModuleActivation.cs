using BasicCore.Contracts;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistFrontendModuleActivation
{
    public static IWistFrontendModuleSource CreateBuiltInSource(WistLanguageFeaturePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return new WistFrontendModuleSource(
            package,
            WistRuntimeComponentCatalog.Modules.Select(static component =>
                new WistFrontendModuleRegistration(
                    component.ContributionId,
                    component.ModuleFactory ?? throw new InvalidOperationException(
                        $"Canonical Wist module '{component.ContributionId.Value}' has no activation factory."))));
    }

    public static IReadOnlyList<Func<IFrontendCoreModule>> CreateOrderedFactories(
        LanguagePlan plan,
        IEnumerable<IWistFrontendModuleSource> sources,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(services);

        var sourceArray = sources.ToArray();
        if (sourceArray.Any(static source => source == null))
            throw new ArgumentException("Wist frontend module sources must not contain null entries.", nameof(sources));

        var duplicatePackage = sourceArray
            .GroupBy(static source => (source.Package.Descriptor.Id, source.Package.Descriptor.Version))
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicatePackage != null)
        {
            throw new InvalidOperationException(
                $"Multiple Wist frontend module sources were supplied for package '{duplicatePackage.Key.Id.Value}' " +
                $"version '{duplicatePackage.Key.Version.Value}'.");
        }

        var sourceByPackage = sourceArray.ToDictionary(
            static source => (source.Package.Descriptor.Id, source.Package.Descriptor.Version));
        var selectedModules = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.FrontendSyntax)
            .ToArray();
        var result = new List<Func<IFrontendCoreModule>>(selectedModules.Length + 1)
        {
            static () => new WistProgramStructureFrontendModule()
        };

        foreach (var contribution in selectedModules)
        {
            if (!sourceByPackage.TryGetValue((contribution.PackageId, contribution.PackageVersion), out var source))
            {
                throw new InvalidOperationException(
                    $"No Wist frontend module source was supplied for selected package '{contribution.PackageId.Value}' " +
                    $"version '{contribution.PackageVersion.Value}'.");
            }

            ValidatePackageBinding(contribution, source);
            var registrations = source.FrontendModules
                .Where(registration => registration.ContributionId == contribution.Contribution.Id)
                .ToArray();
            if (registrations.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Package '{contribution.PackageId.Value}' must register exactly one Wist frontend module for planned contribution " +
                    $"'{contribution.Contribution.Id.Value}', but {registrations.Length} were found.");
            }

            if (WistRuntimeComponentCatalog.IsCanonicalModule(contribution.Contribution.Id) &&
                contribution.PackageId != WistLanguageFeaturePackage.PackageId)
            {
                throw new InvalidOperationException(
                    $"Canonical Wist module contribution '{contribution.Contribution.Id.Value}' can only be activated from " +
                    $"'{WistLanguageFeaturePackage.PackageId.Value}'.");
            }

            var registration = registrations[0];
            result.Add(() =>
            {
                var instance = registration.Create(services);
                if (instance is not IFrontendCoreModule module)
                {
                    throw new InvalidOperationException(
                        $"Wist frontend module factory '{registration.ContributionId.Value}' returned '{instance.GetType().FullName}', " +
                        $"which does not implement '{typeof(IFrontendCoreModule).FullName}'.");
                }
                return module;
            });
        }

        return result;
    }

    private static void ValidatePackageBinding(
        ResolvedLanguageContribution contribution,
        IWistFrontendModuleSource source)
    {
        var descriptor = source.Package.Descriptor;
        if (descriptor.Id != contribution.PackageId || descriptor.Version != contribution.PackageVersion)
            throw new InvalidOperationException("Wist frontend module source package identity does not match the language plan.");

        var manifest = LanguageFeatureManifestSerializer.ComputeSha256(descriptor);
        if (!StringComparer.Ordinal.Equals(manifest, contribution.ManifestSha256))
        {
            throw new InvalidOperationException(
                $"Wist frontend module source '{descriptor.Id.Value}' does not match the exact package manifest captured by LanguagePlan.");
        }

        if (!contribution.PackageIdentity.IsImplementationInstance(source.Package))
        {
            throw new InvalidOperationException(
                $"Wist frontend module source '{descriptor.Id.Value}' is not bound to the exact package implementation registered during planning.");
        }

        if (!descriptor.Contributions.Any(item => item.Id == contribution.Contribution.Id))
        {
            throw new InvalidOperationException(
                $"Wist frontend module source '{descriptor.Id.Value}' does not declare planned contribution '{contribution.Contribution.Id.Value}'.");
        }
    }
}
