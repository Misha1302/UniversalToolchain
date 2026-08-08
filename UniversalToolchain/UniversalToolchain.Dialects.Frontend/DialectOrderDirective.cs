using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectOrderDirective
{
    public DialectOrderDirective(DialectOrderDirectiveKind kind, string sourceModule, string targetModule)
        : this(kind, sourceModule, targetModule, null)
    {
    }

    public DialectOrderDirective(
        DialectOrderDirectiveKind kind,
        string sourceModule,
        string targetModule,
        DialectSourceLocation? sourceLocation)
    {
        if (string.IsNullOrWhiteSpace(sourceModule))
            Thrower.Argument(nameof(sourceModule), "Source module must not be empty.");

        if (string.IsNullOrWhiteSpace(targetModule))
            Thrower.Argument(nameof(targetModule), "Target module must not be empty.");

        Kind = kind;
        SourceModule = sourceModule;
        TargetModule = targetModule;
        SourceLocation = sourceLocation;
    }

    public DialectOrderDirectiveKind Kind { get; }

    public string Directive => Kind switch
    {
        DialectOrderDirectiveKind.Requires => "requires",
        DialectOrderDirectiveKind.Before => "before",
        DialectOrderDirectiveKind.After => "after",
        _ => Thrower.InvalidOpEx<string>("Unknown order directive kind.")
    };

    public string SourceModule { get; }

    public string TargetModule { get; }

    public DialectSourceLocation? SourceLocation { get; }
}
