using System.Diagnostics.CodeAnalysis;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class ToolchainRuntimeHostOwnershipTests
{
    [Test]
    public void Dispose_WithBorrowedProvider_DoesNotDisposeProvider()
    {
        var provider = new TrackingServiceProvider();
        var host = new ToolchainRuntimeHost(provider, EmptyRuntimeConfiguration.Instance);

        host.Dispose();

        Assert.That(provider.IsDisposed, Is.False);
    }

    [Test]
    public void Dispose_WithOwnedProvider_DisposesProviderExactlyOnce()
    {
        var provider = new TrackingServiceProvider();
        var host = new ToolchainRuntimeHost(
            provider,
            EmptyRuntimeConfiguration.Instance,
            ServiceProviderOwnership.Owned);

        host.Dispose();
        host.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(provider.IsDisposed, Is.True);
            Assert.That(provider.DisposeCount, Is.EqualTo(1));
        });
    }

    private sealed class TrackingServiceProvider : IServiceProvider, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public object? GetService(Type serviceType) => null;

        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }

    private sealed class EmptyRuntimeConfiguration : IToolchainRuntimeConfiguration
    {
        public static EmptyRuntimeConfiguration Instance { get; } = new();

        public string DialectName => "test";

        public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends => [];

        public bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId)
        {
            backendId = default;
            return false;
        }

        public bool TryGetEnabledBackend(
            DialectBackendId backendId,
            [MaybeNullWhen(false)] out DialectBackendRuntimeConfiguration backendConfiguration)
        {
            backendConfiguration = null;
            return false;
        }
    }
}
