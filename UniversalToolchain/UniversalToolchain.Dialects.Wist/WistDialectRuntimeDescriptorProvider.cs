using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers central Wist runtime descriptors that are owned by the Wist dialect layer itself.
/// </summary>
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    private readonly IReadOnlyList<IDialectBackendRuntimeRegistrar> _backendProviders;

    public WistDialectRuntimeDescriptorProvider(IEnumerable<IDialectBackendRuntimeRegistrar> backendProviders)
    {
        if (backendProviders == null)
        {
            Thrower.ArgumentNull(nameof(backendProviders));
        }

        _backendProviders = backendProviders.ToList();
    }

    public decimal Order => 0m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

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
