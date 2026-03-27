using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public interface IWistKnownBackendsProvider
{
    IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends();
}
