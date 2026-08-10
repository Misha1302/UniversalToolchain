using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Exact Wist frontend module registration owned by a language package.
/// The contribution id is the only selection key; aliases and implementation names are not runtime authority.
/// </summary>
public sealed class WistFrontendModuleRegistration
{
    private readonly Func<IServiceProvider, object> _factory;

    public WistFrontendModuleRegistration(
        LanguageContributionId contributionId,
        Func<IServiceProvider, object> factory)
    {
        ContributionId = contributionId;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public LanguageContributionId ContributionId { get; }

    internal object Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return _factory(services) ?? throw new InvalidOperationException(
            $"Wist frontend module factory '{ContributionId.Value}' returned null.");
    }
}

/// <summary>
/// Wist-specific extension seam that binds frontend registrations to the exact feature-package object
/// used during LanguagePlan compilation.
/// </summary>
public interface IWistFrontendModuleSource
{
    ILanguageFeaturePackage Package { get; }
    IReadOnlyList<WistFrontendModuleRegistration> FrontendModules { get; }
}

public sealed class WistFrontendModuleSource : IWistFrontendModuleSource
{
    public WistFrontendModuleSource(
        ILanguageFeaturePackage package,
        IEnumerable<WistFrontendModuleRegistration> frontendModules)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        ArgumentNullException.ThrowIfNull(frontendModules);
        FrontendModules = frontendModules.ToArray();
        if (FrontendModules.Any(static registration => registration == null))
            throw new ArgumentException("Wist frontend registrations must not contain null entries.", nameof(frontendModules));
    }

    public ILanguageFeaturePackage Package { get; }
    public IReadOnlyList<WistFrontendModuleRegistration> FrontendModules { get; }
}
