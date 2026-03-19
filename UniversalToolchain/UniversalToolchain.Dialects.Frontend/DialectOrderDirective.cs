using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectOrderDirective
{
    public DialectOrderDirective(DialectOrderDirectiveKind kind, string sourceModule, string targetModule)
    {
        if (string.IsNullOrWhiteSpace(sourceModule))
            Thrower.Argument(nameof(sourceModule), "Source module must not be empty.");

        if (string.IsNullOrWhiteSpace(targetModule))
            Thrower.Argument(nameof(targetModule), "Target module must not be empty.");

        Kind = kind;
        SourceModule = sourceModule;
        TargetModule = targetModule;
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
}