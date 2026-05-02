using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Resolves backend-targeted intrinsic policy directives for dialect runtime activation.
/// </summary>
public interface IDialectBackendIntrinsicPolicyResolver
{
    (IReadOnlyList<string> Allowed, IReadOnlyList<string> Forbidden, bool HasExplicitAllowList) Resolve(
        DialectBuildPlan buildPlan,
        DialectBackendId backendId);
}