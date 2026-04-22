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
        errors.AddRange(coverageValidator.Validate(
            plan.RegisteredProviderTypes,
            plan.Requirements.Select(static x => (x.ModuleType, x.ProviderType)).ToList()));

        if (errors.Count > 0)
        {
            Thrower.InvalidOpEx("Intrinsic semantic startup validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }
}
