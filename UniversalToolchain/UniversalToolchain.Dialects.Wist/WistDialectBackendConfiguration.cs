using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

[Obsolete("Use DialectBackendRuntimeConfiguration from UniversalToolchain.Dialects.Integration.")]
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
