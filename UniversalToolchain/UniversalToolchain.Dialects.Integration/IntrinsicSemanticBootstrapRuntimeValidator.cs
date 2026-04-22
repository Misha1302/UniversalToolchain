using BasicCore.Core;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Validates intrinsic bootstrap requirements against provider-built instances.
/// </summary>
public sealed class IntrinsicSemanticBootstrapRuntimeValidator
{
    public void Validate(IServiceProvider provider, IntrinsicSemanticBootstrapPlan plan)
    {
        provider = provider.ArgNotNull();
        plan = plan.ArgNotNull();

        var validator = provider.GetRequiredService<IntrinsicSemanticStartupValidator>();
        var providers = provider.GetServices<IIntrinsicDescriptorProvider>();
        var requirements = plan.Requirements
            .Select(static x => (x.ModuleType, x.ProviderType))
            .ToList();

        validator.Validate(providers, requirements);
    }
}
