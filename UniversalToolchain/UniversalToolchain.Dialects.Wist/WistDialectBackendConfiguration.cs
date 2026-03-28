using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable backend-specific execution configuration for one enabled Wist backend.
/// </summary>
public sealed class WistDialectBackendConfiguration : DialectBackendRuntimeConfiguration
{
    public WistDialectBackendConfiguration(
        RuntimeBackendDescriptor backendDescriptor,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList)
        : base(backendDescriptor, allowedIntrinsics, forbiddenIntrinsics, hasExplicitAllowList)
    {
    }
}
