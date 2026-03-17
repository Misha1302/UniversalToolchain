using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectOrderDirective
{
    public DialectOrderDirective(string directive, string sourceModule, string targetModule)
    {
        if (string.IsNullOrWhiteSpace(directive))
        {
            Thrower.Argument(nameof(directive), "Directive name must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(sourceModule))
        {
            Thrower.Argument(nameof(sourceModule), "Source module must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(targetModule))
        {
            Thrower.Argument(nameof(targetModule), "Target module must not be empty.");
        }

        Directive = directive;
        SourceModule = sourceModule;
        TargetModule = targetModule;
    }

    public string Directive { get; }

    public string SourceModule { get; }

    public string TargetModule { get; }
}
