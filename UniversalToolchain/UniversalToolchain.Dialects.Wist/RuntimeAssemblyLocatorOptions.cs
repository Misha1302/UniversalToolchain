namespace UniversalToolchain.Dialects.Wist;

public sealed class RuntimeAssemblyLocatorOptions
{
    public IReadOnlyList<string> AdditionalSearchDirectories { get; init; } = [];
}
