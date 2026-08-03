using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Immutable snapshot of one host-owned assembly that may retain default-context type identity.
/// </summary>
public sealed record RuntimeSharedAssemblyDescriptor(
    Assembly HostAssembly,
    RuntimeAssemblyIdentity Identity,
    string HostAssemblyPath,
    string Sha256)
{
    public static RuntimeSharedAssemblyDescriptor Create(Assembly hostAssembly)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);
        if (hostAssembly.IsDynamic)
            throw new InvalidOperationException("Dynamic assemblies cannot be registered as runtime shared assemblies.");
        if (!ReferenceEquals(AssemblyLoadContext.GetLoadContext(hostAssembly), AssemblyLoadContext.Default))
        {
            throw new InvalidOperationException(
                $"Runtime shared assembly '{hostAssembly.FullName}' must be loaded in AssemblyLoadContext.Default.");
        }
        if (string.IsNullOrWhiteSpace(hostAssembly.Location))
            throw new InvalidOperationException($"Runtime shared assembly '{hostAssembly.FullName}' has no physical location.");

        var path = Path.GetFullPath(hostAssembly.Location);
        if (!Path.IsPathRooted(path) || !File.Exists(path))
            throw new InvalidOperationException($"Runtime shared assembly path '{path}' is not an existing absolute file.");

        var identity = RuntimeAssemblyIdentity.FromAssemblyName(hostAssembly.GetName());
        var fileIdentity = RuntimeAssemblyIdentity.FromAssemblyName(AssemblyName.GetAssemblyName(path));
        if (identity != fileIdentity)
        {
            throw new InvalidOperationException(
                $"Runtime shared host assembly identity '{identity}' does not match its file identity '{fileIdentity}' at '{path}'.");
        }

        return new RuntimeSharedAssemblyDescriptor(hostAssembly, identity, path, ComputeSha256(path));
    }

    internal static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
