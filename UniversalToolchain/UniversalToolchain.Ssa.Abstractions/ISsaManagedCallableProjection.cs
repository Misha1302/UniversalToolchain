using System.Reflection;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

/// <summary>
/// Projects a known managed method to a canonical SSA callable owned by a host integration.
/// Unknown methods must return false and remain execution-scoped managed callables.
/// </summary>
public interface ISsaManagedCallableProjection
{
    bool TryProject(
        MethodInfo method,
        bool consumesInstanceReceiver,
        out CallableId callable);
}
