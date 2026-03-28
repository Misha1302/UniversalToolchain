using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public interface IDialectRuntimeSelection
{
    bool IsResolved { get; }

    IReadOnlyList<DialectDiagnostic> Diagnostics { get; }
}