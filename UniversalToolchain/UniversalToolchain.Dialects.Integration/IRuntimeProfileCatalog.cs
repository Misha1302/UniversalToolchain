using System.Diagnostics.CodeAnalysis;

namespace UniversalToolchain.Dialects.Integration;

public interface IRuntimeProfileCatalog
{
    IReadOnlyList<RuntimeProfileDefinition> Profiles { get; }

    bool TryGet(string name, [MaybeNullWhen(false)] out RuntimeProfileDefinition profile);
}
