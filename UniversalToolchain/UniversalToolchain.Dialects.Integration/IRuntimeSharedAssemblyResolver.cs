using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeSharedAssemblyResolver
{
    RuntimeSharedAssemblyResolution Resolve(AssemblyName requestedIdentity, string configuredAssemblyPath);
}
