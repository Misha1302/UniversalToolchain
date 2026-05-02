using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Loads runtime assemblies and exact activation types from those assemblies.
/// </summary>
public interface IRuntimeAssemblyTypeLoader
{
    /// <summary>
    ///     Loads a runtime assembly by its simple name.
    /// </summary>
    Assembly LoadAssembly(string assemblySimpleName);

    /// <summary>
    ///     Loads an exact runtime activation type by full name from a runtime assembly.
    /// </summary>
    Type LoadType(string assemblySimpleName, string activationTypeFullName);
}