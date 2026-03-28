namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeAssemblyLocator
{
    bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath);
}