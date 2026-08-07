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
/// Wist-specific extension seam for packages that contribute frontend modules.
/// Implementations are bound to exact package registrations captured by LanguagePlan.
/// </summary>
public interface IWistFrontendModuleSource : ILanguageFeaturePackage
{
    IReadOnlyList<WistFrontendModuleRegistration> FrontendModules { get; }
}
