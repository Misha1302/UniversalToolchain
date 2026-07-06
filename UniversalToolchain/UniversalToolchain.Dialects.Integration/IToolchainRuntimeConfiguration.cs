using System.Diagnostics.CodeAnalysis;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes a resolved runtime configuration that can be executed by the neutral runtime host.
/// </summary>
public interface IToolchainRuntimeConfiguration
{
    string DialectName { get; }

    IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends { get; }

    bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId);

    bool TryGetEnabledBackend(DialectBackendId backendId, [MaybeNullWhen(false)] out DialectBackendRuntimeConfiguration backendConfiguration);
}
