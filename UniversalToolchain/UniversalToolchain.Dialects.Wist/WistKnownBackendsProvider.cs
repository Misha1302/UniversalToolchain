using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Compatibility alias for existing Wist-facing wiring that delegates to generic runtime backend metadata provider.
/// </summary>
public sealed class WistKnownBackendsProvider : IWistKnownBackendsProvider
{
    private readonly IRuntimeKnownBackendsProvider _inner;

    public WistKnownBackendsProvider(IRuntimeKnownBackendsProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends() => _inner.GetKnownBackends();
}
