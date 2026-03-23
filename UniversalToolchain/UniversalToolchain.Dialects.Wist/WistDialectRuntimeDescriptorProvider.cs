using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers central Wist runtime descriptors that are owned by the Wist dialect layer itself.
/// </summary>
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly IReadOnlyList<IWistDialectBackendServiceProvider> _backendProviders;

    public WistDialectRuntimeDescriptorProvider(IEnumerable<IWistDialectBackendServiceProvider> backendProviders)
    {
        if (backendProviders == null)
        {
            Thrower.ArgumentNull(nameof(backendProviders));
        }

        _backendProviders = backendProviders.ToList();
    }

    public int Order => 0;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        builder.RegisterAttributedBackendsFromAssemblies(typeof(WistDialectRuntimeDescriptorProvider).Assembly);
        RegisterIntrinsics(builder);
    }

    private void RegisterIntrinsics(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        foreach (var intrinsic in WistDialectIntrinsicRegistry.CreateDescriptors(_backendProviders))
        {
            builder.RegisterIntrinsic(intrinsic);
        }
    }
}
