using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeAssemblyLoadStrategy
{
    Assembly LoadAssembly(string assemblySimpleName);
}