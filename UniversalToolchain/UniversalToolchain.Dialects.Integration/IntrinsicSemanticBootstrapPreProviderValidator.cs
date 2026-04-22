using BasicCore.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Validates intrinsic bootstrap requirements before building a provider instance.
/// </summary>
public sealed class IntrinsicSemanticBootstrapPreProviderValidator
{
    public void Validate(IntrinsicSemanticBootstrapPlan plan, IServiceCollection services)
    {
        plan = plan.ArgNotNull();
        services = services.ArgNotNull();

        var metadataValidator = new IntrinsicDescriptorProviderMetadataValidator();
        var coverageValidator = new IntrinsicSemanticCoverageValidator();
        var errors = new List<string>();

        errors.AddRange(metadataValidator.Validate(services));
        errors.AddRange(GetUnsupportedRegistrationErrors(plan.ProviderRegistrations));

        var preBuildResolvableProviderTypes = plan.GetPreBuildResolvableProviderTypes();
        errors.AddRange(coverageValidator.Validate(
            preBuildResolvableProviderTypes,
            plan.Requirements.Select(static x => (x.ModuleType, x.ProviderType)).ToList()));

        if (errors.Count > 0)
        {
            Thrower.InvalidOpEx("Intrinsic semantic startup validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    private static IReadOnlyList<string> GetUnsupportedRegistrationErrors(IReadOnlyList<IntrinsicDescriptorProviderRegistration> registrations)
    {
        return registrations
            .Where(static x => x.Kind == IntrinsicDescriptorProviderRegistrationKind.Factory)
            .OrderBy(x => x.RegistrationIndex)
            .Select(static x =>
                $"Intrinsic descriptor provider registration at index {x.RegistrationIndex} uses factory-based registration. Pre-provider bootstrap validation cannot infer provider type from factories. Use implementation-type or implementation-instance registration for IIntrinsicDescriptorProvider.")
            .ToList();
    }
}
