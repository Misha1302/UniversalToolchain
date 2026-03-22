using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers the real Wist runtime descriptor catalog used by dialect resolution.
/// </summary>
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly IReadOnlyList<Assembly> _runtimeAssemblies;
    private readonly IReadOnlyList<IWistDialectBackendServiceProvider> _backendProviders;

    public WistDialectRuntimeDescriptorProvider(
        IEnumerable<IWistDialectBackendServiceProvider> backendProviders,
        IEnumerable<IWistDialectRuntimeAssemblyContributor> runtimeAssemblyContributors)
    {
        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        if (runtimeAssemblyContributors == null)
            Thrower.ArgumentNull(nameof(runtimeAssemblyContributors));

        _backendProviders = backendProviders.ToList();
        _runtimeAssemblies = WistDialectRuntimeAssemblyCatalog.Build(runtimeAssemblyContributors).ToList();
    }

    public int Order => 0;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        builder
            .RegisterAttributedModulesFromAssemblies(_runtimeAssemblies.ToArray())
            .RegisterAttributedOptimizersFromAssemblies(_runtimeAssemblies.ToArray())
            .RegisterAttributedBackendsFromAssemblies(typeof(WistDialectRuntimeDescriptorProvider).Assembly);
        RegisterIntrinsics(builder);
    }

    private void RegisterIntrinsics(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        foreach (var intrinsic in WistDialectIntrinsicRegistry.CreateDescriptors(_backendProviders))
            builder.RegisterIntrinsic(intrinsic);
    }
}
