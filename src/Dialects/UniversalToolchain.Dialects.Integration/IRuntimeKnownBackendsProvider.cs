namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeKnownBackendsProvider
{
    IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends();
}