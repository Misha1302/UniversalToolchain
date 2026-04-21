using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public interface IDialectResolvedRuntimeSelection : IDialectRuntimeSelection
{
    IReadOnlyList<RuntimeComponentManifestEntry> OrderedModules { get; }

    IReadOnlyList<RuntimeComponentManifestEntry> EnabledOptimizers { get; }

    IReadOnlyList<RuntimeComponentManifestEntry> EnabledBackends { get; }
}
