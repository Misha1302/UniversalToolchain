namespace UniversalToolchain.Dialects.Wist;

public interface IRuntimeAssemblyLocator
{
    bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath);
}
