using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeProfileProvenanceEntry
{
    public RuntimeProfileProvenanceEntry(string directiveKind, string directiveName, string source)
    {
        if (string.IsNullOrWhiteSpace(directiveKind))
            Thrower.Argument(nameof(directiveKind), "Directive kind must not be empty.");

        if (string.IsNullOrWhiteSpace(directiveName))
            Thrower.Argument(nameof(directiveName), "Directive name must not be empty.");

        if (string.IsNullOrWhiteSpace(source))
            Thrower.Argument(nameof(source), "Directive source must not be empty.");

        DirectiveKind = directiveKind;
        DirectiveName = directiveName;
        Source = source;
    }

    public string DirectiveKind { get; }

    public string DirectiveName { get; }

    public string Source { get; }
}
