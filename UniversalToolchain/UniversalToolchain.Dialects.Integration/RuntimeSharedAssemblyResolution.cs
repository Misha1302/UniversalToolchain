using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

public enum RuntimeSharedAssemblyResolutionKind
{
    NotShared,
    Shared
}

public sealed record RuntimeSharedAssemblyResolution(
    RuntimeSharedAssemblyResolutionKind Kind,
    Assembly? Assembly)
{
    public static RuntimeSharedAssemblyResolution NotShared { get; } =
        new(RuntimeSharedAssemblyResolutionKind.NotShared, null);

    public static RuntimeSharedAssemblyResolution Shared(Assembly assembly) =>
        new(RuntimeSharedAssemblyResolutionKind.Shared, assembly ?? throw new ArgumentNullException(nameof(assembly)));
}
